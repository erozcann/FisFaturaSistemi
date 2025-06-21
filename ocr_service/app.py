from flask import Flask, request, jsonify
from utils import process_ocr
from field_detector import detect_fields
from receipt_detector import detect_receipt_fields
import os
import logging
from werkzeug.utils import secure_filename
from datetime import datetime
import traceback
import re

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

app = Flask(__name__)

# Yapılandırma
UPLOAD_FOLDER = 'uploads'
ALLOWED_EXTENSIONS = {'png', 'jpg', 'jpeg', 'pdf'}
MAX_CONTENT_LENGTH = 16 * 1024 * 1024  # 16MB max-limit

app.config['UPLOAD_FOLDER'] = UPLOAD_FOLDER
app.config['MAX_CONTENT_LENGTH'] = MAX_CONTENT_LENGTH

# Uploads klasörünü oluştur
os.makedirs(UPLOAD_FOLDER, exist_ok=True)


def allowed_file(filename):
    """Dosya uzantısı kontrolü"""
    return '.' in filename and filename.rsplit('.', 1)[1].lower() in ALLOWED_EXTENSIONS


@app.route("/health", methods=["GET"])
def health_check():
    """Servis sağlık kontrolü"""
    return jsonify({"status": "healthy", "service": "OCR Service"}), 200


