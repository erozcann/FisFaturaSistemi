import re
import logging

logger = logging.getLogger(__name__)

def normalize_text(text):
    """
    Metni standart bir formata getirir.
    - Büyük harfe çevirir.
    - Türkçe karakterleri düzeltir.
    - Fazla boşlukları temizler.
    """
    if not text:
        return ""
    
    text = text.upper()
    
    replacements = {
        'İ': 'I', 'Ş': 'S', 'Ğ': 'G', 'Ü': 'U', 'Ö': 'O', 'Ç': 'C',
        'ı': 'i', 'ş': 's', 'ğ': 'g', 'ü': 'u', 'ö': 'o', 'ç': 'c'
    }
    for old, new in replacements.items():
        text = text.replace(old, new)
        
    return ' '.join(text.split())

def normalize_monetary(value_str):
    """
    Parasal değeri '123,45' formatına getirir.
    """
    if not value_str:
        return "0,00"
    
    # Sadece rakamlar, virgül ve nokta kalacak şekilde temizle
    cleaned_value = re.sub(r'[^\d,.]', '', value_str)
    
    # Noktayı virgüle çevir
    cleaned_value = cleaned_value.replace('.', ',')
    
    # Eğer birden fazla virgül varsa, sondan bir öncekini ondalık ayırıcı olarak kabul et
    parts = cleaned_value.split(',')
    if len(parts) > 2:
        cleaned_value = "".join(parts[:-1]) + "," + parts[-1]
        
    # Ondalık kısmı 2 haneye tamamla
    if ',' in cleaned_value:
        integer_part, decimal_part = cleaned_value.split(',')
        decimal_part = (decimal_part + '00')[:2]
        return f"{integer_part},{decimal_part}"
    else:
        return f"{cleaned_value},00"

def normalize_date(date_str):
    """
    Tarihi 'YYYY-MM-DD' formatına getirir.
    """
    if not date_str:
        return ""
    
    match = re.search(r'(\d{2})[./-](\d{2})[./-](\d{4})', date_str)
    if match:
        day, month, year = match.groups()
        return f"{year}-{month}-{day}"
    
    match = re.search(r'(\d{4})[./-](\d{2})[./-](\d{2})', date_str)
    if match:
        year, month, day = match.groups()
        return f"{year}-{month}-{day}"
        
    return date_str # Eşleşmezse orijinalini döndür

def normalize_vkn(vkn_str):
    """
    VKN/TCKN'yi sadece rakamlardan oluşacak şekilde temizler.
    """
    if not vkn_str:
        return ""
    return re.sub(r'\D', '', vkn_str)

def normalize_text(text):
    try:
        if not text:
            return ""

        # Türkçe karakter düzeltmeleri
        replacements = {
            'İ': 'I', 'ı': 'i', 'Ğ': 'G', 'ğ': 'g',
            'Ü': 'U', 'ü': 'u', 'Ş': 'S', 'ş': 's',
            'Ö': 'O', 'ö': 'o', 'Ç': 'C', 'ç': 'c'
        }
        
        # Karakter değişimleri
        for old, new in replacements.items():
            text = text.replace(old, new)
        
        # Gereksiz boşlukları temizle
        text = re.sub(r'\s+', ' ', text)
        
        # Özel karakterleri temizle
        text = re.sub(r'[^\w\s.,:;()-]', '', text)
        
        # Sayısal değerlerdeki nokta ve virgülleri düzelt
        text = re.sub(r'(\d+)[.,](\d+)', r'\1.\2', text)
        
        # Fatura numarası formatını düzelt
        text = re.sub(r'([A-Z]{1,3})[-\s]?(\d+)', r'\1\2', text)
        
        # Tarih formatını düzelt
        text = re.sub(r'(\d{2})[./-](\d{2})[./-](\d{4})', r'\1.\2.\3', text)
        
        # Trim ve uppercase
        text = text.strip().upper()
        
        return text
    except Exception as e:
        logger.error(f"Metin normalizasyonu hatası: {str(e)}")
        return text
