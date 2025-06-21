import re
from normalizer import normalize_date, normalize_monetary

def detect_receipt_fields(lines):
    """
    Extracts relevant fields from OCR'd lines of a receipt.
    This is a simplified detector for receipts, not official invoices.
    """
    result = {
        'firma_adi': None,
        'vergi_no': None,
        'fis_no': None,
        'tarih': None,
        'toplam_tutar': None,
        'kdv_toplam': None,
        'matrah_toplam': None,
        'kdv_oranlari': {}, # e.g., {'18': '15.25'}
        'matrah_oranlari': {} # e.g., {'18': '84.75'}
    }

    # Simplified logic for receipts
    # 1. Company Name (usually at the top)
    if lines:
        result['firma_adi'] = lines[0]

    # 2. Date and Receipt No (often on the same line or close)
    for line in lines:
        # Date
        date_match = re.search(r'\d{2}[./-]\d{2}[./-]\d{4}', line)
        if date_match and not result['tarih']:
            result['tarih'] = normalize_date(date_match.group(0))

        # Receipt No
        fis_no_match = re.search(r'(?:Fiş|FIS) NO\s?[:\s]\s?(\w+)', line, re.IGNORECASE)
        if fis_no_match and not result['fis_no']:
            result['fis_no'] = fis_no_match.group(1)

    # 3. Total Amount (look for keywords like TOPLAM, but often the largest value is the total)
    total = 0.0
    for line in lines:
        # Use regex to find monetary values
        monetary_values = re.findall(r'(\d+[.,]\d{2})', line)
        for val_str in monetary_values:
            val_float = float(val_str.replace(',', '.'))
            # Heuristic: Find the largest monetary value on the receipt for the total
            if val_float > total:
                total = val_float
    
    if total > 0.0:
        result['toplam_tutar'] = normalize_monetary(str(total).replace('.', ','))

    # 4. VKN (can be complex, look for 10 digits with a keyword)
    for line in lines:
        vkn_match = re.search(r'\b\d{10}\b', line)
        if vkn_match:
            if 'vkn' in line.lower() or 'vergi' in line.lower():
                 result['vergi_no'] = vkn_match.group(0)
                 break
    
    # 5. VAT and Base amounts (look for % signs)
    # This is a simplified approach
    total_kdv = 0.0
    total_matrah = 0.0
    for line in lines:
        # Example: %18 KDV 15,25
        kdv_match = re.search(r'%\s?(\d{1,2})\s*(?:KDV|TOPKDV)?\s*(\d+[.,]\d{2})', line, re.IGNORECASE)
        if kdv_match:
            rate = kdv_match.group(1)
            value_str = kdv_match.group(2)
            value_float = float(value_str.replace(',', '.'))
            total_kdv += value_float
            result['kdv_oranlari'][rate] = normalize_monetary(value_str)

        # Example: Matrah 84,75
        matrah_match = re.search(r'(?:MATRAH|matrah)\s*(\d+[.,]\d{2})', line, re.IGNORECASE)
        if matrah_match:
             matrah_str = matrah_match.group(1)
             matrah_float = float(matrah_str.replace(',', '.'))
             total_matrah += matrah_float
             # Try to associate with a VAT rate on the same line
             rate_on_line_match = re.search(r'%\s?(\d{1,2})', line)
             if rate_on_line_match:
                 rate = rate_on_line_match.group(1)
                 result['matrah_oranlari'][rate] = normalize_monetary(matrah_str)

    if total_kdv > 0:
        result['kdv_toplam'] = normalize_monetary(str(total_kdv).replace('.', ','))
    if total_matrah > 0:
        result['matrah_toplam'] = normalize_monetary(str(total_matrah).replace('.', ','))

    return result 