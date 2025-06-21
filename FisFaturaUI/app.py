# Alan çıkarımı tamamlandıktan hemen önce, eksik alanları default ile doldur
expected_fields = [
    "firma_adi", "vergi_no", "fatura_no", "fatura_tarihi", "toplam_tutar",
    "odeme_turu", "icerik_turu", "fatura_turu", "odeme_yontemi",
    "fatura_gonderen", "fatura_gonderen_vergi_no", "fatura_alan", "fatura_alan_vergi_no",
    "kdv0", "matrah0", "kdv1", "matrah1", "kdv8", "matrah8", "kdv10", "matrah10", "kdv18", "matrah18", "kdv20", "matrah20",
    "kdv_toplam_tutar", "matrah_toplam_tutar", "raw_ocr_lines"
]
for field in expected_fields:
    if field not in result:
        if field.startswith("kdv") or field.startswith("matrah"):
            result[field] = "0.00"
        elif field == "raw_ocr_lines":
            result[field] = []
        else:
            result[field] = ""
# Tüm alanları string olarak döndür (raw_ocr_lines hariç)
for key in result:
    if key != "raw_ocr_lines" and not isinstance(result[key], str):
        result[key] = str(result[key])
import json
print("PYTHON OCR JSON RESPONSE:", json.dumps(result, ensure_ascii=False))
return jsonify(result) 