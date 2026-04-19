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
# The API_URL uses the Docker service name 'srm-api' and internal port 9000
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

# Using JSON formatter for structured logging and easier observability
formatter = jsonlogger.JsonFormatter(
    fmt='%(asctime)s %(levelname)s %(name)s %(message)s'
)
logHandler.setFormatter(formatter)
logger.addHandler(logHandler)

# ==============================================================================
# NETWORK RESILIENCE (RETRY STRATEGY)
# ==============================================================================
def get_resilient_session():
    """
    Implements Exponential Backoff and Retries for network stability.
    This ensures the worker doesn't crash if the .NET API is temporarily down.
    """
    session = requests.Session()
    retry_strategy = Retry(
        total=5,
        backoff_factor=1, # 1s, 2s, 4s, 8s, 16s
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
    """
    Advanced Computer Vision preprocessing to optimize Tesseract accuracy.
    Uses Adaptive Gaussian Thresholding to handle shadows and uneven lighting.
    """
    # Convert PIL Image to OpenCV format (numpy array)
    open_cv_image = np.array(image)
    
    # Step 1: Grayscale conversion
    gray = cv2.cvtColor(open_cv_image, cv2.COLOR_RGB2GRAY)
    
    # Step 2: Denoising (optional but recommended for mobile photos)
    denoised = cv2.fastNlMeansDenoising(gray, h=10)
    
    # Step 3: Adaptive Thresholding
    # Block size 11 and constant 2 are optimized for document scanning
    processed = cv2.adaptiveThreshold(
        denoised, 255, cv2.ADAPTIVE_THRESH_GAUSSIAN_C, 
        cv2.THRESH_BINARY, 11, 2
    )
    
    return processed

# ==============================================================================
# DOCUMENT PARSING & DATA EXTRACTION
# ==============================================================================
def parse_invoice_text(full_text: str, avg_confidence: float = 0.99) -> dict:
    """
    Business Logic: Uses Regular Expressions to extract structured data.
    Implements fail-safe defaults (UNKNOWN/0.0) to prevent pipeline blockage.
    """
    # Refined Regex for Invoice Reference: Handles "Facture Ref", "Ref", "N°"
    ref_pattern = r'(?:Facture\s+Ref|Facture|Ref|N[°\.]+)[\s:]*([A-Z0-9\-]{3,})'
    ref_match = re.search(ref_pattern, full_text, re.IGNORECASE)
    
    # Date Pattern: Supports YYYY-MM-DD and DD/MM/YYYY
    date_pattern = r'(\d{2}/\d{2}/\d{4}|\d{4}-\d{2}-\d{2})'
    date_match = re.search(date_pattern, full_text)
    
    # Amount Pattern: Looks for Total/TTC followed by a number with 2 decimals
    amount_pattern = r'(?:Total|TTC|Montant)[\s:]*([\d\s]+[,.]\d{2})'
    amount_match = re.search(amount_pattern, full_text, re.IGNORECASE)

    # Data Normalization
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
# ORCHESTRATION LAYER
# ==============================================================================
def extract_data_from_document(file_path: Path) -> dict:
    """
    High-level orchestration of the OCR process for a single file.
    """
    logger.info("Starting OCR extraction sequence", extra={"file": file_path.name})
    
    images = []
    # Support for both PDF and standard image formats
    if file_path.suffix.lower() == '.pdf':
        images = convert_from_path(file_path)
    else:
        images = [Image.open(file_path)]

    full_text = ""
    confidences = []

    # Process first page (Main Page)
    if images:
        processed_img = preprocess_image(images[0])
        
        # Extract full data structure from Tesseract to get confidence scores per word
        ocr_data = pytesseract.image_to_data(processed_img, lang='fra', output_type=pytesseract.Output.DICT)
        
        for i, word in enumerate(ocr_data['text']):
            conf = float(ocr_data['conf'][i])
            # Only include non-empty words with positive confidence
            if word.strip() and conf > 0:
                full_text += word + " "
                confidences.append(conf)

    # Normalized confidence score (0.0 to 1.0)
    avg_confidence = (sum(confidences) / len(confidences) / 100.0) if confidences else 0.0
    
    # Parse text into structured JSON
    payload = parse_invoice_text(full_text, avg_confidence)
    
    # LINKING STEP: Add SourceFile metadata for Backend reconciliation
    payload["metadata"].append({
        "key": "SourceFile", 
        "value": file_path.name, 
        "confidence": 1.0
    })

    logger.info("OCR extraction completed successfully", extra={
        "file": file_path.name, 
        "avg_confidence": round(avg_confidence, 2)
    })
    return payload

def process_file(file_path: Path):
    """
    Manages the lifecycle of a document: Extraction -> Transmission -> Archival.
    """
    try:
        # Step 1: Extract Data
        payload = extract_data_from_document(file_path)
        
        # Step 2: Push to API
        logger.info("Dispatching payload to API", extra={"file": file_path.name})
        response = http_client.post(API_URL, json=payload, timeout=20)
        response.raise_for_status()
        
        # Step 3: Atomic Archival
        shutil.move(str(file_path), str(PROCESSED_DIR / file_path.name))
        logger.info("File successfully processed and archived", extra={"file": file_path.name})

    except requests.exceptions.RequestException as e:
        # Resilience: Do not move the file so it can be retried in the next poll
        logger.error("API Communication Failure", extra={"file": file_path.name, "error": str(e)})
    except Exception as e:
        # Critical Failure: Move to failed directory to avoid infinite loops
        logger.exception("Fatal processing error", extra={"file": file_path.name, "error": str(e)})
        shutil.move(str(file_path), str(FAILED_DIR / file_path.name))

# ==============================================================================
# MAIN SERVICE LOOP
# ==============================================================================
def main():
    """
    Main entry point for the SRM OCR Worker.
    Implements a non-blocking polling mechanism.
    """
    logger.info("SRM OCR Worker Service is active and monitoring 'pending' folder")
    
    while True:
        # Discover files
        valid_extensions = ['.pdf', '.png', '.jpg', '.jpeg']
        files = [f for f in PENDING_DIR.iterdir() if f.suffix.lower() in valid_extensions]
        
        if not files:
            # Idle state: sleep to reduce I/O and CPU usage
            time.sleep(5)
            continue
            
        logger.info(f"Detected {len(files)} new document(s) for processing")
        for file_path in files:
            process_file(file_path)

if __name__ == "__main__":
    main()