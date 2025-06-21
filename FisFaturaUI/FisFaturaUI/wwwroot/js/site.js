// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

let eklemeTipi = ''; // 'Fatura' veya 'Fiş'

/**
 * Kullanıcının 'Yeni Fatura' veya 'Yeni Fiş' butonuna basmasına göre işlem tipini ayarlar
 * ve firma seçme modal'ını gösterir.
 * @param {'Fatura' | 'Fiş'} tip Kullanıcının seçtiği işlem tipi.
 */
function setEklemeTipi(tip) {
    eklemeTipi = tip;
    
    // Eğer 'Yeni Firma'ya tıklandıysa, modal'ı göstermeden doğrudan sayfaya yönlendir.
    if (tip === 'Firma') {
        window.location.href = '/Firm/Create';
        return;
    }

    // 'Fiş' veya 'Fatura' için modal'ı göster.
    const modal = new bootstrap.Modal(document.getElementById('firmaSecModal'));
    modal.show();
}

/**
 * API'den firmaları çeker ve modal'daki select listesini doldurur.
 */
async function fetchFirmsAndPopulateModal() {
    try {
        const response = await fetch(`${window.apiBaseUrl}/api/Firm`);
        if (!response.ok) {
            throw new Error(`API isteği başarısız: ${response.statusText}`);
        }
        const firms = await response.json();
        const select = $('#firmaSelect');
        select.empty().append('<option value="">Firma Seçiniz...</option>');
        
        firms.forEach(firma => {
            const kayitTarihi = new Date(firma.kayitTarihi).toLocaleDateString('tr-TR');
            select.append(
                `<option value="${firma.id}" data-vergi="${firma.vergiNo}" data-tarih="${kayitTarihi}">
                    ${firma.firmaAdi}
                </option>`
            );
        });
    } catch (error) {
        console.error('Firmalar yüklenirken bir hata oluştu:', error);
        alert('Firmalar yüklenemedi. Lütfen daha sonra tekrar deneyin.');
    }
}

$(document).ready(function() {
    
    // Modal gösterilmeden hemen önce firmaları yükle.
    $('#firmaSecModal').on('show.bs.modal', function () {
        fetchFirmsAndPopulateModal();
    });

    // Firma seçme modal'ındaki "Devam Et" butonunun tıklama olayını yönetir.
    $('#firmaSecDevamBtn').on('click', function() {
        const firmaId = $('#firmaSelect').val();
        const firmaAdi = $('#firmaSelect').find('option:selected').text();
        
        if (!firmaId) {
            alert('Lütfen bir firma seçin.');
            return;
        }

        // Modal'ı gizle
        const modalElement = document.getElementById('firmaSecModal');
        const modalInstance = bootstrap.Modal.getInstance(modalElement);
        if (modalInstance) {
            modalInstance.hide();
        }

        // Seçilen tipe göre doğru sayfaya yönlendir.
        let url;
        if (eklemeTipi === 'Fatura') {
            url = `/Invoice/Create?firmaId=${firmaId}`;
        } else if (eklemeTipi === 'Fiş') {
            url = `/Invoice/ReceiptUpload?firmaAdi=${encodeURIComponent(firmaAdi)}&firmaId=${firmaId}`;
        }

        if (url) {
            window.location.href = url;
        }
    });

    // Modal'daki firma seçim listesi değiştiğinde "Devam Et" butonunu aktif/pasif et.
    // Bu kodun _Layout.cshtml'den buraya taşınması daha doğru.
    $('#firmaSelect').on('change', function() {
        const firmaId = $(this).val();
        const btnDevam = $('#firmaSecDevamBtn');
        const firmaInfo = $('#firmaInfo');
        const selectedOption = $(this).find('option:selected');
        
        if (firmaId) {
            btnDevam.prop('disabled', false);
            firmaInfo.show();
            $('#selectedVergiNo').text(selectedOption.data('vergi'));
            $('#selectedTarih').text(selectedOption.data('tarih'));
        } else {
            btnDevam.prop('disabled', true);
            firmaInfo.hide();
        }
    });
});
