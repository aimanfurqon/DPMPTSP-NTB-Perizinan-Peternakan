// Permit Create Page JavaScript
// This file contains all JavaScript functionality for the permit creation page

class PermitCreateManager {
    constructor() {
        this.isLoadingOriginPorts = false;
        this.isLoadingDestinationPorts = false;
        this.livestockIndex = 1;
        this.quotaCache = {};
        this.originProvinceCode = '';
        
        this.init();
    }

    init() {
        this.initializeAllSelect2();
        this.setupEventListeners();
    }

    initializeAllSelect2() {
        const select2Options = {
            theme: 'bootstrap-5',
            width: '100%',
            placeholder: 'Pilih dari daftar',
            allowClear: true
        };

        $('#companyProvinceSelect, #companyRegencySelect, #companyDistrictSelect, #companyVillageSelect').select2(select2Options);
        $('#originProvinceSelect, #destinationProvinceSelect, #originRegencySelect, #destinationRegencySelect').select2(select2Options);
        $('#departurePortSelect, #arrivalPortSelect').select2(select2Options);
    }

    loadLocationData(url, targetSelector, placeholder) {
        const $target = $(targetSelector);
        $target.html(`<option value="">${placeholder}</option>`).prop('disabled', true);
        if (!url) return;

        $target.html('<option value="">Memuat...</option>');
        $.get(url, function(response) {
            const items = response.data || response;
            let options = `<option value="">${placeholder}</option>`;
            if (Array.isArray(items)) {
                options += items.map(i => `<option value="${i.code}|${i.name}">${i.name}</option>`).join('');
            }
            $target.html(options).prop('disabled', false);
        }).fail(function() {
            $target.html(`<option value="">Gagal memuat</option>`);
        });
    }

    loadRegencies(provinceId, targetSelect) {
        const $target = $(targetSelect);
        $target.html('<option value="">Memuat...</option>').prop('disabled', true);

        $.get('/Location/GetRegencies', { provinceId: provinceId }, function(response) {
            const regencies = response.data || response;
            let options = '<option value="">Pilih Kabupaten/Kota</option>';
            if (Array.isArray(regencies)) {
                options += regencies.map(regency =>
                    `<option value="${regency.code}|${regency.name}">${regency.name}</option>`
                ).join('');
            }
            $target.html(options).prop('disabled', false).trigger('change');
        }).fail(function(xhr, status, error) {
            console.error('Failed to load regencies:', error);
            $target.html('<option value="">Gagal memuat</option>').prop('disabled', true);
        });
    }

    loadPortsByProvince(provinceCode, targetSelect, placeholderText, isOrigin = true) {
        const $target = $(targetSelect);
        const loadingVar = isOrigin ? 'isLoadingOriginPorts' : 'isLoadingDestinationPorts';

        if (this[loadingVar]) return;
        this[loadingVar] = true;

        $target.empty().append(`<option value="">Memuat pelabuhan...</option>`).prop('disabled', true);

        $target.select2('destroy');
        $target.select2({
            placeholder: placeholderText,
            allowClear: true,
            width: '100%',
            theme: 'bootstrap-5'
        });

        if (!provinceCode) {
            $target.empty().append(`<option value="">${placeholderText}</option>`).prop('disabled', true);
            this[loadingVar] = false;
            return;
        }

        $.get('/Port/GetByProvince', { provinceCode: provinceCode })
            .done((response) => {
                const ports = response.results || [];
                $target.empty().append(`<option value="">${placeholderText}</option>`);

                if (Array.isArray(ports) && ports.length > 0) {
                    ports.forEach(port => {
                        $target.append(`<option value="${port.id}">${port.text}</option>`);
                    });
                    $target.prop('disabled', false);
                    this.showNotification('success', `${ports.length} pelabuhan tersedia`);
                } else {
                    $target.append('<option value="">Tidak ada pelabuhan tersedia</option>').prop('disabled', true);
                    this.showNotification('warning', 'Tidak ada pelabuhan tersedia untuk provinsi ini');
                }
                $target.trigger('change');
            })
            .fail((xhr, status, error) => {
                console.error('Failed to load ports:', error);
                $target.empty().append('<option value="">Gagal memuat pelabuhan</option>').prop('disabled', true);
                this.showNotification('error', 'Gagal memuat data pelabuhan');
            })
            .always(() => {
                this[loadingVar] = false;
            });
    }

