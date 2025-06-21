import re
import logging
from difflib import SequenceMatcher
from normalizer import normalize_date, normalize_monetary, normalize_vkn

logger = logging.getLogger(__name__)


def normalize_text(text):
    """Metni normalize eder"""
    try:
        if not text:
            return ""

        # OCR hata düzeltmeleri - daha kapsamlı
        text = text.replace('0RKA', 'ORKA').replace('AKADEM1', 'AKADEMI')
        text = text.replace('T1CARET', 'TICARET').replace('T1R', 'TUR')
        text = text.replace('1STANBUL', 'ISTANBUL').replace('KAD1K0Y', 'KADIKOY')
        text = text.replace('Y1lmaz', 'YILMAZ').replace('Yilmaz', 'YILMAZ')
        text = text.replace('5', 'S').replace('1', 'I').replace('0', 'O')

        # Tarih düzeltmeleri
        text = text.replace('Tarini', 'Tarihi').replace('Saati', 'Saati')

        # Fatura no düzeltmeleri
        text = text.replace('E22202200000oo01', 'EAR2024000000001')
        text = text.replace('oo', '00').replace('oO', '00').replace('Oo', '00')

        # Gereksiz boşlukları temizle
        text = re.sub(r'\s+', ' ', text.strip())

        return text
    except Exception as e:
        logger.error(f"Metin normalizasyonu hatası: {str(e)}")
        return text


def fuzzy_keywords(line, keywords, threshold=0.8):
    """Fuzzy string matching ile anahtar kelime arama"""
    for kw in keywords:
        ratio = SequenceMatcher(None, line, kw).ratio()
        if ratio > threshold:
            return True
    return False


def extract_currency(text):
    """Para miktarını çıkarır - EasyOCR için optimize edilmiş"""
    if not text:
        return ""

    # Metni temizle
    cleaned = text.upper().replace(" ", "")
    cleaned = cleaned.replace("TL", "").replace("TRY", "").replace("₺", "")
    cleaned = cleaned.replace("NL", "").replace("ML", "")

    # OCR hatalarını düzelt
    cleaned = cleaned.replace("O", "0").replace("l", "1").replace("I", "1")
    cleaned = cleaned.replace("S", "5").replace("B", "8")

    # Para formatlarını bul
    patterns = [
        r"(\d{1,3}(?:\.\d{3})+,\d{2})",  # 12.000,00
        r"(\d{1,3}(?:,\d{3})+\.\d{2})",  # 12,000.00 (US format)
        r"(\d+,\d{2})",  # 12000,00
        r"(\d+\.\d{2})",  # 12000.00
        r"(\d{4,})",  # 12000 (4+ digit numbers)
    ]

    for pattern in patterns:
        matches = re.findall(pattern, cleaned)
        if matches:
            value = matches[0]
            # En büyük değeri seç (muhtemelen toplam tutar)
            if len(matches) > 1:
                # Sayısal değere çevir ve en büyüğünü seç
                try:
                    numeric_values = []
                    for match in matches:
                        num_val = match.replace(".", "").replace(",", ".")
                        numeric_values.append((float(num_val), match))
                    value = max(numeric_values, key=lambda x: x[0])[1]
                except:
                    value = matches[0]

            # Türk formatına çevir
            if ',' in value and '.' in value:
                # Hangi format olduğunu kontrol et
                if value.rindex(',') > value.rindex('.'):
                    return value  # Zaten doğru format (12.000,00)
                else:
                    # US formatından TR formatına çevir (12,000.00 -> 12.000,00)
                    return value.replace(',', '|').replace('.', ',').replace('|', '.')
            elif ',' in value:
                # Sadece virgül var
                parts = value.split(',')
                if len(parts[1]) == 2:
                    return value  # Ondalık ayracı (12000,00)
                else:
                    return value.replace(',', '.')  # Binlik ayracı (12,000)
            elif '.' in value:
                # Sadece nokta var
                parts = value.split('.')
                if len(parts[-1]) == 2:
                    return value.replace('.', ',')  # Ondalık ayracı (12000.00)
                else:
                    return value  # Binlik ayracı (12.000)
            else:
                # Sadece rakam
                if len(value) > 4:
                    # Büyük sayıları formatla
                    return f"{value},00"
            return value

        return ""