@app.route("/ocr", methods=["POST"])
def ocr_endpoint():
    """Ana OCR endpoint'i"""
    try:
        # Dosya kontrolü
        if "file" not in request.files:
            logger.error("❌ Dosya yüklenmedi")
            return jsonify({"error": "Dosya yüklenmedi", "code": "NO_FILE"}), 400

        file = request.files["file"]
        if not file.filename:
            logger.error("❌ Dosya adı boş")
            return jsonify({"error": "Dosya adı boş", "code": "EMPTY_FILENAME"}), 400

        if not allowed_file(file.filename):
            logger.error(f"❌ Geçersiz dosya uzantısı: {file.filename}")
            return jsonify({
                "error": "Geçersiz dosya formatı. Sadece PNG, JPG, JPEG, PDF desteklenir",
                "code": "INVALID_FORMAT"
            }), 400

        # Güvenli dosya adı oluştur
        timestamp = datetime.now().strftime("%Y%m%d%H%M%S%f")
        original_name = secure_filename(file.filename)
        filename = f"{timestamp}_{original_name}"
        filepath = os.path.join(app.config["UPLOAD_FOLDER"], filename)

        logger.info(f"📁 Dosya kaydediliyor: {filename}")
        file.save(filepath)

        try:
            # OCR işlemi
            logger.info("🔍 OCR işlemi başlıyor...")
            lines = process_ocr(filepath)

            logger.info(f"📄 OCR tamamlandı. {len(lines)} satır çıkarıldı.")

            # Debug için OCR çıktısını göster
            print("\n" + "=" * 50)
            print("📄 OCR ÇIKTISI:")
            print("=" * 50)
            for i, line in enumerate(lines, 1):
                print(f"{i:2d}: {line}")
            print("=" * 50 + "\n")

        except Exception as ocr_err:
            logger.error(f"❌ OCR hatası: {ocr_err}")
            logger.error(traceback.format_exc())
            return jsonify({
                "error": f"OCR işlemi sırasında hata oluştu: {str(ocr_err)}",
                "code": "OCR_ERROR"
            }), 500

        try:
            # Alan çıkarma işlemi
            logger.info("🎯 Alan çıkarma işlemi başlıyor...")
            result = detect_fields(lines)

            # Orijinal OCR çıktısını da ekle
            result["raw_ocr_lines"] = lines
            result["processing_info"] = {
                "total_lines": len(lines),
                "filename": original_name,
                "timestamp": timestamp
            }

            logger.info("✅ Alan çıkarma tamamlandı")

            # Debug için sonuçları göster
            print("\n" + "=" * 50)
            print("🎯 ÇIKARILAN ALANLAR:")
            print("=" * 50)
            for key, value in result.items():
                if key not in ["raw_ocr_lines", "processing_info"] and value:
                    print(f"{key:25}: {value}")
            print("=" * 50 + "\n")

        except Exception as extract_err:
            logger.error(f"❌ Alan çıkarım hatası: {extract_err}")
            logger.error(traceback.format_exc())

            # Hata durumunda boş sonuç döndür
            result = {
                'firma_adi': '', 'vergi_no': '', 'fatura_no': '', 'fatura_tarihi': '',
                'toplam_tutar': '', 'odeme_turu': '', 'icerik_turu': '',
                'fatura_gonderen': '', 'fatura_gonderen_vergi_no': '',
                'fatura_alan': '', 'fatura_alan_vergi_no': '',
                'kdv_0': '', 'kdv_1': '', 'kdv_8': '', 'kdv_10': '', 'kdv_18': '', 'kdv_20': '',
                'matrah_0': '', 'matrah_1': '', 'matrah_8': '', 'matrah_10': '', 'matrah_18': '', 'matrah_20': '',
                'kdv_toplam': '0,00', 'matrah_toplam': '0,00',
                'fatura_turu': 'E-ARŞİV', 'para_birimi': 'TRY',
                'raw_ocr_lines': lines,
                'error': f"Alan çıkarımı sırasında hata: {str(extract_err)}",
                'code': 'EXTRACTION_ERROR'
            }

        # Geçici dosyayı sil
        try:
            os.remove(filepath)
            logger.info(f"🗑️ Geçici dosya silindi: {filename}")
        except Exception as del_err:
            logger.warning(f"⚠️ Geçici dosya silinemedi: {del_err}")

        # Toplam tutar için öncelikli satırlar
        total_amount_keywords = ['Ödenecek Tutar', 'Vergiler Dahil Toplam Tutar', 'Toplam Tutar', 'Genel Toplam']
        total_amount = None
        for keyword in total_amount_keywords:
            for line in lines:
                if keyword in line:
                    # Satırın hemen altındaki veya üstündeki satırda değer olabilir
                    idx = lines.index(line)
                    if idx + 1 < len(lines):
                        potential_value = lines[idx + 1].strip()
                        if re.search(r'\d+[.,]\d+', potential_value):
                            total_amount = potential_value
                            break
                    if idx > 0:
                        potential_value = lines[idx - 1].strip()
                        if re.search(r'\d+[.,]\d+', potential_value):
                            total_amount = potential_value
                            break
            if total_amount:
                break
        if not total_amount:
            # Eğer öncelikli satırlarda bulunamazsa, en büyük değeri al
            total_amount = max([float(re.search(r'\d+[.,]\d+', line).group().replace(',', '.')) for line in lines if re.search(r'\d+[.,]\d+', line)], default=None)

        # KDV için 'Hesaplanan KDV' satırında parantez içindeki oran ve değeri yakala
        kdv_keywords = ['Hesaplanan KDV', 'Hesaplanan K.D.V']
        kdv = None
        for keyword in kdv_keywords:
            for line in lines:
                if keyword in line:
                    # Parantez içindeki oran ve değeri yakala
                    match = re.search(r'\((\d+)%\)\s*(\d+[.,]\d+)', line)
                    if match:
                        kdv_rate = int(match.group(1))
                        kdv_value = float(match.group(2).replace(',', '.'))
                        kdv = kdv_value
                        break
            if kdv:
                break

        # Matrah için 'Matrah' veya 'KDV Matrahı' satırını ara
        matrah_keywords = ['Matrah', 'KDV Matrahı']
        matrah = None
        for keyword in matrah_keywords:
            for line in lines:
                if keyword in line:
                    # Satırın hemen altındaki veya üstündeki satırda değer olabilir
                    idx = lines.index(line)
                    if idx + 1 < len(lines):
                        potential_value = lines[idx + 1].strip()
                        if re.search(r'\d+[.,]\d+', potential_value):
                            matrah = potential_value
                            break
                    if idx > 0:
                        potential_value = lines[idx - 1].strip()
                        if re.search(r'\d+[.,]\d+', potential_value):
                            matrah = potential_value
                            break
            if matrah:
                break

        # Sonuçları güncelle (SADECE DOLUYSA ÜSTÜNE YAZ)
        if total_amount is not None:
            result["toplam_tutar"] = total_amount
        # kdv ve matrah mapping'de yok, onları set etme

        # Tüm alanları string'e çevir (raw_ocr_lines hariç)
        for key in result:
            if key != "raw_ocr_lines":
                if result[key] is None:
                    result[key] = ''
                elif not isinstance(result[key], str):
                    result[key] = str(result[key])

        # Mapping ve PascalCase key'lerle JSON oluştur (FONKSİYONUN SONUNDA!)
        mapping = {
            "firma_adi": "FirmaAliciAdi",
            "vergi_no": "FirmaAliciVergiNo",
            "fatura_no": "FaturaNo",
            "fatura_tarihi": "FaturaTarihi",
            "toplam_tutar": "ToplamTutar",
            "kdv_0": "Kdv0",
            "kdv_1": "Kdv1",
            "kdv_8": "Kdv8",
            "kdv_10": "Kdv10",
            "kdv_18": "Kdv18",
            "kdv_20": "Kdv20",
            "matrah_0": "Matrah0",
            "matrah_1": "Matrah1",
            "matrah_8": "Matrah8",
            "matrah_10": "Matrah10",
            "matrah_18": "Matrah18",
            "matrah_20": "Matrah20",
            "kdv_toplam": "KdvToplamTutar",
            "matrah_toplam": "MatrahToplamTutar",
            "icerik_turu": "IcerikTuru",
            "odeme_turu": "OdemeTuru",
            "raw_ocr_lines": "RawOcrLines"
        }
        mapped_result = {}
        for k, v in mapping.items():
            value = result.get(k, "")
            if value is None:
                value = ""
            mapped_result[v] = value
        # Tüm alanları string'e çevir (RawOcrLines hariç)
        for key in mapped_result:
            if key != "RawOcrLines":
                if mapped_result[key] is None:
                    mapped_result[key] = ""
                elif not isinstance(mapped_result[key], str):
                    mapped_result[key] = str(mapped_result[key])
        print("JSON RESPONSE:", mapped_result)
        return jsonify(mapped_result)

    except Exception as e:
        logger.error(f"❌ Genel hata: {e}")
        logger.error(traceback.format_exc())
        return jsonify({
            "error": f"Genel sistem hatası: {str(e)}",
            "code": "SYSTEM_ERROR"
        }), 500


