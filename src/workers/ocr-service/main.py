import os
import time
import shutil
import logging
import re
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
UPLOAD_DIR = Path("uploads")
PENDING_DIR = UPLOAD_DIR / "pending"
PROCESSED_DIR = UPLOAD_DIR / "processed"
FAILED_DIR = UPLOAD_DIR / "failed"

# Core SRE Principle: Ensure infrastructure readiness on startup
for directory in [PENDING_DIR, PROCESSED_DIR, FAILED_DIR]:
    directory.mkdir(parents=True, exist_ok=True)

# ==============================================================================
# LOGGING CONFIGURATION (LOKI/GRAFANA READY)
# ==============================================================================
logger = logging.getLogger("srm_ocr_worker")
logger.setLevel(logging.INFO)
logHandler = logging.StreamHandler()

# Using JSON formatter for structured logging - Loki va adorer ça
formatter = jsonlogger.JsonFormatter(
    fmt='%(asctime)s %(levelname)s %(name)s %(message)s'
)
logHandler.setFormatter(formatter)
logger.addHandler(logHandler)

# ==============================================================================
# NETWORK RESILIENCE (RETRY STRATEGY)
# ==============================================================================
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

    reference = ref_match.group(1) if ref_match else f"UNKNOWN-{int(time.time())}"
    date_val = date_match.group(1) if date_match else "N/A"
    
    try:
        raw_amount = amount_match.group(1).replace(' ', '').replace(',', '.') if amount_match else "0.0"
        total_amount = float(raw_amount)
    except (ValueError, AttributeError):
        total_amount = 0.0

    return {
        "reference": reference,
        "supplierName": "SRM Oriental (Automatic Detection)", 
        "totalAmount": total_amount,
        "metadata": [
            { "key": "Date", "value": date_val, "confidence": round(avg_confidence, 2) },
            { "key": "ExtractionTimestamp", "value": time.strftime("%Y-%m-%d %H:%M:%S"), "confidence": 1.0 }
        ]
    }

# ==============================================================================
# ORCHESTRATION LAYER (MIS A JOUR POUR L'OBSERVABILITE)
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
    payload = parse_invoice_text(full_text, avg_confidence)
    
    payload["metadata"].append({
        "key": "SourceFile", 
        "value": file_path.name, 
        "confidence": 1.0
    })

    # On passe le log_context pour garder la trace
    logger.info("OCR extraction completed successfully", extra={
        **log_context, 
        "avg_confidence": round(avg_confidence, 2)
    })
    return payload

def process_file(file_path: Path):
    start_time = time.time()
    filename = file_path.name
    
    # 1. EXTRACTION DU CORRELATION ID
    # On s'attend à ce que n8n nomme le fichier "1234abcd-5678_nomfichier.pdf"
    parts = filename.split('_', 1)
    correlation_id = parts[0] if len(parts) > 1 else "unknown-id"
    
    # Dictionnaire de base pour tous les logs de ce document
    log_context = {"correlation_id": correlation_id, "file": filename}
    
    try:
        # Step 1: Extract Data
        payload = extract_data_from_document(file_path, log_context)
        
        # 2. CALCUL DE LA DUREE (Pour Dashboard Grafana RED)
        ocr_duration = int((time.time() - start_time) * 1000)
        logger.info("OCR Processing Metrics", extra={**log_context, "duration_ms": ocr_duration})
        
        # Step 2: Push to API
        logger.info("Dispatching payload to API", extra=log_context)
        
        # 3. RENVOI DU CORRELATION ID A L'API
        headers = {"X-Correlation-ID": correlation_id}
        response = http_client.post(API_URL, json=payload, headers=headers, timeout=20)
        response.raise_for_status()
        
        # Step 3: Atomic Archival
        shutil.move(str(file_path), str(PROCESSED_DIR / filename))
        
        total_duration = int((time.time() - start_time) * 1000)
        logger.info("File successfully processed and archived", extra={**log_context, "total_duration_ms": total_duration})

    except requests.exceptions.RequestException as e:
        logger.error("API Communication Failure", extra={**log_context, "error": str(e)})
    except Exception as e:
        logger.exception("Fatal processing error", extra={**log_context, "error": str(e)})
        shutil.move(str(file_path), str(FAILED_DIR / filename))

# ==============================================================================
# MAIN SERVICE LOOP
# ==============================================================================
def main():
    logger.info("SRM OCR Worker Service is active and monitoring 'pending' folder")
    
    while True:
        valid_extensions = ['.pdf', '.png', '.jpg', '.jpeg']
        files = [f for f in PENDING_DIR.iterdir() if f.suffix.lower() in valid_extensions]
        
        if not files:
            time.sleep(5)
            continue
            
        logger.info(f"Detected {len(files)} new document(s) for processing")
        for file_path in files:
            process_file(file_path)

if __name__ == "__main__":
    main()