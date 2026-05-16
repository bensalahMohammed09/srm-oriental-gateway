import os
import time
import shutil
import logging
import re
import uuid
from pathlib import Path

# Third-party libraries for OCR and Image Processing
import requests
from requests.adapters import HTTPAdapter
from urllib3.util.retry import Retry
from pythonjsonlogger import jsonlogger

import cv2
import numpy as np
import pytesseract
from pdf2image import convert_from_path
from PIL import Image

# ==============================================================================
# CONFIGURATION & ENVIRONMENT SETUP
# ==============================================================================
API_URL = os.getenv("API_URL", "http://srm-api:9000/api/v1/document/ingest")

PENDING_DIR = Path(os.getenv("STORAGE_PENDING_PATH", "/app/uploads/pending"))
PROCESSED_DIR = Path(os.getenv("STORAGE_PROCESSED_PATH", "/app/uploads/processed"))
FAILED_DIR = Path(os.getenv("STORAGE_FAILED_PATH", "/app/uploads/failed"))

for directory in [PENDING_DIR, PROCESSED_DIR, FAILED_DIR]:
    directory.mkdir(parents=True, exist_ok=True)

# ==============================================================================
# LOGGING CONFIGURATION (LOKI/GRAFANA READY)
# ==============================================================================
logger = logging.getLogger("srm_ocr_worker")
logger.setLevel(logging.INFO)
logHandler = logging.StreamHandler()

formatter = jsonlogger.JsonFormatter(
    fmt='%(asctime)s %(levelname)s %(name)s %(message)s'
)
logHandler.setFormatter(formatter)
logger.addHandler(logHandler)

def get_resilient_session():
    session = requests.Session()
    retry_strategy = Retry(
        total=5,
        backoff_factor=1,
        status_forcelist=[429, 500, 502, 503, 504],
        allowed_methods=["HEAD", "GET", "POST", "OPTIONS"]
    )
    adapter = HTTPAdapter(max_retries=retry_strategy)
    session.mount("http://", adapter)
    session.mount("https://", adapter)
    return session

http_client = get_resilient_session()

# ==============================================================================
# IMAGE PROCESSING & COMPUTER VISION
# ==============================================================================
def preprocess_image(image: Image.Image) -> np.ndarray:
    open_cv_image = np.array(image)
    gray = cv2.cvtColor(open_cv_image, cv2.COLOR_RGB2GRAY)
    denoised = cv2.fastNlMeansDenoising(gray, h=10)
    processed = cv2.adaptiveThreshold(
        denoised, 255, cv2.ADAPTIVE_THRESH_GAUSSIAN_C, 
        cv2.THRESH_BINARY, 11, 2
    )
    return processed

# ==============================================================================
# DOCUMENT PARSING & DATA EXTRACTION
# ==============================================================================
def parse_invoice_text(full_text: str, avg_confidence: float = 0.99) -> dict:
    ref_pattern = r'(?:Facture\s+Ref|Facture|Ref|N[°\.]+)[\s:]*([A-Z0-9\-]{3,})'
    ref_match = re.search(ref_pattern, full_text, re.IGNORECASE)
    
    date_pattern = r'(\d{2}/\d{2}/\d{4}|\d{4}-\d{2}-\d{2})'
    date_match = re.search(date_pattern, full_text)
    
    amount_pattern = r'(?:Total|TTC|Montant)[\s:]*([\d\s]+[,.]\d{2})'
    amount_match = re.search(amount_pattern, full_text, re.IGNORECASE)

    reference = ref_match.group(1) if ref_match else "REQUIRES_MANUAL_REVIEW"
    date_val = date_match.group(1) if date_match else "N/A"
    
    try:
        raw_amount = amount_match.group(1).replace(' ', '').replace(',', '.') if amount_match else "0.0"
        total_amount = float(raw_amount)
    except (ValueError, AttributeError):
        total_amount = 0.0

    return {
        "reference": reference,
        "supplierName": "SRM ORI (Automatic Detection)", 
        "totalAmount": total_amount,
        "metadata": [
            { "key": "Date", "value": date_val, "confidence": round(avg_confidence, 2) },
            { "key": "ExtractionTimestamp", "value": time.strftime("%Y-%m-%d %H:%M:%S"), "confidence": 1.0 }
        ]
    }