    showNotification(type, message) {
        const alertClass = type === 'error' ? 'danger' : type === 'success' ? 'success' : 'warning';
        const notification = $(`
            <div class="alert alert-${alertClass} alert-dismissible fade show position-fixed" style="top: 20px; right: 20px; z-index: 9999; max-width: 350px;">
                <i class="fas fa-${type === 'error' ? 'exclamation-circle' : type === 'success' ? 'check-circle' : 'info-circle'}"></i>
                ${message}
                <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
            </div>
        `);

        $('body').append(notification);
        setTimeout(() => {
            notification.alert('close');
        }, 4000);
    }

    updateLocation(provinceSel, regencySel, hiddenInput) {
        const provinceName = $(provinceSel + ' option:selected').text();
        const regencyName = $(regencySel + ' option:selected').text();

        if ($(provinceSel).val() && $(regencySel).val() &&
            provinceName !== "Pilih Provinsi" && regencyName !== "Pilih Kabupaten/Kota") {
            $(hiddenInput).val(`${regencyName}, ${provinceName}`);
        } else {
            $(hiddenInput).val('');
        }
        this.updateShippingSummary();
    }

    updateShippingSummary() {
        const originLocation = $('#originLocationHidden').val();
        const destinationLocation = $('#destinationLocationHidden').val();
        const departurePort = $('#departurePortHidden').val();
        const arrivalPort = $('#arrivalPortHidden').val();

        $('#summaryOrigin').text(originLocation || '-');
        $('#summaryDestination').text(destinationLocation || '-');
        $('#summaryDeparturePort').text(departurePort || '-');
        $('#summaryArrivalPort').text(arrivalPort || '-');

        if (originLocation && destinationLocation && departurePort && arrivalPort) {
            $('#shippingSummary').slideDown();
        } else {
            $('#shippingSummary').slideUp();
        }
    }

    validateShippingDetails() {
        const requiredFields = [
            { field: '#originLocationHidden', name: 'Asal Ternak' },
            { field: '#destinationLocationHidden', name: 'Tujuan Pengiriman' },
            { field: '#departurePortHidden', name: 'Pelabuhan Keberangkatan' },
            { field: '#arrivalPortHidden', name: 'Pelabuhan Tujuan' }
        ];

        const missingFields = requiredFields.filter(item => !$(item.field).val());

        if (missingFields.length > 0) {
            const fieldNames = missingFields.map(item => item.name).join(', ');
            this.showNotification('error', `Mohon lengkapi: ${fieldNames}`);
            return false;
        }
        return true;
    }

    setupEventListeners() {
        this.setupCompanyAddressHandlers();
        this.setupShippingDetailsHandlers();
        this.setupLivestockHandlers();
        this.setupDocumentHandlers();
    }