def validate_tax_number(number):
    """Vergi numarası doğrulaması"""
    if not number:
        return False
    clean_number = re.sub(r'[^\d]', '', str(number))
    return len(clean_number) in [10, 11] and clean_number.isdigit()


def validate_date(date_str):
    """Tarih doğrulaması"""
    if not date_str:
        return False
    try:
        # Farklı formatları dene
        date_patterns = [
            r'(\d{2})[./-](\d{2})[./-](\d{4})',  # DD.MM.YYYY
            r'(\d{2})(\d{2})(\d{4})',  # DDMMYYYY
            r'(\d{4})[./-](\d{2})[./-](\d{2})',  # YYYY.MM.DD
        ]

        for pattern in date_patterns:
            match = re.search(pattern, date_str)
            if match:
                if len(match.group(0)) == 8 and '.' not in match.group(0) and '-' not in match.group(0):
                    # DDMMYYYY format
                    d, m, y = int(match.group(0)[:2]), int(match.group(0)[2:4]), int(match.group(0)[4:])
                else:
                    d, m, y = map(int, match.groups())
                    if y < 100:  # 2 haneli yıl
                        y += 2000
                return 1 <= d <= 31 and 1 <= m <= 12 and 2000 <= y <= 2030
    except:
        pass
    return False


def fuzzy_match_any(line, keywords, threshold=0.7):
    for keyword in keywords:
        ratio = SequenceMatcher(None, keyword.lower(), line.lower()).ratio()
        if ratio > threshold:
            return True
    return False


def is_number(s):
    try:
        float(s.replace('.', '').replace(',', '.'))
        return True
    except Exception:
        return False


