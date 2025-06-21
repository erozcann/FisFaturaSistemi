import easyocr
import cv2
import numpy as np
import logging
import os
import re
from PIL import Image, ImageEnhance, ImageFilter
from typing import List, Optional, Tuple
import threading
from pathlib import Path

# Loglama ayarları
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s',
    handlers=[
        logging.FileHandler('ocr_service.log', encoding='utf-8'),
        logging.StreamHandler()
    ]
)
logger = logging.getLogger(__name__)

# OCR reader'ı singleton olarak oluştur
_reader = None
_reader_lock = threading.Lock()

# Desteklenen görüntü formatları
SUPPORTED_FORMATS = {'.jpg', '.jpeg', '.png', '.bmp', '.tiff', '.tif', '.webp'}


def get_reader(languages: List[str] = None, use_gpu: bool = False) -> easyocr.Reader:
    """
    EasyOCR reader'ını singleton olarak döndürür

    Args:
        languages: Desteklenecek diller (varsayılan: ['tr', 'en'])
        use_gpu: GPU kullanımı (varsayılan: False)

    Returns:
        EasyOCR Reader instance
    """
    global _reader

    if languages is None:
        languages = ['tr', 'en']

    with _reader_lock:
        if _reader is None:
            try:
                logger.info(f"🚀 EasyOCR reader başlatılıyor... Diller: {languages}, GPU: {use_gpu}")
                _reader = easyocr.Reader(languages, gpu=use_gpu, verbose=False)
                logger.info("✅ EasyOCR reader hazır")
            except Exception as e:
                logger.error(f"❌ EasyOCR reader hatası: {e}")
                raise
    return _reader


def normalize_text(text: str) -> str:
    """
    OCR çıktısını normalize eder

    Args:
        text: Ham OCR çıktısı

    Returns:
        Normalize edilmiş metin
    """
    if not text or not isinstance(text, str):
        return ""

    try:
        # Başlangıç ve bitiş boşluklarını temizle
        text = text.strip()

        # Türkçe karakter OCR hatalarını düzelt
        char_fixes = {
            # OCR sıklıkla bu karakterleri karıştırır
            'İ': 'I', 'ı': 'i', 'Ğ': 'G', 'ğ': 'g',
            'Ü': 'U', 'ü': 'u', 'Ş': 'S', 'ş': 's',
            'Ö': 'O', 'ö': 'o', 'Ç': 'C', 'ç': 'c'
        }

        # Türkçe karakter düzeltmelerini uygula
        for old_char, new_char in char_fixes.items():
            text = text.replace(old_char, new_char)

        # Yaygın OCR hatalarını düzelt (sadece sayı içeren metinlerde)
        if re.search(r'\d', text):
            ocr_fixes = {
                'l': '1',  # küçük L -> 1
                'I': '1',  # büyük i -> 1
                'O': '0',  # büyük o -> 0
                'S': '5',  # S -> 5 (context'e göre)
                'B': '8',  # B -> 8 (context'e göre)
                'G': '6',  # G -> 6 (context'e göre)
                'Z': '2',  # Z -> 2 (context'e göre)
            }

            for old_char, new_char in ocr_fixes.items():
                text = text.replace(old_char, new_char)

        # Çoklu boşlukları tek boşluğa çevir
        text = re.sub(r'\s+', ' ', text)

        # Gereksiz noktalama işaretlerini temizle
        text = re.sub(r'[^\w\s\.,;:!?\-\(\)]+', '', text)

        return text.strip()

    except Exception as e:
        logger.error(f"Metin normalizasyonu hatası: {str(e)}")
        return text


def validate_image(image_path: str) -> bool:
    """
    Görüntü dosyasının geçerli olup olmadığını kontrol eder

    Args:
        image_path: Görüntü dosyası yolu

    Returns:
        Geçerli ise True, değilse False
    """
    try:
        path = Path(image_path)

        # Dosya var mı?
        if not path.exists():
            logger.error(f"❌ Dosya bulunamadı: {image_path}")
            return False

        # Desteklenen format mı?
        if path.suffix.lower() not in SUPPORTED_FORMATS:
            logger.error(f"❌ Desteklenmeyen format: {path.suffix}")
            return False

        # Dosya boyutu kontrol (çok büyük dosyaları engelle)
        file_size = path.stat().st_size
        max_size = 50 * 1024 * 1024  # 50MB
        if file_size > max_size:
            logger.error(f"❌ Dosya çok büyük: {file_size / 1024 / 1024:.1f}MB > 50MB")
            return False

        # Görüntü açılabilir mi?
        img = cv2.imread(str(path))
        if img is None:
            logger.error(f"❌ Görüntü açılamadı: {image_path}")
            return False

        return True

    except Exception as e:
        logger.error(f"❌ Görüntü doğrulama hatası: {str(e)}")
        return False