    setupCompanyAddressHandlers() {
        $('#companyProvinceSelect').on('change', function() {
            const selectedValue = $(this).val();
            $('input[name="CompanyProvince"]').val('');
            $('input[name="CompanyRegency"]').val('');
            $('input[name="AddressDistrict"]').val('');
            $('input[name="AddressVillage"]').val('');

            if (selectedValue) {
                const [id, name] = selectedValue.split('|');
                $('input[name="CompanyProvince"]').val(name);
                window.permitCreateManager.loadLocationData(`/Location/GetRegencies?provinceId=${id}`, '#companyRegencySelect', 'Pilih Kabupaten/Kota');
            }
            $('#companyRegencySelect, #companyDistrictSelect, #companyVillageSelect').val(null).trigger('change');
        });

        $('#companyRegencySelect').on('change', function() {
            const selectedValue = $(this).val();
            $('input[name="CompanyRegency"]').val('');
            $('input[name="AddressDistrict"]').val('');
            $('input[name="AddressVillage"]').val('');

            if (selectedValue) {
                const [id, name] = selectedValue.split('|');
                $('input[name="CompanyRegency"]').val(name);
                window.permitCreateManager.loadLocationData(`/Location/GetDistricts?regencyId=${id}`, '#companyDistrictSelect', 'Pilih Kecamatan');
            }
            $('#companyDistrictSelect, #companyVillageSelect').val(null).trigger('change');
        });

        $('#companyDistrictSelect').on('change', function() {
            const selectedValue = $(this).val();
            $('input[name="AddressDistrict"]').val('');
            $('input[name="AddressVillage"]').val('');

            if (selectedValue) {
                const [id, name] = selectedValue.split('|');
                $('input[name="AddressDistrict"]').val(name);
                window.permitCreateManager.loadLocationData(`/Location/GetVillages?districtId=${id}`, '#companyVillageSelect', 'Pilih Desa/Kelurahan');
            }
            $('#companyVillageSelect').val(null).trigger('change');
        });

        $('#companyVillageSelect').on('change', function() {
            const selectedValue = $(this).find('option:selected').text();
            $('input[name="AddressVillage"]').val(selectedValue === 'Pilih Desa/Kelurahan' ? '' : selectedValue);
        });
    }

    setupShippingDetailsHandlers() {
        $('#originProvinceSelect').on('change', function() {
            const selectedValue = $(this).val();
            $('#originLocationHidden').val('');
            $('#departurePortHidden').val('');
            $('#departurePortSelect').val(null).trigger('change');

            if (selectedValue) {
                const [id, name] = selectedValue.split('|');
                window.permitCreateManager.originProvinceCode = id;
                window.permitCreateManager.loadRegencies(id, '#originRegencySelect');
                window.permitCreateManager.loadPortsByProvince(id, '#departurePortSelect', 'Pilih Pelabuhan Keberangkatan', true);
            }
            $('#originRegencySelect').val(null).trigger('change');
        });

        $('#originRegencySelect').on('change', function() {
            window.permitCreateManager.updateLocation('#originProvinceSelect', '#originRegencySelect', '#originLocationHidden');
        });

        $('#destinationProvinceSelect').on('change', function() {
            const selectedValue = $(this).val();
            $('#destinationLocationHidden').val('');
            $('#arrivalPortHidden').val('');
            $('#arrivalPortSelect').val(null).trigger('change');

            if (selectedValue) {
                const [id, name] = selectedValue.split('|');
                window.permitCreateManager.loadRegencies(id, '#destinationRegencySelect');
                window.permitCreateManager.loadPortsByProvince(id, '#arrivalPortSelect', 'Pilih Pelabuhan Tujuan', false);
            }
            $('#destinationRegencySelect').val(null).trigger('change');
        });

        $('#destinationRegencySelect').on('change', function() {
            window.permitCreateManager.updateLocation('#destinationProvinceSelect', '#destinationRegencySelect', '#destinationLocationHidden');
        });

        $('#departurePortSelect').on('change', function() {
            const selectedText = $(this).find('option:selected').text();
            $('#departurePortHidden').val(selectedText === 'Pilih Pelabuhan Keberangkatan' ? '' : selectedText);
            window.permitCreateManager.updateShippingSummary();
        });

        $('#arrivalPortSelect').on('change', function() {
            const selectedText = $(this).find('option:selected').text();
            $('#arrivalPortHidden').val(selectedText === 'Pilih Pelabuhan Tujuan' ? '' : selectedText);
            window.permitCreateManager.updateShippingSummary();
        });
    }