@app.route("/receipt", methods=["POST"])
def receipt_ocr_endpoint():
    """Endpoint for receipt OCR"""
    try:
        # File validation (similar to ocr_endpoint)
        if "file" not in request.files:
            logger.error("❌ Dosya yüklenmedi (receipt)")
            return jsonify({"error": "Dosya yüklenmedi", "code": "NO_FILE"}), 400

        file = request.files["file"]
        if not file.filename:
            logger.error("❌ Dosya adı boş (receipt)")
            return jsonify({"error": "Dosya adı boş", "code": "EMPTY_FILENAME"}), 400

        if not allowed_file(file.filename):
            logger.error(f"❌ Geçersiz dosya uzantısı: {file.filename} (receipt)")
            return jsonify({
                "error": "Geçersiz dosya formatı. Sadece PNG, JPG, JPEG, PDF desteklenir",
                "code": "INVALID_FORMAT"
            }), 400

        # Secure filename and save
        timestamp = datetime.now().strftime("%Y%m%d%H%M%S%f")
        original_name = secure_filename(file.filename)
        filename = f"{timestamp}_{original_name}"
        filepath = os.path.join(app.config["UPLOAD_FOLDER"], filename)

        logger.info(f"📁 Fiş dosyası kaydediliyor: {filename}")
        file.save(filepath)

        try:
            # OCR process
            logger.info("🔍 Fiş OCR işlemi başlıyor...")
            lines = process_ocr(filepath)
            logger.info(f"📄 Fiş OCR tamamlandı. {len(lines)} satır çıkarıldı.")

            # Debug için OCR çıktısını göster
            print("\n" + "=" * 50)
            print("📄 FİŞ OCR ÇIKTISI:")
            print("=" * 50)
            for i, line in enumerate(lines, 1):
                print(f"{i:2d}: {line}")
            print("=" * 50 + "\n")

        except Exception as ocr_err:
            logger.error(f"❌ Fiş OCR hatası: {ocr_err}")
            logger.error(traceback.format_exc())
            return jsonify({
                "error": f"OCR işlemi sırasında hata oluştu: {str(ocr_err)}",
                "code": "OCR_ERROR"
            }), 500

        try:
            # Field detection using the new receipt detector
            logger.info("🎯 Fiş alan çıkarma işlemi başlıyor...")
            result = detect_receipt_fields(lines) # Use the new detector
            result["raw_ocr_lines"] = lines
            logger.info("✅ Fiş alan çıkarma tamamlandı")

            # Debug için sonuçları göster
            print("\n" + "=" * 50)
            print("🎯 ÇIKARILAN FİŞ ALANLARI:")
            print("=" * 50)
            for key, value in result.items():
                if key not in ["raw_ocr_lines"] and value:
                    print(f"{key:25}: {value}")
            print("=" * 50 + "\n")

        except Exception as extract_err:
            logger.error(f"❌ Fiş alan çıkarım hatası: {extract_err}")
            logger.error(traceback.format_exc())
            result = {
                'firma_adi': '', 'vergi_no': '', 'fis_no': '', 'tarih': '',
                'toplam_tutar': '', 'kdv_toplam': '', 'matrah_toplam': '',
                'kdv_oranlari': {}, 'matrah_oranlari': {},
                'error': f"Alan çıkarımı sırasında hata: {str(extract_err)}",
                'code': 'EXTRACTION_ERROR',
                'raw_ocr_lines': lines
            }

        # Clean up the temporary file
        try:
            os.remove(filepath)
            logger.info(f"🗑️ Geçici fiş dosyası silindi: {filename}")
        except Exception as del_err:
            logger.warning(f"⚠️ Geçici fiş dosyası silinemedi: {del_err}")

        # Mapping to match UI expectations
        mapping = {
            "firma_adi": "firmaAdi",
            "vergi_no": "vergiNo", 
            "fis_no": "fisNo",
            "tarih": "tarih",
            "toplam_tutar": "toplamTutar",
            "kdv_oranlari": "kdvOranlari",
            "matrah_oranlari": "matrahOranlari",
            "raw_ocr_lines": "rawOcrLines"
        }
        
        mapped_result = {}
        for k, v in mapping.items():
            value = result.get(k, "")
            if value is None:
                value = ""
            mapped_result[v] = value

        # Ensure all fields are strings (except objects)
        for key in mapped_result:
            if key not in ["kdvOranlari", "matrahOranlari", "rawOcrLines"]:
                if mapped_result[key] is None:
                    mapped_result[key] = ""
                elif not isinstance(mapped_result[key], str):
                    mapped_result[key] = str(mapped_result[key])

        print("FİŞ JSON RESPONSE:", mapped_result)
        return jsonify(mapped_result), 200

    except Exception as e:
        logger.critical(f"💥 Beklenmedik hata /receipt endpoint: {e}")
        logger.critical(traceback.format_exc())
        return jsonify({"error": "Beklenmedik bir sunucu hatası oluştu.", "code": "UNEXPECTED_ERROR"}), 500


@app.errorhandler(413)
def too_large(e):
    """Dosya boyutu çok büyük hatası"""
    return jsonify({
        "error": "Dosya boyutu çok büyük. Maksimum 16MB desteklenir",
        "code": "FILE_TOO_LARGE"
    }), 413


@app.errorhandler(404)
def not_found(e):
    """Sayfa bulunamadı hatası"""
    return jsonify({
        "error": "Endpoint bulunamadı",
        "code": "NOT_FOUND"
    }), 404


@app.errorhandler(405)
def method_not_allowed(e):
    """Yanlış HTTP metodu hatası"""
    return jsonify({
        "error": "HTTP metodu desteklenmiyor",
        "code": "METHOD_NOT_ALLOWED"
    }), 405


if __name__ == "__main__":
    logger.info("🚀 OCR Servisi başlatılıyor...")
    logger.info(f"📁 Upload klasörü: {UPLOAD_FOLDER}")
    logger.info(f"📊 Maksimum dosya boyutu: {MAX_CONTENT_LENGTH / (1024 * 1024):.0f}MB")
    logger.info(f"📝 Desteklenen formatlar: {', '.join(ALLOWED_EXTENSIONS)}")

    app.run(debug=True, host='0.0.0.0', port=5000)