# ==============================================================================
# ORCHESTRATION LAYER 
# ==============================================================================
def extract_data_from_document(file_path: Path, log_context: dict) -> dict:
    logger.info("Starting OCR extraction sequence", extra=log_context)
    
    images = []
    if file_path.suffix.lower() == '.pdf':
        images = convert_from_path(file_path)
    else:
        images = [Image.open(file_path)]

    full_text = ""
    confidences = []

    if images:
        processed_img = preprocess_image(images[0])
        ocr_data = pytesseract.image_to_data(processed_img, lang='fra', output_type=pytesseract.Output.DICT)
        
        for i, word in enumerate(ocr_data['text']):
            conf = float(ocr_data['conf'][i])
            if word.strip() and conf > 0:
                full_text += word + " "
                confidences.append(conf)

    avg_confidence = (sum(confidences) / len(confidences) / 100.0) if confidences else 0.0
    
    # 🌟 THE GARBAGE FILTER: Reject files with low confidence (e.g., non-documents)
    if avg_confidence < 0.50:
        raise ValueError(f"OCR confidence too low ({round(avg_confidence, 2)}). Image is likely unreadable or not a document.")

    payload = parse_invoice_text(full_text, avg_confidence)
    
    # Inject standard metadata
    payload["metadata"].append({
        "key": "SourceFile", 
        "value": file_path.name, 
        "confidence": 1.0
    })

    logger.info("OCR extraction completed successfully", extra={
        **log_context, 
        "avg_confidence": round(avg_confidence, 2),
        "extracted_reference": payload["reference"]
    })
    return payload

def process_file(file_path: Path, log_context: dict):
    start_time = time.time()
    filename = file_path.name
    correlation_id = log_context.get("correlation_id")
    
    try:
        # Step 1: Extract Data
        payload = extract_data_from_document(file_path, log_context)
        
        # 🌟 NEW: Bind the extracted reference to the logging context!
        # Now every subsequent log will have BOTH the UUID and the real Invoice Reference.
        extracted_ref = payload.get("reference", "UNKNOWN")
        log_context["document_reference"] = extracted_ref
        
        # 🌟 INJECT CORRELATION ID INTO PAYLOAD METADATA
        payload["metadata"].append({
            "key": "WorkerCorrelationId",
            "value": correlation_id,
            "confidence": 1.0
        })
        
        ocr_duration = int((time.time() - start_time) * 1000)
        logger.info("OCR Processing Metrics", extra={**log_context, "duration_ms": ocr_duration})
        
        # Step 2: Push to API
        logger.info("Dispatching payload to API", extra=log_context)
        
        # 🌟 NEW: Pass the reference in the headers too, so the C# API can log it immediately!
        headers = {
            "X-Correlation-ID": correlation_id,
            "X-Document-Reference": extracted_ref
        }
        response = http_client.post(API_URL, json=payload, headers=headers, timeout=20)
        response.raise_for_status()
        
        # Step 3: Atomic Archival
        shutil.move(str(file_path), str(PROCESSED_DIR / filename))
        
        total_duration = int((time.time() - start_time) * 1000)
        logger.info("File successfully processed and archived", extra={**log_context, "total_duration_ms": total_duration})

    except ValueError as ve:
        # Catch our custom confidence error
        logger.warning(f"Document rejected: {ve}", extra=log_context)
        shutil.move(str(file_path), str(FAILED_DIR / filename))
    except requests.exceptions.RequestException as e:
        logger.error("API Communication Failure", extra={**log_context, "error": str(e)})
        shutil.move(str(file_path), str(FAILED_DIR / filename))
    except Exception as e:
        logger.exception("Fatal processing error", extra={**log_context, "error": str(e)})
        shutil.move(str(file_path), str(FAILED_DIR / filename))

# ==============================================================================
# MAIN SERVICE LOOP
# ==============================================================================
def main():
    logger.info("SRM OCR Worker Service is active", extra={"monitored_path": str(PENDING_DIR.resolve())})
    
    while True:
        valid_extensions = ['.pdf', '.png', '.jpg', '.jpeg']
        files = [f for f in PENDING_DIR.iterdir() if f.suffix.lower() in valid_extensions]
        
        if not files:
            time.sleep(5)
            continue
            
        logger.info(f"Detected {len(files)} new document(s) in pending folder")
        
        for file_path in files:
            # 🌟 GENERATE A UNIQUE CORRELATION ID FOR OBSERVABILITY
            correlation_id = str(uuid.uuid4())
            
            log_context = {"correlation_id": correlation_id, "file": file_path.name}
            logger.info("Picked up file for processing", extra=log_context)
            
            process_file(file_path, log_context)

if __name__ == "__main__":
    main()