    setupLivestockHandlers() {
        $('#addLivestockBtn').on('click', function() {
            window.permitCreateManager.addLivestockDetail();
        });

        $(document).on('click', '.remove-livestock-btn', function() {
            $(this).closest('.livestock-detail').remove();
            window.permitCreateManager.updateLivestockIndexes();
        });
    }

    setupDocumentHandlers() {
        $('input[type="file"]').on('change', function() {
            const fileName = this.files[0]?.name || 'Tidak ada file dipilih';
            $(this).next('.custom-file-label').text(fileName);
        });
    }

    addLivestockDetail() {
        const template = `
            <div class="livestock-detail border rounded p-3 mb-3">
                <div class="row">
                    <div class="col-md-4">
                        <label class="form-label">Jenis Ternak</label>
                        <select name="LivestockDetails[${this.livestockIndex}].LivestockType" class="form-select" required>
                            <option value="">Pilih Jenis Ternak</option>
                            <option value="Sapi">Sapi</option>
                            <option value="Kerbau">Kerbau</option>
                            <option value="Kambing">Kambing</option>
                            <option value="Domba">Domba</option>
                            <option value="Babi">Babi</option>
                            <option value="Unggas">Unggas</option>
                        </select>
                    </div>
                    <div class="col-md-4">
                        <label class="form-label">Jumlah</label>
                        <input type="number" name="LivestockDetails[${this.livestockIndex}].Quantity" class="form-control" min="1" required>
                    </div>
                    <div class="col-md-4">
                        <label class="form-label">Deskripsi</label>
                        <input type="text" name="LivestockDetails[${this.livestockIndex}].Description" class="form-control" placeholder="Contoh: Sapi Bali, Kambing Kacang">
                    </div>
                </div>
                <div class="mt-2">
                    <button type="button" class="btn btn-sm btn-danger remove-livestock-btn">
                        <i class="fas fa-trash"></i> Hapus
                    </button>
                </div>
            </div>
        `;

        $('#livestockDetailsContainer').append(template);
        this.livestockIndex++;
    }

    updateLivestockIndexes() {
        $('.livestock-detail').each((index, element) => {
            $(element).find('select, input').each((i, input) => {
                const name = $(input).attr('name');
                if (name) {
                    const newName = name.replace(/\[\d+\]/, `[${index}]`);
                    $(input).attr('name', newName);
                }
            });
        });
        this.livestockIndex = $('.livestock-detail').length;
    }
}

// Initialize when document is ready
$(document).ready(function() {
    window.permitCreateManager = new PermitCreateManager();
});

// Global functions for backward compatibility
function loadLocationData(url, targetSelector, placeholder) {
    if (window.permitCreateManager) {
        window.permitCreateManager.loadLocationData(url, targetSelector, placeholder);
    }
}

function loadRegencies(provinceId, targetSelect) {
    if (window.permitCreateManager) {
        window.permitCreateManager.loadRegencies(provinceId, targetSelect);
    }
}

function loadPortsByProvince(provinceCode, targetSelect, placeholderText, isOrigin) {
    if (window.permitCreateManager) {
        window.permitCreateManager.loadPortsByProvince(provinceCode, targetSelect, placeholderText, isOrigin);
    }
}

function showNotification(type, message) {
    if (window.permitCreateManager) {
        window.permitCreateManager.showNotification(type, message);
    }
}

function updateLocation(provinceSel, regencySel, hiddenInput) {
    if (window.permitCreateManager) {
        window.permitCreateManager.updateLocation(provinceSel, regencySel, hiddenInput);
    }
}

function updateShippingSummary() {
    if (window.permitCreateManager) {
        window.permitCreateManager.updateShippingSummary();
    }
}

function validateShippingDetails() {
    if (window.permitCreateManager) {
        return window.permitCreateManager.validateShippingDetails();
    }
    return false;
}

function addLivestockDetail() {
    if (window.permitCreateManager) {
        window.permitCreateManager.addLivestockDetail();
    }
}
