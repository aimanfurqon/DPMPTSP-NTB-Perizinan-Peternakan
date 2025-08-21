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
        this.loadInitialLocationData();
        this.setupEventListeners();
    }

    loadInitialLocationData() {
        console.log('🔧 Loading initial location data...');
        
        // Load provinces for company form
        this.loadLocationData('/Location/GetProvinces', '#companyProvinceSelect', 'Pilih Provinsi');
        
        // Load provinces for individual form
        this.loadLocationData('/Location/GetProvinces', '#individualProvinceSelect', 'Pilih Provinsi');
        
        // Load provinces for shipping details (step 2)
        this.loadLocationData('/Location/GetProvinces', '#originProvinceSelect', 'Pilih Provinsi');
        this.loadLocationData('/Location/GetProvinces', '#destinationProvinceSelect', 'Pilih Provinsi');
        
        console.log('✅ Initial location data loading initiated');
    }

    initializeAllSelect2() {
        const select2Options = {
            theme: 'bootstrap-5',
            width: '100%',
            placeholder: 'Pilih dari daftar',
            allowClear: true
        };

        $('#companyProvinceSelect, #companyRegencySelect, #companyDistrictSelect, #companyVillageSelect').select2(select2Options);
        $('#individualProvinceSelect, #individualRegencySelect').select2(select2Options);
        $('#originProvinceSelect, #destinationProvinceSelect, #originRegencySelect, #destinationRegencySelect').select2(select2Options);
        $('#departurePortSelect, #arrivalPortSelect').select2(select2Options);
    }

    loadLocationData(url, targetSelector, placeholder) {
        const $target = $(targetSelector);
        $target.html(`<option value="">${placeholder}</option>`).prop('disabled', true);
        if (!url) return;

        console.log(`🔍 Loading location data from: ${url}`);
        $target.html('<option value="">Memuat...</option>');
        
        $.get(url, function(response) {
            console.log(`📊 Response from ${url}:`, response);
            
            // Handle both direct array and wrapped in data property
            let items = response;
            if (response && response.data) {
                items = response.data;
            }
            
            let options = `<option value="">${placeholder}</option>`;
            
            if (Array.isArray(items)) {
                console.log(`✅ Found ${items.length} items`);
                options += items.map(i => `<option value="${i.code}|${i.name}">${i.name}</option>`).join('');
            } else {
                console.log('❌ Response is not an array:', typeof items);
                console.log('Response structure:', items);
            }
            
            $target.html(options).prop('disabled', false);
            console.log(`✅ Updated ${targetSelector} with ${items?.length || 0} options`);
        }).fail(function(xhr, status, error) {
            console.error(`❌ Failed to load data from ${url}:`, error);
            console.error('Status:', status);
            console.error('Response:', xhr.responseText);
            $target.html(`<option value="">Gagal memuat</option>`);
        });
    }

    loadRegencies(provinceId, targetSelect) {
        const $target = $(targetSelect);
        $target.html('<option value="">Memuat...</option>').prop('disabled', true);

        console.log(`🔍 Loading regencies for province: ${provinceId}`);
        
        $.get('/Location/GetRegencies', { provinceId: provinceId }, function(response) {
            console.log(`📊 Regencies response:`, response);
            
            // Handle both direct array and wrapped in data property
            let regencies = response;
            if (response && response.data) {
                regencies = response.data;
            }
            
            let options = '<option value="">Pilih Kabupaten/Kota</option>';
            if (Array.isArray(regencies)) {
                console.log(`✅ Found ${regencies.length} regencies`);
                options += regencies.map(regency =>
                    `<option value="${regency.code}|${regency.name}">${regency.name}</option>`
                ).join('');
            } else {
                console.log('❌ Regencies response is not an array:', typeof regencies);
            }
            
            $target.html(options).prop('disabled', false).trigger('change');
            console.log(`✅ Updated ${targetSelect} with ${regencies?.length || 0} options`);
        }).fail(function(xhr, status, error) {
            console.error('Failed to load regencies:', error);
            console.error('Status:', status);
            console.error('Response:', xhr.responseText);
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
        this.setupIndividualAddressHandlers();
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

    setupIndividualAddressHandlers() {
        $('#individualProvinceSelect').on('change', function() {
            const selectedValue = $(this).val();
            $('input[name="IndividualProvince"]').val('');
            $('input[name="IndividualRegency"]').val('');

            if (selectedValue) {
                const [id, name] = selectedValue.split('|');
                $('input[name="IndividualProvince"]').val(name);
                window.permitCreateManager.loadLocationData(`/Location/GetRegencies?provinceId=${id}`, '#individualRegencySelect', 'Pilih Kabupaten/Kota');
            }
            $('#individualRegencySelect').val(null).trigger('change');
        });

        $('#individualRegencySelect').on('change', function() {
            const selectedValue = $(this).val();
            $('input[name="IndividualRegency"]').val('');

            if (selectedValue) {
                const [id, name] = selectedValue.split('|');
                $('input[name="IndividualRegency"]').val(name);
            }
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
        // Add livestock button
        $('#addLivestock').on('click', function() {
            window.permitCreateManager.addLivestockDetail();
        });

        // Remove livestock button
        $(document).on('click', '.remove-livestock', function() {
            $(this).closest('.livestock-item').remove();
            window.permitCreateManager.updateLivestockIndexes();
            window.permitCreateManager.updateLivestockNumbers();
        });

        // Quota management handlers
        this.setupQuotaHandlers();
    }

    setupQuotaHandlers() {
        console.log('🔧 Setting up quota handlers...');

        // Refresh quota button
        $('#refreshQuotaBtn').on('click', function() {
            window.permitCreateManager.loadQuotaInfo();
        });

        // Livestock type change handler
        $(document).on('change', '.livestock-type', function() {
            const index = $(this).data('index');
            const livestockType = $(this).val();
            window.permitCreateManager.checkQuotaForLivestock(index, livestockType);
        });

        // Livestock quantity change handler
        $(document).on('input', '.livestock-quantity', function() {
            const index = $(this).data('index');
            const quantity = parseInt($(this).val()) || 0;
            window.permitCreateManager.validateQuantity(index, quantity);
        });

        console.log('✅ Quota handlers setup completed');
    }

    loadQuotaInfo() {
        console.log('🔍 Loading quota information...');
        
        const originLocation = $('#originLocationHidden').val();
        if (!originLocation) {
            this.showNotification('warning', 'Pilih asal ternak terlebih dahulu di step 2');
            return;
        }

        $('#quotaInfoPanel').show();
        $('#quotaInfoContent').html('<div class="text-center"><i class="fas fa-spinner fa-spin"></i> Memuat informasi kuota...</div>');

        // Simulate API call to get quota data
        setTimeout(() => {
            this.displayQuotaInfo(this.getMockQuotaData());
        }, 1000);
    }

    getMockQuotaData() {
        // Mock data - in real implementation, this would come from API
        return {
            origin: 'Lombok Tengah, Nusa Tenggara Barat',
            year: 2025,
            quotas: {
                'Sapi Potong': { total: 5000, used: 3200, available: 1800 },
                'Kerbau Potong': { total: 2000, used: 1200, available: 800 },
                'Kuda Pedaging': { total: 1000, used: 600, available: 400 },
                'Kambing': { total: 8000, used: 4500, available: 3500 },
                'Domba': { total: 6000, used: 3800, available: 2200 }
            }
        };
    }

    displayQuotaInfo(quotaData) {
        let html = `
            <div class="quota-header">
                <h5>Kuota Ternak ${quotaData.year}</h5>
                <p class="text-muted">Asal: ${quotaData.origin}</p>
            </div>
            <div class="quota-table">
                <table class="table table-sm">
                    <thead>
                        <tr>
                            <th>Jenis Ternak</th>
                            <th>Total Kuota</th>
                            <th>Terpakai</th>
                            <th>Tersedia</th>
                            <th>Status</th>
                        </tr>
                    </thead>
                    <tbody>
        `;

        Object.entries(quotaData.quotas).forEach(([type, quota]) => {
            const percentage = (quota.used / quota.total * 100).toFixed(1);
            const statusClass = percentage > 80 ? 'text-danger' : percentage > 60 ? 'text-warning' : 'text-success';
            const statusIcon = percentage > 80 ? 'fa-exclamation-triangle' : percentage > 60 ? 'fa-exclamation-circle' : 'fa-check-circle';
            
            html += `
                <tr>
                    <td>${type}</td>
                    <td>${quota.total.toLocaleString()}</td>
                    <td>${quota.used.toLocaleString()}</td>
                    <td>${quota.available.toLocaleString()}</td>
                    <td class="${statusClass}">
                        <i class="fas ${statusIcon}"></i>
                        ${percentage}%
                    </td>
                </tr>
            `;
        });

        html += `
                    </tbody>
                </table>
            </div>
        `;

        $('#quotaInfoContent').html(html);
        console.log('✅ Quota information displayed');
    }

    checkQuotaForLivestock(index, livestockType) {
        console.log(`🔍 Checking quota for livestock ${index}: ${livestockType}`);
        
        if (!livestockType) {
            this.hideQuotaIndicator(index);
            return;
        }

        const quotaData = this.getMockQuotaData();
        const quota = quotaData.quotas[livestockType];
        
        if (quota) {
            this.showQuotaIndicator(index, quota);
        } else {
            this.hideQuotaIndicator(index);
        }
    }

    showQuotaIndicator(index, quota) {
        const $indicator = $(`.quota-indicator[data-index="${index}"]`);
        const $limit = $(`.quota-limit[data-index="${index}"]`);
        const $quantity = $(`.livestock-quantity[data-index="${index}"]`);
        
        $indicator.show();
        $indicator.find('.quota-text').text(`Tersedia: ${quota.available.toLocaleString()} ekor`);
        
        $limit.show();
        $limit.find('.max-quota').text(quota.available.toLocaleString());
        
        $quantity.attr('max', quota.available);
        
        console.log(`✅ Quota indicator shown for index ${index}`);
    }

    hideQuotaIndicator(index) {
        const $indicator = $(`.quota-indicator[data-index="${index}"]`);
        const $limit = $(`.quota-limit[data-index="${index}"]`);
        
        $indicator.hide();
        $limit.hide();
        
        console.log(`✅ Quota indicator hidden for index ${index}`);
    }

    validateQuantity(index, quantity) {
        console.log(`🔍 Validating quantity for livestock ${index}: ${quantity}`);
        
        const $quantity = $(`.livestock-quantity[data-index="${index}"]`);
        const $feedback = $(`.quantity-feedback[data-index="${index}"]`);
        const maxQuota = parseInt($quantity.attr('max')) || 999999;
        
        $quantity.removeClass('is-valid is-invalid');
        $feedback.hide();
        
        if (quantity > 0) {
            if (quantity > maxQuota) {
                $quantity.addClass('is-invalid');
                $feedback.show().find('.feedback-text')
                    .removeClass('text-success text-warning text-danger')
                    .addClass('text-danger')
                    .text(`Jumlah melebihi kuota tersedia (maks: ${maxQuota.toLocaleString()} ekor)`);
            } else if (quantity > maxQuota * 0.8) {
                $quantity.addClass('is-valid');
                $feedback.show().find('.feedback-text')
                    .removeClass('text-success text-warning text-danger')
                    .addClass('text-warning')
                    .text(`Hampir mencapai batas kuota (${((quantity/maxQuota)*100).toFixed(1)}%)`);
            } else {
                $quantity.addClass('is-valid');
                $feedback.show().find('.feedback-text')
                    .removeClass('text-success text-warning text-danger')
                    .addClass('text-success')
                    .text(`Dalam batas kuota (${((quantity/maxQuota)*100).toFixed(1)}%)`);
            }
        }
        
        this.updateSummary();
    }

    addLivestockItem() {
        const currentIndex = $('.livestock-item').length;
        const template = `
            <div class="livestock-item" data-index="${currentIndex}">
                <div class="livestock-header">
                    <h4>Ternak #<span class="livestock-number">${currentIndex + 1}</span></h4>
                    <button type="button" class="btn btn-sm btn-outline-danger remove-livestock" title="Hapus item ini">
                        <i class="fas fa-trash"></i>
                    </button>
                </div>

                <div class="livestock-form">
                    <div class="form-group">
                        <label class="form-label required">
                            <i class="fas fa-paw"></i>
                            Jenis Ternak
                            <span class="quota-indicator" data-index="${currentIndex}" style="display: none;">
                                <i class="fas fa-info-circle text-primary"></i>
                                <span class="quota-text">Loading...</span>
                            </span>
                        </label>
                        <select name="LivestockDetails[${currentIndex}].LivestockType" class="form-control livestock-type" data-index="${currentIndex}" required>
                            <option value="">Pilih Jenis Ternak</option>
                            <option value="Sapi Potong">Sapi Potong</option>
                            <option value="Kerbau Potong">Kerbau Potong</option>
                            <option value="Kuda Pedaging">Kuda Pedaging</option>
                            <option value="Kambing">Kambing</option>
                            <option value="Domba">Domba</option>
                        </select>
                    </div>

                    <div class="form-group">
                        <label class="form-label required">
                            <i class="fas fa-calculator"></i>
                            Jumlah (ekor)
                            <span class="quota-limit" data-index="${currentIndex}" style="display: none;">
                                <small class="text-muted">Maks: <span class="max-quota">-</span> ekor</small>
                            </span>
                        </label>
                        <input type="number" name="LivestockDetails[${currentIndex}].Quantity"
                               class="form-control livestock-quantity" data-index="${currentIndex}"
                               min="1" max="1"
                               placeholder="Masukkan jumlah ternak" required />
                        <div class="quantity-feedback" data-index="${currentIndex}" style="display: none;">
                            <small class="feedback-text">-</small>
                        </div>
                    </div>

                    <div class="form-group">
                        <label class="form-label">
                            <i class="fas fa-comment"></i>
                            Keterangan
                        </label>
                        <textarea name="LivestockDetails[${currentIndex}].Description"
                                  class="form-control" rows="2"
                                  placeholder="Keterangan tambahan (opsional)"></textarea>
                    </div>
                </div>
            </div>
        `;

        $('#livestockContainer').append(template);
        this.updateLivestockNumbers();
        console.log(`✅ Added livestock item ${currentIndex + 1}`);
    }

    updateLivestockNumbers() {
        $('.livestock-item').each(function(index) {
            $(this).attr('data-index', index);
            $(this).find('.livestock-number').text(index + 1);
            $(this).find('.livestock-type').attr('data-index', index);
            $(this).find('.livestock-quantity').attr('data-index', index);
            $(this).find('.quota-indicator').attr('data-index', index);
            $(this).find('.quota-limit').attr('data-index', index);
            $(this).find('.quantity-feedback').attr('data-index', index);
            
            // Update name attributes
            $(this).find('select[name*="LivestockType"]').attr('name', `LivestockDetails[${index}].LivestockType`);
            $(this).find('input[name*="Quantity"]').attr('name', `LivestockDetails[${index}].Quantity`);
            $(this).find('textarea[name*="Description"]').attr('name', `LivestockDetails[${index}].Description`);
        });
    }

    updateSummary() {
        const totalTypes = $('.livestock-item').length;
        let totalQuantity = 0;
        
        $('.livestock-quantity').each(function() {
            totalQuantity += parseInt($(this).val()) || 0;
        });

        $('#totalTypes').text(totalTypes);
        $('#totalQuantity').text(totalQuantity.toLocaleString());

        // Show quota usage if there are items
        if (totalTypes > 0) {
            $('.quota-usage').show();
            this.updateQuotaStatus();
        } else {
            $('.quota-usage').hide();
        }

        console.log(`📊 Summary updated: ${totalTypes} types, ${totalQuantity} total quantity`);
    }

    updateQuotaStatus() {
        let hasErrors = false;
        let hasWarnings = false;

        $('.livestock-quantity').each(function() {
            const $quantity = $(this);
            if ($quantity.hasClass('is-invalid')) {
                hasErrors = true;
            } else if ($quantity.hasClass('is-valid') && $quantity.next('.quantity-feedback').find('.text-warning').length > 0) {
                hasWarnings = true;
            }
        });

        const $status = $('#quotaStatus');
        if (hasErrors) {
            $status.html('<i class="fas fa-exclamation-triangle text-danger"></i> Melebihi Kuota');
        } else if (hasWarnings) {
            $status.html('<i class="fas fa-exclamation-circle text-warning"></i> Hampir Penuh');
        } else {
            $status.html('<i class="fas fa-check-circle text-success"></i> Dalam Batas');
        }
    }

    setupDocumentHandlers() {
        $('input[type="file"]').on('change', function() {
            const fileName = this.files[0]?.name || 'Tidak ada file dipilih';
            $(this).next('.custom-file-label').text(fileName);
        });
    }

    addLivestockDetail() {
        const template = `
            <div class="livestock-item" data-index="${this.livestockIndex}">
                <div class="livestock-header">
                    <h4>Ternak #<span class="livestock-number">${this.livestockIndex + 1}</span></h4>
                    <button type="button" class="btn btn-sm btn-outline-danger remove-livestock" title="Hapus item ini">
                        <i class="fas fa-trash"></i>
                    </button>
                </div>

                <div class="livestock-form">
                    <div class="form-group">
                        <label class="form-label required">
                            <i class="fas fa-paw"></i>
                            Jenis Ternak
                            <span class="quota-indicator" data-index="${this.livestockIndex}" style="display: none;">
                                <i class="fas fa-info-circle text-primary"></i>
                                <span class="quota-text">Loading...</span>
                            </span>
                        </label>
                        <select name="LivestockDetails[${this.livestockIndex}].LivestockType" class="form-control livestock-type" data-index="${this.livestockIndex}" required>
                            <option value="">Pilih Jenis Ternak</option>
                            <option value="Sapi Potong">Sapi Potong</option>
                            <option value="Kerbau Potong">Kerbau Potong</option>
                            <option value="Kuda Pedaging">Kuda Pedaging</option>
                            <option value="Kambing">Kambing</option>
                            <option value="Domba">Domba</option>
                        </select>
                    </div>

                    <div class="form-group">
                        <label class="form-label required">
                            <i class="fas fa-calculator"></i>
                            Jumlah (ekor)
                            <span class="quota-limit" data-index="${this.livestockIndex}" style="display: none;">
                                <small class="text-muted">Maks: <span class="max-quota">-</span> ekor</small>
                            </span>
                        </label>
                        <input type="number" name="LivestockDetails[${this.livestockIndex}].Quantity"
                               class="form-control livestock-quantity" data-index="${this.livestockIndex}"
                               min="1" max="1"
                               placeholder="Masukkan jumlah ternak" required />
                        <div class="quantity-feedback" data-index="${this.livestockIndex}" style="display: none;">
                            <small class="feedback-text">-</small>
                        </div>
                    </div>

                    <div class="form-group">
                        <label class="form-label">
                            <i class="fas fa-comment"></i>
                            Keterangan
                        </label>
                        <textarea name="LivestockDetails[${this.livestockIndex}].Description"
                                  class="form-control" rows="2"
                                  placeholder="Keterangan tambahan (opsional)"></textarea>
                    </div>
                </div>
            </div>
        `;

        $('#livestockContainer').append(template);
        this.livestockIndex++;
        
        // Update livestock numbers
        this.updateLivestockNumbers();
    }

    updateLivestockIndexes() {
        $('.livestock-item').each((index, element) => {
            $(element).find('select, input').each((i, input) => {
                const name = $(input).attr('name');
                if (name) {
                    const newName = name.replace(/\[\d+\]/, `[${index}]`);
                    $(input).attr('name', newName);
                }
            });
        });
        this.livestockIndex = $('.livestock-item').length;
    }

    updateLivestockNumbers() {
        $('.livestock-item').each((index, element) => {
            $(element).find('.livestock-number').text(index + 1);
            $(element).attr('data-index', index);
            $(element).find('[data-index]').attr('data-index', index);
        });
    }

    checkQuotaForLivestock(index, livestockType) {
        console.log(`🔍 Checking quota for livestock ${index}: ${livestockType}`);
        
        if (!livestockType) {
            this.hideQuotaIndicator(index);
            return;
        }

        const quotaData = this.getMockQuotaData();
        const quota = quotaData.quotas[livestockType];
        
        if (quota) {
            this.showQuotaIndicator(index, quota);
        } else {
            this.hideQuotaIndicator(index);
        }
    }

    showQuotaIndicator(index, quota) {
        const $indicator = $(`.quota-indicator[data-index="${index}"]`);
        const $limit = $(`.quota-limit[data-index="${index}"]`);
        const $quantity = $(`.livestock-quantity[data-index="${index}"]`);
        
        $indicator.show();
        $indicator.find('.quota-text').text(`Tersedia: ${quota.available.toLocaleString()} ekor`);
        
        $limit.show();
        $limit.find('.max-quota').text(quota.available.toLocaleString());
        
        $quantity.attr('max', quota.available);
        
        console.log(`✅ Quota indicator shown for index ${index}`);
    }

    hideQuotaIndicator(index) {
        const $indicator = $(`.quota-indicator[data-index="${index}"]`);
        const $limit = $(`.quota-limit[data-index="${index}"]`);
        
        $indicator.hide();
        $limit.hide();
        
        console.log(`✅ Quota indicator hidden for index ${index}`);
    }

    validateQuantity(index, quantity) {
        console.log(`🔍 Validating quantity for livestock ${index}: ${quantity}`);
        
        const $quantity = $(`.livestock-quantity[data-index="${index}"]`);
        const $feedback = $(`.quantity-feedback[data-index="${index}"]`);
        const maxQuota = parseInt($quantity.attr('max')) || 999999;
        
        $quantity.removeClass('is-valid is-invalid');
        $feedback.hide();
        
        if (quantity > 0) {
            if (quantity > maxQuota) {
                $quantity.addClass('is-invalid');
                $feedback.show().find('.feedback-text')
                    .removeClass('text-success text-warning text-danger')
                    .addClass('text-danger')
                    .text(`Jumlah melebihi kuota tersedia (maks: ${maxQuota.toLocaleString()} ekor)`);
            } else if (quantity > maxQuota * 0.8) {
                $quantity.addClass('is-valid');
                $feedback.show().find('.feedback-text')
                    .removeClass('text-success text-warning text-danger')
                    .addClass('text-warning')
                    .text(`Hampir mencapai batas kuota (${((quantity/maxQuota)*100).toFixed(1)}%)`);
            } else {
                $quantity.addClass('is-valid');
                $feedback.show().find('.feedback-text')
                    .removeClass('text-success text-warning text-danger')
                    .addClass('text-success')
                    .text(`Dalam batas kuota (${((quantity/maxQuota)*100).toFixed(1)}%)`);
            }
        }
        
        this.updateSummary();
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