def preprocess_image(image_path: str, output_path: Optional[str] = None) -> bool:
    """
    Görüntüyü OCR için optimize eder

    Args:
        image_path: Giriş görüntüsü yolu
        output_path: Çıkış yolu (None ise orijinal dosya üzerine yazar)

    Returns:
        İşlem başarılı ise True
    """
    try:
        logger.info(f"🖼️  Görüntü ön işleme başlıyor: {image_path}")

        # Görüntü doğrulama
        if not validate_image(image_path):
            return False

        # OpenCV ile görüntüyü oku
        img = cv2.imread(image_path)

        # Görüntü boyutunu optimize et
        img = _resize_image(img)

        # Görüntü kalitesini artır
        processed_img = _enhance_image(img)

        # Çıkış yolunu belirle
        if output_path is None:
            output_path = image_path.replace('.', '_processed.')

        # İşlenmiş görüntüyü kaydet
        success = cv2.imwrite(output_path, processed_img)
        if not success:
            logger.error(f"❌ İşlenmiş görüntü kaydedilemedi: {output_path}")
            return False

        # Orijinal dosyayı değiştir (eğer output_path belirtilmemişse)
        if output_path.endswith('_processed.'):
            os.replace(output_path, image_path)

        logger.info("✅ Görüntü ön işleme tamamlandı")
        return True

    except Exception as e:
        logger.error(f"❌ Görüntü ön işleme hatası: {str(e)}")
        return False


def _resize_image(img: np.ndarray) -> np.ndarray:
    """Görüntüyü optimal boyutlara getirir"""
    height, width = img.shape[:2]
    logger.info(f"📏 Orijinal boyut: {width}x{height}")

    # Çok küçükse büyüt
    if width < 800 or height < 600:
        scale_factor = max(800 / width, 600 / height)
        new_width = int(width * scale_factor)
        new_height = int(height * scale_factor)
        img = cv2.resize(img, (new_width, new_height), interpolation=cv2.INTER_LANCZOS4)
        logger.info(f"📈 Görüntü büyütüldü: {new_width}x{new_height}")

    # Çok büyükse küçült
    elif width > 4000 or height > 4000:
        scale_factor = min(4000 / width, 4000 / height)
        new_width = int(width * scale_factor)
        new_height = int(height * scale_factor)
        img = cv2.resize(img, (new_width, new_height), interpolation=cv2.INTER_AREA)
        logger.info(f"📉 Görüntü küçültüldü: {new_width}x{new_height}")

    return img


def _enhance_image(img: np.ndarray) -> np.ndarray:
    """Görüntü kalitesini OCR için optimize eder"""
    # Gri tonlamaya çevir
    if len(img.shape) == 3:
        gray = cv2.cvtColor(img, cv2.COLOR_BGR2GRAY)
    else:
        gray = img.copy()

    # Gürültü azaltma
    denoised = cv2.fastNlMeansDenoising(gray, h=10, templateWindowSize=7, searchWindowSize=21)

    # Kontrast artırma (CLAHE)
    clahe = cv2.createCLAHE(clipLimit=3.0, tileGridSize=(8, 8))
    enhanced = clahe.apply(denoised)

    # Gamma düzeltmesi
    gamma = 1.2
    lookup_table = np.array([((i / 255.0) ** gamma) * 255 for i in np.arange(0, 256)]).astype("uint8")
    gamma_corrected = cv2.LUT(enhanced, lookup_table)

    # Adaptif eşikleme
    binary = cv2.adaptiveThreshold(
        gamma_corrected, 255, cv2.ADAPTIVE_THRESH_GAUSSIAN_C,
        cv2.THRESH_BINARY, 11, 2
    )

    # Morfolojik işlemler (gürültü temizleme)
    kernel = np.ones((2, 2), np.uint8)
    cleaned = cv2.morphologyEx(binary, cv2.MORPH_CLOSE, kernel)
    cleaned = cv2.morphologyEx(cleaned, cv2.MORPH_OPEN, kernel)

    return cleaned


def process_ocr(image_path: str, languages: List[str] = None, preprocess: bool = True) -> List[str]:
    """
    Ana OCR işlemi

    Args:
        image_path: Görüntü dosyası yolu
        languages: Desteklenecek diller
        preprocess: Ön işleme yapılsın mı

    Returns:
        Çıkarılan metin satırları listesi
    """
    try:
        logger.info(f"🔍 OCR işlemi başlıyor: {image_path}")

        # Görüntü doğrulama
        if not validate_image(image_path):
            return []

        # Görüntü ön işleme
        if preprocess and not preprocess_image(image_path):
            logger.warning("⚠️  Görüntü ön işleme başarısız, orijinal görüntü kullanılacak")

        # EasyOCR reader'ını al
        reader = get_reader(languages)

        # OCR işlemini çalıştır
        results = _run_ocr(reader, image_path)

        # Sonuçları işle ve temizle
        processed_lines = _process_ocr_results(results)

        logger.info(f"✅ OCR tamamlandı. {len(results)} ham -> {len(processed_lines)} işlenmiş satır")

        return processed_lines

    except Exception as e:
        logger.error(f"❌ OCR işlemi hatası: {str(e)}")
        import traceback
        logger.error(f"Hata detayı: {traceback.format_exc()}")
        return []