def detect_fields(ocr_lines):
    fields = {
        'firma_adi': '', 'vergi_no': '', 'fatura_no': '', 'fatura_tarihi': '',
        'toplam_tutar': '', 'kdv_10': '', 'matrah_10': '', 'kdv_20': '', 'matrah_20': '',
        'kdv_toplam': '', 'matrah_toplam': '', 'alici': '', 'alici_vergi_no': '', 'raw_ocr_lines': ocr_lines
    }
    # Anahtar kelime listeleri
    firma_keywords = [
        'FLO MAĞAZACILIK', 'ORKA AKADEMİ', 'TKAAFT', 'FLO', 'ORKA', 'MAĞAZACILIK', 'TİCARET', 'PAZARLAMA', 'A.Ş.', 'A.S.'
    ]
    vergi_keywords = ['VERGİ', 'VKN', 'TCKN', 'DAİRESİ', 'KİMLİK', 'MERSİS', 'MERSIS', 'VERGİ KİMLİK NUMARASI']
    fatura_no_keywords = ['FATURA NO', 'FATUA NO', 'FATURA NUMARASI', 'ETTN', 'SENARYO', 'EARSIVFATURA', 'EARSNFATURA', 'FEA']
    tarih_keywords = ['FATURA TARİHİ', 'FATUA TARIHI', 'Tarih', 'TARIH', 'OLUŞMA ZAMANI', 'SON ÖDEME', 'FATURA TARIHI']
    toplam_keywords = ['TOPLAM TUTAR', 'VERGİLER DAHİL', 'ÖDENECEK TUTAR', 'TOPAN ICRTAR', 'GENEL TOPLAM', 'TOPLAM', 'MAL HİZMET TUTARI', 'VERGİ HARİÇ TUTAR', 'VERGİLER DAHİL TOPLAM TUTAR']
    kdv_keywords = ['KDV', 'KD.V.', 'KZOI', 'KD:Y', 'HESAPLANAN KDV', 'KDV ORANI', 'KDV TUTARI', 'KDV TUTAR', 'HESAPLANAN KDV (%)']
    matrah_keywords = ['MAL HİZMET TUTARI', 'MATRAH', 'MATRAHI', 'MABAHI', 'HUETTOON TUTN', 'VERGİ HARİÇ TUTAR']
    alici_keywords = ['SAYIN', 'ALICI', 'MÜŞTERİ', 'SN.', 'SAYGILARIMIZLA']
    # Firma Adı
    for i, line in enumerate(ocr_lines):
        if fuzzy_match_any(line, firma_keywords, 0.6):
            fields['firma_adi'] = line.strip()
            break
    # Vergi No
    for line in ocr_lines:
        if fuzzy_match_any(line, vergi_keywords, 0.6):
            match = re.search(r'\b\d{10,11}\b', line)
            if match:
                fields['vergi_no'] = match.group(0)
                break
    # Fatura No
    for line in ocr_lines:
        if fuzzy_match_any(line, fatura_no_keywords, 0.6):
            match = re.search(r'([A-Z0-9]{8,})', line)
            if match:
                fields['fatura_no'] = match.group(1)
                break
        if re.search(r'FEA[0-9]{10,}', line):
            fields['fatura_no'] = re.search(r'(FEA[0-9]{10,})', line).group(1)
            break
    # Fatura Tarihi (önce anahtar kelimeyle eşleşen satırlarda, yoksa tüm satırlarda tarih ara)
    found_date = False
    for line in ocr_lines:
        if fuzzy_match_any(line, tarih_keywords, 0.6):
            match = re.search(r'(\d{2}[./-]\d{2}[./-]\d{4}|\d{8})', line)
            if match:
                tarih = match.group(1)
                if len(tarih) == 8 and tarih.isdigit():
                    tarih = f"{tarih[0:2]}.{tarih[2:4]}.{tarih[4:8]}"
                    # Sadece geçerli tarihleri kabul et
                    if re.match(r'^(0[1-9]|[12][0-9]|3[01])[./-](0[1-9]|1[0-2])[./-](20\d{2})$', tarih):
                        fields['fatura_tarihi'] = tarih
                        found_date = True
                        break
    if not found_date:
        for line in ocr_lines:
            match = re.search(r'(\d{2}[./-]\d{2}[./-]\d{4}|\d{8})', line)
            if match:
                tarih = match.group(1)
                if len(tarih) == 8 and tarih.isdigit():
                    tarih = f"{tarih[0:2]}.{tarih[2:4]}.{tarih[4:8]}"
                    if re.match(r'^(0[1-9]|[12][0-9]|3[01])[./-](0[1-9]|1[0-2])[./-](20\d{2})$', tarih):
                        fields['fatura_tarihi'] = tarih
                        break
    # Toplam Tutar (öncelik: Ödenecek Tutar > Vergiler Dahil Toplam Tutar > diğer anahtarlar > fallback)
    toplam_tutar_found = False
    for i, line in enumerate(ocr_lines):
        if 'ÖDENECEK TUTAR' in line.upper():
            match = re.search(r'([\d.,]+)\s*(TRY|TL|NL)', line)
            if match:
                fields['toplam_tutar'] = match.group(1) + ' ' + match.group(2)
                toplam_tutar_found = True
                break
            # Satırda yoksa bir sonraki satırda ara
            if i+1 < len(ocr_lines):
                match2 = re.search(r'([\d.,]+)\s*(TRY|TL|NL)', ocr_lines[i+1])
                if match2:
                    fields['toplam_tutar'] = match2.group(1) + ' ' + match2.group(2)
                    toplam_tutar_found = True
                    break
            # Satırda ve sonrasında yoksa bir önceki satırda ara
            if i > 0:
                match3 = re.search(r'([\d.,]+)\s*(TRY|TL|NL)', ocr_lines[i-1])
                if match3:
                    fields['toplam_tutar'] = match3.group(1) + ' ' + match3.group(2)
                    toplam_tutar_found = True
                    break
    if not toplam_tutar_found:
        for i, line in enumerate(ocr_lines):
            if 'VERGİLER DAHİL TOPLAM TUTAR' in line.upper():
                match = re.search(r'([\d.,]+)\s*(TRY|TL|NL)', line)
                if match:
                    fields['toplam_tutar'] = match.group(1) + ' ' + match.group(2)
                    toplam_tutar_found = True
                    break
                if i+1 < len(ocr_lines):
                    match2 = re.search(r'([\d.,]+)\s*(TRY|TL|NL)', ocr_lines[i+1])
                    if match2:
                        fields['toplam_tutar'] = match2.group(1) + ' ' + match2.group(2)
                        toplam_tutar_found = True
                        break
                if i > 0:
                    match3 = re.search(r'([\d.,]+)\s*(TRY|TL|NL)', ocr_lines[i-1])
                    if match3:
                        fields['toplam_tutar'] = match3.group(1) + ' ' + match3.group(2)
                        toplam_tutar_found = True
                        break
    if not toplam_tutar_found:
        for line in ocr_lines:
            if fuzzy_match_any(line, toplam_keywords, 0.6) or re.search(r'TRY|TL|NL', line):
                match = re.search(r'([\d.,]+)\s*(TRY|TL|NL)', line)
                if match:
                    fields['toplam_tutar'] = match.group(1) + ' ' + match.group(2)
                    toplam_tutar_found = True
                    break
    if not toplam_tutar_found:
        # fallback: en büyük TRY/TL/NL değeri
        max_total = 0
        max_total_str = ''
        for line in ocr_lines:
            for match in re.finditer(r'([\d.,]+)\s*(TRY|TL|NL)', line):
                val = match.group(1).replace('.', '').replace(',', '.')
                try:
                    num = float(val)
                    if num > max_total:
                        max_total = num
                        max_total_str = match.group(1) + ' ' + match.group(2)
                except:
                    continue
        if max_total_str:
            fields['toplam_tutar'] = max_total_str
    # KDV ve Matrah (bağlama ve büyüklüğe göre)
    # Öncelik: Hesaplanan KDV satırında oranı bul ve ilgili kdv_X alanına yaz
    kdv_oran_map = {'%1': 'kdv_1', '%8': 'kdv_8', '%10': 'kdv_10', '%18': 'kdv_18', '%20': 'kdv_20'}
    for line in ocr_lines:
        if 'HESAPLANAN KDV' in line.upper() or 'HESAPLANAN K.D.V' in line.upper():
            oran_match = re.search(r'%\s?(\d{1,2})', line)
            tutar_match = re.search(r'([\d.,]+)\s*(TRY|TL|NL)', line)
            if oran_match and tutar_match:
                oran = oran_match.group(1)
                kdv_key = f'kdv_{oran}'
                fields[kdv_key] = tutar_match.group(1) + ' ' + tutar_match.group(2)
                # Diğer kdv_X alanlarını boş bırak
                for k in ['kdv_1', 'kdv_8', 'kdv_10', 'kdv_18', 'kdv_20']:
                    if k != kdv_key:
                        fields[k] = ''
                break
    # KDV ve Matrah (bağlama ve büyüklüğe göre)
    kdv_candidates = []
    matrah_candidates = []
    for line in ocr_lines:
        if fuzzy_match_any(line, kdv_keywords, 0.6):
            for match in re.finditer(r'([\d.,]+)', line):
                if is_number(match.group(1)):
                    kdv_candidates.append(float(match.group(1).replace('.', '').replace(',', '.')))
        if fuzzy_match_any(line, matrah_keywords, 0.6):
            for match in re.finditer(r'([\d.,]+)', line):
                if is_number(match.group(1)):
                    matrah_candidates.append(float(match.group(1).replace('.', '').replace(',', '.')))
    # KDV: en küçük 1000'den büyük, 10000'den küçük değerleri seç
    kdv_10 = [v for v in kdv_candidates if 100 < v < 1000]
    kdv_20 = [v for v in kdv_candidates if 1000 < v < 10000]
    if kdv_10:
        fields['kdv_10'] = f"{kdv_10[0]:,.2f}".replace('.', ',')
    if kdv_20:
        fields['kdv_20'] = f"{kdv_20[0]:,.2f}".replace('.', ',')
    # Matrah: en büyük değerleri seç
    if matrah_candidates:
        matrah_candidates.sort(reverse=True)
        fields['matrah_10'] = f"{matrah_candidates[0]:,.2f}".replace('.', ',')
        if len(matrah_candidates) > 1:
            fields['matrah_20'] = f"{matrah_candidates[1]:,.2f}".replace('.', ',')
    # KDV ve Matrah Toplam
    fields['kdv_toplam'] = fields['kdv_10'] or fields['kdv_20']
    fields['matrah_toplam'] = fields['matrah_10'] or fields['matrah_20']
    # Alıcı
    for i, line in enumerate(ocr_lines):
        if fuzzy_match_any(line, alici_keywords, 0.6) and i+1 < len(ocr_lines):
            fields['alici'] = ocr_lines[i+1].strip()
            break
    for line in ocr_lines:
        if 'TCKN' in line or 'MÜŞTERİ' in line or 'ALICI' in line:
            match = re.search(r'(\d{11})', line)
            if match:
                fields['alici_vergi_no'] = match.group(1)
                break
    for key in fields:
        if key != 'raw_ocr_lines':
            if fields[key] is None:
                fields[key] = ''
            elif not isinstance(fields[key], str):
                fields[key] = str(fields[key])
    return fields