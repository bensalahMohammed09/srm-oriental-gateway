import pytest
from main import parse_invoice_text

def test_parse_invoice_text_valid_srm_format():
    """
    Test the happy path with a perfectly formatted invoice.
    Ensures that standard inputs yield correct exact types and values.
    """
    # 1. ARRANGE
    fake_ocr_text = """
    SRM Oriental - Digital Bureau d'Ordre
    Date: 2026-04-18
    Facture N°: STR-999-2026
    Total TTC: 1250.50 MAD
    """
    
    # 2. ACT
    result = parse_invoice_text(fake_ocr_text, 0.95)
    
    # 3. ASSERT
    assert result["reference"] == "STR-999-2026"
    assert result["totalAmount"] == 1250.50
    assert result["metadata"][0]["value"] == "2026-04-18"
    assert result["metadata"][0]["confidence"] == 0.95

def test_parse_invoice_text_missing_amount():
    """
    Test resilience when the OCR fails to read the amount due to poor image quality.
    """
    # 1. ARRANGE
    fake_ocr_text = "Facture Ref: STR-111-2026. No total found due to blur."
    
    # 2. ACT
    result = parse_invoice_text(fake_ocr_text)
    
    # 3. ASSERT: Should gracefully default to 0.0 without throwing a ValueError
    assert result["totalAmount"] == 0.0
    assert result["reference"] == "STR-111-2026"

def test_parse_invoice_text_messy_amount_formatting():
    """
    Test the regex's ability to clean spaces and commas in numbers.
    Very common with European/Moroccan number formatting in OCR.
    """
    # 1. ARRANGE
    fake_ocr_text = "Ref: A12 Total: 1 250,50"
    
    # 2. ACT
    result = parse_invoice_text(fake_ocr_text)
    
    # 3. ASSERT: The script should convert "1 250,50" into a clean float 1250.50
    assert result["totalAmount"] == 1250.50

def test_parse_invoice_text_missing_date():
    """
    Test resilience when the date regex finds no match.
    """
    # 1. ARRANGE
    fake_ocr_text = "Facture STR-404-2026 Total TTC: 100.00"
    
    # 2. ACT
    result = parse_invoice_text(fake_ocr_text)
    
    # 3. ASSERT: The date key should exist but contain 'N/A'
    assert result["metadata"][0]["key"] == "Date"
    assert result["metadata"][0]["value"] == "N/A"