def _run_ocr(reader: easyocr.Reader, image_path: str) -> List:
    """OCR işlemini çalıştırır"""
    # OCR parametreleri
    ocr_params = {
        'detail': 0,  # Sadece metni döndür
        'paragraph': False,  # Satır satır oku
        'width_ths': 0.7,
        'height_ths': 0.7,
        'decoder': 'greedy',
        'beamWidth': 5,
        'batch_size': 1,
    }

    logger.info("🤖 EasyOCR ile metin çıkarımı başlıyor...")
    results = reader.readtext(image_path, **ocr_params)

    # Eğer sonuç boşsa, daha düşük eşiklerle tekrar dene
    if not results:
        logger.warning("⚠️  OCR sonucu boş, alternatif yöntem deneniyor...")
        ocr_params.update({'width_ths': 0.5, 'height_ths': 0.5})
        results = reader.readtext(image_path, **ocr_params)

    return results


def _process_ocr_results(results: List) -> List[str]:
    """OCR sonuçlarını işler ve temizler"""
    processed_lines = []

    for result in results:
        # Metin çıkar
        if isinstance(result, str):
            text = result
        else:
            text = result[1] if len(result) > 1 else str(result)

        # Metni normalize et
        normalized = normalize_text(text)

        # Boş veya çok kısa metinleri filtrele
        if normalized and len(normalized.strip()) > 1:
            processed_lines.append(normalized)

    # Benzer satırları temizle
    unique_lines = _remove_duplicates(processed_lines)

    # Debug çıktısı
    if unique_lines:
        logger.info("📝 İlk 5 satır:")
        for i, line in enumerate(unique_lines[:5]):
            logger.info(f"  {i + 1}: {line}")

    return unique_lines


def _remove_duplicates(lines: List[str]) -> List[str]:
    """Benzer satırları kaldırır"""
    unique_lines = []

    for line in lines:
        line_lower = line.lower().strip()

        # Mevcut satırlarla benzerlik kontrolü
        is_duplicate = False
        for existing in unique_lines:
            existing_lower = existing.lower().strip()

            # Tam eşleşme veya bir satır diğerinin alt kümesi ise
            if (line_lower == existing_lower or
                    line_lower in existing_lower or
                    existing_lower in line_lower):
                is_duplicate = True
                break

        if not is_duplicate:
            unique_lines.append(line)

    return unique_lines


def extract_text_from_image(image_path: str, **kwargs) -> str:
    """
    Görüntüden metin çıkarır ve tek string olarak döndürür

    Args:
        image_path: Görüntü dosyası yolu
        **kwargs: process_ocr fonksiyonuna geçirilecek ek parametreler

    Returns:
        Çıkarılan metin (satırlar arası yeni satır karakteri ile ayrılmış)
    """
    lines = process_ocr(image_path, **kwargs)
    return '\n'.join(lines)


def batch_process_images(image_paths: List[str], **kwargs) -> dict:
    """
    Birden fazla görüntüyü toplu olarak işler

    Args:
        image_paths: Görüntü dosyası yolları listesi
        **kwargs: process_ocr fonksiyonuna geçirilecek ek parametreler

    Returns:
        {dosya_yolu: metin_listesi} şeklinde dictionary
    """
    results = {}

    for image_path in image_paths:
        logger.info(f"📂 Toplu işlem: {image_path}")
        try:
            results[image_path] = process_ocr(image_path, **kwargs)
        except Exception as e:
            logger.error(f"❌ {image_path} işlenirken hata: {str(e)}")
            results[image_path] = []

    return results


def cleanup_temp_files(directory: str = ".") -> None:
    """
    Geçici işlenmiş görüntü dosyalarını temizler

    Args:
        directory: Temizlenecek dizin
    """
    try:
        temp_pattern = "*_processed.*"
        temp_files = Path(directory).glob(temp_pattern)

        for temp_file in temp_files:
            temp_file.unlink()
            logger.info(f"🗑️  Geçici dosya silindi: {temp_file}")

    except Exception as e:
        logger.error(f"❌ Geçici dosya temizleme hatası: {str(e)}")


# Ana fonksiyon (test için)
def main():
    """Test fonksiyonu"""
    import sys

    if len(sys.argv) < 2:
        print("Kullanım: python ocr_utils.py <görüntü_dosyası>")
        return

    image_path = sys.argv[1]

    # OCR işlemi
    results = process_ocr(image_path)

    # Sonuçları yazdır
    print("\n" + "=" * 50)
    print("OCR SONUÇLARI")
    print("=" * 50)

    if results:
        for i, line in enumerate(results, 1):
            print(f"{i:2d}: {line}")
    else:
        print("Metin bulunamadı.")

    print("=" * 50)


if __name__ == "__main__":
    main()