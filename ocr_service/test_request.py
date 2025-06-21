import requests

url = "http://127.0.0.1:5000/ocr"
file_path = "C:/Users/Admin/Downloads/WhatsApp Görsel 2025-06-11 saat 16.28.12_e778e1c2.jpg"# kendi görsel yolunu buraya yaz

with open(file_path, 'rb') as f:
    files = {'file': f}
    response = requests.post(url, files=files)

print("Sonuç:")
print(response.status_code)
print(response.json())
