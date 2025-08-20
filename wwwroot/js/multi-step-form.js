// Multi-Step Form JavaScript - Fixed Version with Individual Support
// This file handles ONLY the multi-step navigation and basic form validation
// Location dropdowns, quota management, and other features are handled in Create.cshtml

// ===============================================
// GLOBAL VARIABLES
// ===============================================

let currentStep = 1;
const totalSteps = 4;
let livestockIndex = Math.max($('.livestock-item').length, 1);
const totalRequiredFiles = 7;
const uploadedDocuments = new Map();

const documentTypes = [
    'SuratPermohonan',
    'RekomendasiDinasProv',
    'RekomendasiDaerahTujuan',
    'SKKHKabupatenAsal',
    'SKKHDinasProvinsi',
    'SuratJalanTernak',
    'HasilPemeriksaanFisik',
    'DokumenOpsional'
];

const stepValidationRules = {
    1: validateCompanyStep,
    2: validateShippingStep, // Changed from array to function
    3: validateLivestockStep,
    4: validateDocumentStep
};

// ===============================================
// SERVER RESPONSE HANDLING
// ===============================================

// Function to handle server-side validation errors
function handleServerValidationErrors() {
    console.log('🔍 Checking for server-side validation errors...');
    
    // Check for validation summary errors
    const validationSummary = $('.validation-summary-errors');
    if (validationSummary.length > 0) {
        const errors = validationSummary.find('li');
        if (errors.length > 0) {
            const errorMessages = [];
            errors.each(function() {
                errorMessages.push($(this).text().trim());
            });
            
            if (errorMessages.length > 0) {
                showAlert('Terjadi kesalahan validasi: ' + errorMessages.join(', '), 'danger');
                console.log('❌ Server validation errors found:', errorMessages);
                
                // Reset submit button
                $('#submitBtn').html('<i class="fas fa-paper-plane"></i> Ajukan Permohonan').prop('disabled', false);
                
                // Stay on current step instead of going back to step 1
                console.log('📍 Staying on current step due to validation errors');
                return true;
            }
        }
    }
    
    // Check for field-specific errors
    const fieldErrors = $('.field-validation-error');
    if (fieldErrors.length > 0) {
        const errorMessages = [];
        fieldErrors.each(function() {
            const errorText = $(this).text().trim();
            if (errorText) {
                errorMessages.push(errorText);
            }
        });
        
        if (errorMessages.length > 0) {
            showAlert('Terjadi kesalahan validasi: ' + errorMessages.join(', '), 'danger');
            console.log('❌ Field validation errors found:', errorMessages);
            
            // Reset submit button
            $('#submitBtn').html('<i class="fas fa-paper-plane"></i> Ajukan Permohonan').prop('disabled', false);
            
            // Stay on current step instead of going back to step 1
            console.log('📍 Staying on current step due to field validation errors');
            return true;
        }
    }
    
    // Check for TempData error messages
    const tempDataError = $('[data-tempdata-error]');
    if (tempDataError.length > 0) {
        const errorMessage = tempDataError.attr('data-tempdata-error');
        if (errorMessage) {
            showAlert(errorMessage, 'danger');
            console.log('❌ TempData error found:', errorMessage);
            
            // Reset submit button
            $('#submitBtn').html('<i class="fas fa-paper-plane"></i> Ajukan Permohonan').prop('disabled', false);
            
            // Stay on current step instead of going back to step 1
            console.log('📍 Staying on current step due to TempData error');
            return true;
        }
    }
    
    console.log('✅ No server validation errors found');
    return false;
}

// Function to handle successful submission
function handleSuccessfulSubmission() {
    console.log('✅ Handling successful submission...');
    
    // Check for success message
    const successMessage = $('[data-tempdata-success]');
    if (successMessage.length > 0) {
        const message = successMessage.attr('data-tempdata-success');
        if (message) {
            showAlert(message, 'success');
            console.log('✅ Success message found:', message);
        }
    }
    
    // Reset submit button
    $('#submitBtn').html('<i class="fas fa-paper-plane"></i> Ajukan Permohonan').prop('disabled', false);
}

// ===============================================
// INITIALIZATION
// ===============================================
$(document).ready(function () {
    console.log('🚀 Initializing Multi-Step Form Navigation...');

    initializeForm();
    initializeFileUpload();
    initializeEventHandlers();
    initializeDocumentDetailsValidation();

    console.log('✅ Multi-Step Form Navigation initialized');
});

function initializeForm() {
    showStep(1);
    updateNavigationButtons();
    updateStepProgress();
    updateUploadProgress();
    updateDocumentChecklist();
    
    // Initialize applicant type selection
    initializeApplicantTypeSelection();
    
    // Handle server-side validation errors and success messages
    handleServerValidationErrors();
    handleSuccessfulSubmission();
}

// Initialize applicant type selection
function initializeApplicantTypeSelection() {
    console.log('🔧 Initializing applicant type selection...');
    
    // Check if any radio button is selected
    const selectedType = $('input[name="ApplicantType"]:checked').val();
    console.log('🔍 Initial ApplicantType selection:', selectedType);
    
    if (selectedType === 'Individual') {
        $('#noSelectionMessage').removeClass('show').addClass('hide').hide();
        $('#companyForm').removeClass('show').hide();
        $('#individualForm').removeClass('hide').addClass('show').show();
        console.log('👤 Initial state: Showing Individual form');
    } else if (selectedType === 'Company') {
        $('#noSelectionMessage').removeClass('show').addClass('hide').hide();
        $('#individualForm').removeClass('show').hide();
        $('#companyForm').removeClass('hide').addClass('show').show();
        console.log('🏢 Initial state: Showing Company form');
    } else {
        // Default: show message when no selection and hide both forms
        $('#noSelectionMessage').removeClass('hide').addClass('show').show();
        $('#individualForm').removeClass('show').hide();
        $('#companyForm').removeClass('show').hide();
        console.log('❓ Default state: Showing selection message (no selection)');
    }
    
    // Update hidden field
    $('#applicantTypeHidden').val(selectedType || '');
}

// ===============================================
// FILE UPLOAD INITIALIZATION 
// ===============================================
function initializeFileUpload() {
    console.log('🔧 Initializing file upload system...');

    $('.file-input').each(function () {
        const $input = $(this);
        const documentType = $input.attr('id');
        const $uploadArea = $input.closest('.upload-area');
        const $uploadItem = $input.closest('.upload-item');

        console.log(`📄 Setting up upload for: ${documentType}`);

        // File input change event
        $input.on('change', function (e) {
            const file = e.target.files[0];
            if (file) {
                console.log(`📄 File selected for ${documentType}:`, file.name);
                handleFileSelection(this, file);
            }
        });

        // Click to upload
        $uploadArea.find('.upload-placeholder').on('click', function (e) {
            e.preventDefault();
            console.log(`🖱️ Click to upload for ${documentType}`);
            $input[0].click();
        });

        // Remove file button
        $uploadArea.find('.btn-remove').on('click', function (e) {
            e.preventDefault();
            console.log(`🗑️ Remove file for ${documentType}`);
            removeFile($input);
        });

        // Drag & Drop events
        $uploadArea.on('dragover', function (e) {
            e.preventDefault();
            e.stopPropagation();
            $(this).addClass('drag-over');
        });

        $uploadArea.on('dragleave', function (e) {
            e.preventDefault();
            e.stopPropagation();
            $(this).removeClass('drag-over');
        });

        $uploadArea.on('drop', function (e) {
            e.preventDefault();
            e.stopPropagation();
            $(this).removeClass('drag-over');

            console.log(`📁 File dropped on ${documentType}`);

            const files = e.originalEvent.dataTransfer.files;
            if (files.length > 0) {
                const file = files[0];
                console.log(`📄 Dropped file: ${file.name}`);

                // Set the file to the input
                const dt = new DataTransfer();
                dt.items.add(file);
                $input[0].files = dt.files;

                // Handle the file
                handleFileSelection($input[0], file);
            }
        });

        // Prevent default drag behaviors on document
        $uploadArea.on('dragenter', function (e) {
            e.preventDefault();
            e.stopPropagation();
        });
    });

    console.log('✅ File upload system initialized');
}

// ===============================================
// STEP NAVIGATION
// ===============================================
function nextStep() {
    console.log(`📍 Attempting to move from step ${currentStep} to ${currentStep + 1}`);

    if (validateCurrentStep()) {
        if (currentStep < totalSteps) {
            showStep(currentStep + 1);
        }
    } else {
        // Don't show generic alert here - let each validation function handle its own alerts
        console.log('❌ Step validation failed - alert handled by specific validation function');
    }
}

function prevStep() {
    console.log(`📍 Moving back from step ${currentStep} to ${currentStep - 1}`);

    if (currentStep > 1) {
        showStep(currentStep - 1);
    }
}

function showStep(step) {
    if (step < 1 || step > totalSteps) return;

    console.log(`🔄 Switching to step ${step}`);

    const direction = step > currentStep ? 'next' : 'prev';
    currentStep = step;
    $('#currentStepNumber').text(step);

    // Update step visibility with animation
    $('.form-step').removeClass('active prev');
    const $targetStep = $(`.form-step[data-step="${step}"]`);
    $targetStep.addClass(`active ${direction === 'prev' ? 'prev' : ''}`);

    updateStepProgress();
    updateNavigationButtons();

    // Smooth scroll to top
    $('.create-container').animate({ scrollTop: 0 }, 300);

    // Trigger step-specific actions
    onStepChange(step);
}

function onStepChange(step) {
    // Step-specific logic can be added here
    switch (step) {
        case 2:
            // Shipping details step - integration point with main script
            console.log('💼 Entered shipping details step');
            break;
        case 3:
            // Livestock step - integration point with quota system
            console.log('🐄 Entered livestock details step');
            // Load quota information if available
            if (window.permitCreateManager) {
                setTimeout(() => {
                    window.permitCreateManager.loadQuotaInfo();
                }, 500);
            }
            break;
        case 4:
            // Documents step
            console.log('📄 Entered documents step');
            updateUploadProgress();
            break;
    }
}

function updateStepProgress() {
    $('.step-indicator').each(function () {
        const stepNum = parseInt($(this).data('step'));
        const $this = $(this);

        $this.removeClass('active completed');

        if (stepNum === currentStep) {
            $this.addClass('active');
        } else if (stepNum < currentStep) {
            $this.addClass('completed');
        }
    });
}

function updateNavigationButtons() {
    const $prevBtn = $('#prevBtn');
    const $nextBtn = $('#nextBtn');
    const $submitBtn = $('#submitBtn');

    if (currentStep === 1) {
        $prevBtn.hide();
    } else {
        $prevBtn.show();
    }

    if (currentStep === totalSteps) {
        $nextBtn.hide();
        $submitBtn.show();
    } else {
        $nextBtn.show();
        $submitBtn.hide();
    }
}

// ===============================================
// VALIDATION FUNCTIONS - ENHANCED
// ===============================================
function validateCurrentStep() {
    console.log(`🔍 Validating step ${currentStep}`);

    const rules = stepValidationRules[currentStep];

    if (typeof rules === 'function') {
        const isValid = rules();
        console.log(`📊 Step ${currentStep} validation result: ${isValid ? 'PASSED' : 'FAILED'}`);
        return isValid;
    } else if (Array.isArray(rules)) {
        return validateRequiredFields(rules);
    }

    return true;
}

function validateRequiredFields(fieldNames) {
    let isValid = true;

    fieldNames.forEach(fieldName => {
        const $field = $(`[name="${fieldName}"]`);
        const value = $field.val()?.trim();

        if (!value) {
            $field.addClass('is-invalid');
            isValid = false;
            console.log(`❌ Field ${fieldName} is empty`);
        } else {
            $field.removeClass('is-invalid');
            console.log(`✅ Field ${fieldName} is valid`);
        }
    });

    return isValid;
}

function validateCompanyStep() {
    console.log('🏢 Validating step 1 (Company/Individual)...');

    let isValid = true;
    const errors = [];

    // Get applicant type - check both radio button and hidden field (sinkron dengan inline script)
    const applicantType = $('input[name="ApplicantType"]:checked').val() || $('#applicantTypeHidden').val();

    console.log(`📋 Current applicant type: ${applicantType}`);

    if (!applicantType) {
        errors.push('Pilih tipe pemohon terlebih dahulu');
        isValid = false;
        console.log('❌ No applicant type selected');
    } else if (applicantType === 'Company') {
        // Validasi untuk form perusahaan - SAMA dengan logic di Create.cshtml
        isValid = validateCompanyForm(errors);
    } else if (applicantType === 'Individual') {
        // Validasi untuk form perorangan - SAMA dengan logic di Create.cshtml  
        isValid = validateIndividualForm(errors);
    }

    // Show errors if any
    if (errors.length > 0) {
        showAlert('Mohon lengkapi: ' + errors.join(', '), 'warning');
        console.log('❌ Validation errors:', errors);
    }

    console.log(`📊 Step 1 validation result: ${isValid ? 'PASSED' : 'FAILED'}`);
    return isValid;
}

function validateCompanyForm(errors) {
    let isValid = true;

    console.log('🏢 Validating company form...');

    // Clear previous validation state for individual form
    clearFormValidation('#individualForm');

    // Check if applicant type is actually Company
    const applicantType = $('input[name="ApplicantType"]:checked').val();
    if (applicantType !== 'Company') {
        console.log('⚠️ Skipping CompanyName validation - applicant type is not Company');
        return true; // Skip validation if not Company
    }

    // Validasi nama perusahaan
    const companyName = $('[name="CompanyName"]').val()?.trim();
    console.log('🔍 CompanyName field value:', companyName);
    
    if (!companyName) {
        $('[name="CompanyName"]').addClass('is-invalid');
        errors.push('Nama perusahaan harus diisi');
        isValid = false;
        console.log('❌ Company name is empty');
    } else {
        $('[name="CompanyName"]').removeClass('is-invalid').addClass('is-valid');
        console.log('✅ Company name is valid');
    }

    // Validasi provinsi perusahaan 
    const province = $('[name="CompanyProvince"]').val()?.trim();
    if (!province) {
        $('#companyProvinceSelect').addClass('is-invalid');
        errors.push('Provinsi perusahaan harus dipilih');
        isValid = false;
        console.log('❌ Company province is empty');
    } else {
        $('#companyProvinceSelect').removeClass('is-invalid').addClass('is-valid');
        console.log('✅ Company province is valid');
    }

    // Validasi kabupaten perusahaan
    const regency = $('[name="CompanyRegency"]').val()?.trim();
    if (!regency) {
        $('#companyRegencySelect').addClass('is-invalid');
        errors.push('Kabupaten/Kota perusahaan harus dipilih');
        isValid = false;
        console.log('❌ Company regency is empty');
    } else {
        $('#companyRegencySelect').removeClass('is-invalid').addClass('is-valid');
        console.log('✅ Company regency is valid');
    }

    // Validasi kecamatan
    const district = $('[name="AddressDistrict"]').val()?.trim();
    if (!district) {
        $('#companyDistrictSelect').addClass('is-invalid');
        errors.push('Kecamatan harus dipilih');
        isValid = false;
        console.log('❌ Company district is empty');
    } else {
        $('#companyDistrictSelect').removeClass('is-invalid').addClass('is-valid');
        console.log('✅ Company district is valid');
    }

    // Validasi desa/kelurahan
    const village = $('[name="AddressVillage"]').val()?.trim();
    if (!village) {
        $('#companyVillageSelect').addClass('is-invalid');
        errors.push('Desa/Kelurahan harus dipilih');
        isValid = false;
        console.log('❌ Company village is empty');
    } else {
        $('#companyVillageSelect').removeClass('is-invalid').addClass('is-valid');
        console.log('✅ Company village is valid');
    }

    // Validasi nama jalan
    const street = $('[name="AddressStreet"]').val()?.trim();
    if (!street) {
        $('[name="AddressStreet"]').addClass('is-invalid');
        errors.push('Nama jalan harus diisi');
        isValid = false;
        console.log('❌ Company street address is empty');
    } else {
        $('[name="AddressStreet"]').removeClass('is-invalid').addClass('is-valid');
        console.log('✅ Company street address is valid');
    }

    // Validasi RT
    const rt = $('[name="AddressRT"]').val()?.trim();
    if (!rt) {
        $('[name="AddressRT"]').addClass('is-invalid');
        errors.push('RT harus diisi');
        isValid = false;
        console.log('❌ Company RT is empty');
    } else {
        $('[name="AddressRT"]').removeClass('is-invalid').addClass('is-valid');
        console.log('✅ Company RT is valid');
    }

    // Validasi RW
    const rw = $('[name="AddressRW"]').val()?.trim();
    if (!rw) {
        $('[name="AddressRW"]').addClass('is-invalid');
        errors.push('RW harus diisi');
        isValid = false;
        console.log('❌ Company RW is empty');
    } else {
        $('[name="AddressRW"]').removeClass('is-invalid').addClass('is-valid');
        console.log('✅ Company RW is valid');
    }

    console.log(`🏢 Company form validation result: ${isValid ? 'PASSED' : 'FAILED'}`);
    return isValid;
}

function validateIndividualForm(errors) {
    let isValid = true;

    console.log('👤 Validating individual form...');

    // Clear previous validation state for company form
    clearFormValidation('#companyForm');

    // Check if applicant type is actually Individual
    const applicantType = $('input[name="ApplicantType"]:checked').val();
    if (applicantType !== 'Individual') {
        console.log('⚠️ Skipping IndividualName validation - applicant type is not Individual');
        return true; // Skip validation if not Individual
    }

    // Pastikan data profil diisi terlebih dahulu
    fillIndividualDataFromProfile();

    // Validasi nama lengkap - lebih fleksibel
    const individualName = $('[name="IndividualName"]').val()?.trim();
    if (!individualName) {
        $('[name="IndividualName"]').addClass('is-invalid');
        errors.push('Nama lengkap harus diisi');
        isValid = false;
        console.log('❌ Individual name is empty');
    } else {
        $('[name="IndividualName"]').removeClass('is-invalid').addClass('is-valid');
        console.log('✅ Individual name is valid:', individualName);
    }

    // Validasi provinsi individual (skip jika dropdown disembunyikan)
    const individualProvinceGroup = $('#individualProvinceGroup');
    if (individualProvinceGroup.is(':visible')) {
        const individualProvince = $('[name="IndividualProvince"]').val()?.trim();
        if (!individualProvince) {
            $('#individualProvinceSelect').addClass('is-invalid');
            errors.push('Provinsi harus dipilih');
            isValid = false;
            console.log('❌ Individual province is empty');
        } else {
            $('#individualProvinceSelect').removeClass('is-invalid').addClass('is-valid');
            console.log('✅ Individual province is valid');
        }
    } else {
        console.log('⚠️ Skipping Individual province validation - dropdown is hidden');
    }

    // Validasi kabupaten individual (skip jika dropdown disembunyikan)
    const individualRegencyGroup = $('#individualRegencyGroup');
    if (individualRegencyGroup.is(':visible')) {
        const individualRegency = $('[name="IndividualRegency"]').val()?.trim();
        if (!individualRegency) {
            $('#individualRegencySelect').addClass('is-invalid');
            errors.push('Kabupaten/Kota harus dipilih');
            isValid = false;
            console.log('❌ Individual regency is empty');
        } else {
            $('#individualRegencySelect').removeClass('is-invalid').addClass('is-valid');
            console.log('✅ Individual regency is valid');
        }
    } else {
        console.log('⚠️ Skipping Individual regency validation - dropdown is hidden');
    }

    // Validasi alamat lengkap individual - lebih fleksibel
    const individualAddress = $('[name="IndividualAddress"]').val()?.trim();
    if (!individualAddress) {
        $('[name="IndividualAddress"]').addClass('is-invalid');
        errors.push('Alamat lengkap harus diisi');
        isValid = false;
        console.log('❌ Individual address is empty');
    } else {
        $('[name="IndividualAddress"]').removeClass('is-invalid').addClass('is-valid');
        console.log('✅ Individual address is valid:', individualAddress);
    }

    console.log(`👤 Individual form validation result: ${isValid ? 'PASSED' : 'FAILED'}`);
    return isValid;
}


function clearFormValidation(formSelector) {
    console.log(`🧹 Clearing validation for: ${formSelector}`);

    // Remove validation classes from all inputs in the specified form
    $(formSelector + ' input, ' + formSelector + ' select, ' + formSelector + ' textarea')
        .removeClass('is-invalid is-valid');

    // Clear any error messages
    $(formSelector + ' .text-danger').text('');
}

// ===============================================
// STEP 2 VALIDATION - SHIPPING DETAILS
// ===============================================
function validateShippingStep() {
    console.log('🚢 Validating shipping step...');

    let isValid = true;
    const errors = [];

    // Check required shipping fields
    const requiredFields = [
        { field: 'OriginLocation', name: 'Asal Ternak' },
        { field: 'DestinationLocation', name: 'Tujuan Pengiriman' },
        { field: 'DeparturePort', name: 'Pelabuhan Keberangkatan' },
        { field: 'ArrivalPort', name: 'Pelabuhan Tujuan' }
    ];

    requiredFields.forEach(item => {
        const $field = $(`[name="${item.field}"]`);
        const value = $field.val()?.trim();

        if (!value) {
            errors.push(item.name);
            isValid = false;
            console.log(`❌ ${item.name} is empty`);
        } else {
            console.log(`✅ ${item.name} is valid`);
        }
    });

    if (errors.length > 0) {
        showAlert(`Mohon lengkapi: ${errors.join(', ')}`, 'warning');
        console.log('❌ Shipping validation errors:', errors);
    }

    console.log(`🚢 Shipping step validation result: ${isValid ? 'PASSED' : 'FAILED'}`);
    return isValid;
}

// ===============================================
// STEP 3 VALIDATION - LIVESTOCK
// ===============================================
function validateLivestockStep() {
    console.log('🐄 Validating livestock step...');

    let hasValidLivestock = false;
    let quotaExceededItems = [];
    let totalQuantity = 0;
    let missingFields = [];

    $('.livestock-item').each(function () {
        const type = $(this).find('.livestock-type').val();
        const quantity = parseInt($(this).find('.livestock-quantity').val()) || 0;
        const maxQuota = parseInt($(this).find('.livestock-quantity').attr('max')) || 999999;

        if (type && quantity > 0) {
            hasValidLivestock = true;
            totalQuantity += quantity;
            console.log(`✅ Found valid livestock: ${type} - ${quantity} ekor (max: ${maxQuota})`);
            
            // Check if quantity exceeds quota
            if (quantity > maxQuota) {
                quotaExceededItems.push({
                    type: type,
                    quantity: quantity,
                    maxQuota: maxQuota
                });
                console.log(`❌ Quota exceeded for ${type}: ${quantity} > ${maxQuota}`);
            }
        } else if (type && quantity === 0) {
            // Jenis ternak dipilih tapi jumlah 0
            missingFields.push(`Jumlah untuk ${type}`);
        } else if (!type && quantity > 0) {
            // Jumlah diisi tapi jenis ternak tidak dipilih
            missingFields.push('Jenis ternak');
        }
    });

    // Check for quota validation errors (integration with main script)
    const hasQuotaErrors = $('.livestock-quantity.error').length > 0;

    // Validasi field yang kosong
    if (!hasValidLivestock) {
        if (missingFields.length > 0) {
            showAlert(`Mohon lengkapi: ${missingFields.join(', ')}`, 'warning');
        } else {
            showAlert('Minimal harus ada satu jenis ternak dengan jumlah yang valid', 'warning');
        }
        console.log('❌ No valid livestock found');
        return false;
    }

    // Check for quota exceeded items - PRIORITAS UTAMA
    if (quotaExceededItems.length > 0) {
        let errorMessage = '⚠️ **KUOTA TERLAMPAUI** ⚠️\n\n';
        errorMessage += 'Jumlah ternak yang diminta melebihi kuota yang tersedia:\n\n';
        
        quotaExceededItems.forEach(item => {
            const excess = item.quantity - item.maxQuota;
            errorMessage += `• **${item.type}**: ${item.quantity.toLocaleString()} ekor\n`;
            errorMessage += `  └─ Kuota tersedia: ${item.maxQuota.toLocaleString()} ekor\n`;
            errorMessage += `  └─ Kelebihan: ${excess.toLocaleString()} ekor\n\n`;
        });
        
        errorMessage += '**Tindakan yang diperlukan:**\n';
        errorMessage += '• Kurangi jumlah ternak sesuai kuota yang tersedia\n';
        errorMessage += '• Atau pilih jenis ternak lain yang masih memiliki kuota cukup\n';
        errorMessage += '• Hubungi admin jika memerlukan penambahan kuota';
        
        showAlert(errorMessage, 'danger');
        console.log('❌ Quota exceeded for multiple items:', quotaExceededItems);
        return false;
    }

    // Check for other quota validation errors
    if (hasQuotaErrors) {
        showAlert('⚠️ **MASALAH KUOTA TERNAK** ⚠️\n\nMasih ada masalah dengan kuota ternak. Silakan periksa kembali jumlah yang diminta dan pastikan tidak melebihi batas yang ditentukan.', 'warning');
        console.log('❌ Quota validation errors found');
        return false;
    }

    // Additional validation: Check if total quantity is reasonable
    if (totalQuantity > 10000) {
        showAlert('⚠️ **JUMLAH TERNAK TERLALU BESAR** ⚠️\n\nTotal jumlah ternak yang diminta terlalu besar. Silakan periksa kembali jumlah yang diminta atau hubungi admin untuk konfirmasi.', 'warning');
        console.log('❌ Total quantity too large:', totalQuantity);
        return false;
    }

    console.log('✅ Livestock step validation passed');
    return true;
}

// ===============================================
// STEP 4 VALIDATION - DOCUMENTS
// ===============================================


function validateDocumentStep() {
    console.log('📄 Validating document step...');

    // ⭐ HITUNG HANYA DOKUMEN WAJIB (exclude DokumenOpsional)
    const requiredDocs = documentTypes.filter(doc => doc !== 'DokumenOpsional');
    const uploadedRequiredCount = requiredDocs.filter(doc => uploadedDocuments.has(doc)).length;

    console.log(`📊 Required documents uploaded: ${uploadedRequiredCount}/${totalRequiredFiles}`);

    if (uploadedRequiredCount < totalRequiredFiles) {
        showAlert(`Dokumen wajib belum lengkap. ${uploadedRequiredCount}/${totalRequiredFiles} dokumen telah diupload`, 'warning');
        console.log('❌ Required documents incomplete');
        return false;
    }

    // Validasi detail dokumen (tetap sama)
    const documentsWithDetails = ['SuratPermohonan', 'RekomendasiDinasProv', 'RekomendasiDaerahTujuan'];
    let detailsValid = true;
    documentsWithDetails.forEach(docType => {
        if (!validateDocumentDetails(docType)) {
            detailsValid = false;
            console.log(`❌ Document details invalid for: ${docType}`);
        }
    });

    // ⭐ VALIDASI KHUSUS DOKUMEN OPSIONAL (jika diupload)
    if (uploadedDocuments.has('DokumenOpsional')) {
        if (!validateOptionalDocumentDetails()) {
            detailsValid = false;
            console.log('❌ Optional document details invalid');
        }
    }

    if (!detailsValid) {
        showAlert('Mohon lengkapi detail dokumen yang diperlukan', 'warning');
        console.log('❌ Document details validation failed');
        return false;
    }

    console.log('✅ Document step validation passed');
    return true;
}

// ⭐ TAMBAH FUNCTION BARU (setelah function validateDocumentDetails)
function validateOptionalDocumentDetails() {
    const nameInput = document.getElementById('DokumenOpsionalNama');
    const fileInput = document.getElementById('DokumenOpsional');

    let isValid = true;

    // Jika ada file tapi nama kosong
    if (fileInput && fileInput.files && fileInput.files.length > 0) {
        if (!nameInput.value || nameInput.value.trim() === '') {
            showDocumentFieldError(nameInput, 'Nama dokumen harus diisi jika mengupload file');
            isValid = false;
        } else {
            clearDocumentFieldError(nameInput);
        }

        // Validasi tanggal dan nomor (opsional tapi jika diisi harus valid)
        const dateInput = document.getElementById('DokumenOpsionalTanggal');
        const numberInput = document.getElementById('DokumenOpsionalNomor');

        if (dateInput && dateInput.value) {
            const inputDate = new Date(dateInput.value);
            const today = new Date();
            today.setHours(0, 0, 0, 0);

            if (inputDate > today) {
                showDocumentFieldError(dateInput, 'Tanggal tidak boleh di masa depan');
                isValid = false;
            } else {
                clearDocumentFieldError(dateInput);
            }
        }

        // Nomor dokumen tidak wajib, tapi jika diisi tidak boleh kosong
        if (numberInput && numberInput.value && numberInput.value.trim() === '') {
            showDocumentFieldError(numberInput, 'Nomor dokumen tidak boleh kosong');
            isValid = false;
        } else if (numberInput) {
            clearDocumentFieldError(numberInput);
        }
    }

    return isValid;
}
//function validateDocumentStep() {
//    console.log('📄 Validating document step...');

//    const uploadedCount = uploadedDocuments.size;
//    const documentsWithDetails = ['SuratPermohonan', 'RekomendasiDinasProv', 'RekomendasiDaerahTujuan'];

//    console.log(`📊 Documents uploaded: ${uploadedCount}/${totalRequiredFiles}`);

//    if (uploadedCount < totalRequiredFiles) {
//        showAlert(`Dokumen belum lengkap. ${uploadedCount}/${totalRequiredFiles} dokumen telah diupload`, 'warning');
//        console.log('❌ Documents incomplete');
//        return false;
//    }

//    let detailsValid = true;
//    documentsWithDetails.forEach(docType => {
//        if (!validateDocumentDetails(docType)) {
//            detailsValid = false;
//            console.log(`❌ Document details invalid for: ${docType}`);
//        }
//    });

//    if (!detailsValid) {
//        showAlert('Mohon lengkapi tanggal dan nomor dokumen yang diperlukan', 'warning');
//        console.log('❌ Document details validation failed');
//        return false;
//    }

//    console.log('✅ Document step validation passed');
//    return true;
//}

// ===============================================
// FINAL VALIDATION - ALL STEPS
// ===============================================
function validateAllSteps() {
    console.log('🔍 Validating all steps before submission...');

    // Special check for applicant type
    const applicantType = $('input[name="ApplicantType"]:checked').val() || $('#applicantTypeHidden').val();
    console.log(`📋 Final applicant type check: ${applicantType}`);

    if (!applicantType) {
        showAlert('Tipe pemohon belum dipilih', 'error');
        console.log('❌ No applicant type selected');
        return false;
    }

    // Validate each step
    for (let step = 1; step <= totalSteps; step++) {
        const rules = stepValidationRules[step];

        if (typeof rules === 'function') {
            console.log(`📝 Validating step ${step}...`);
            if (!rules()) {
                console.log(`❌ Step ${step} validation failed`);
                showAlert(`Validasi step ${step} gagal. Silakan periksa kembali data yang diisi.`, 'error');

                // Navigate to failed step
                showStep(step);
                return false;
            }
            console.log(`✅ Step ${step} validation passed`);
        }
    }

    console.log('✅ All steps validation passed');
    return true;
}

// ===============================================
// DOCUMENT UPLOAD MANAGEMENT
// ===============================================
function handleFileSelection(inputElement, file) {
    const $input = $(inputElement);
    const documentType = $input.attr('id');
    const maxSize = parseInt($input.data('max-size')) || 5242880; // 5MB

    if (!validateFile(file, maxSize)) {
        $input.val('');
        return;
    }

    updateFilePreview($input, file);

    uploadedDocuments.set(documentType, {
        file: file,
        name: file.name,
        size: file.size
    });

    updateUploadProgress();
    updateDocumentChecklist();

    const $uploadItem = $input.closest('.upload-item');
    updateUploadStatus($uploadItem, true);

    const documentsWithDetails = ['SuratPermohonan', 'RekomendasiDinasProv', 'RekomendasiDaerahTujuan'];
    if (documentsWithDetails.includes(documentType)) {
        setTimeout(() => {
            validateDocumentDetails(documentType);
        }, 100);
    }

    showAlert(`File "${file.name}" berhasil dipilih`, 'success');
}

function removeFile($input) {
    const documentType = $input.attr('id');
    const $uploadArea = $input.closest('.upload-area');

    $input.val('');

    $uploadArea.find('.file-preview').hide();
    $uploadArea.find('.upload-placeholder').show();

    const $status = $uploadArea.closest('.upload-item').find('.upload-status');
    $status.html('<i class="fas fa-circle text-danger"></i> Belum Upload');

    uploadedDocuments.delete(documentType);

    updateUploadProgress();
    updateDocumentChecklist();

    clearDocumentDetailsValidation(documentType);

    showAlert('File berhasil dihapus', 'info');
}

function validateFile(file, maxSize) {
    const allowedTypes = ['application/pdf', 'image/jpeg', 'image/jpg', 'image/png'];
    const allowedExtensions = ['.pdf', '.jpg', '.jpeg', '.png'];

    if (file.size > maxSize) {
        const maxSizeMB = (maxSize / 1024 / 1024).toFixed(1);
        showAlert(`Ukuran file terlalu besar. Maksimal ${maxSizeMB}MB`, 'danger');
        return false;
    }

    const fileExtension = '.' + file.name.split('.').pop().toLowerCase();
    if (!allowedTypes.includes(file.type) && !allowedExtensions.includes(fileExtension)) {
        showAlert('Format file tidak didukung. Gunakan PDF, JPG, JPEG, atau PNG', 'danger');
        return false;
    }

    return true;
}

function updateFilePreview($input, file) {
    const $uploadArea = $input.closest('.upload-area');
    const $placeholder = $uploadArea.find('.upload-placeholder');
    const $preview = $uploadArea.find('.file-preview');

    $preview.find('.file-name').text(file.name);
    $preview.find('.file-size').text(formatFileSize(file.size));

    const fileIcon = getFileIcon(file.type, file.name);
    $preview.find('.file-info i').removeClass().addClass(fileIcon);

    const $status = $uploadArea.closest('.upload-item').find('.upload-status');
    $status.html('<i class="fas fa-check-circle text-success"></i> Sudah Upload');

    $placeholder.hide();
    $preview.show();
}

function getFileIcon(fileType, fileName) {
    const extension = fileName.split('.').pop().toLowerCase();

    switch (extension) {
        case 'pdf':
            return 'fas fa-file-pdf text-danger';
        case 'jpg':
        case 'jpeg':
        case 'png':
            return 'fas fa-file-image text-primary';
        default:
            return 'fas fa-file text-secondary';
    }
}

function updateUploadStatus($uploadItem, isUploaded) {
    const $status = $uploadItem.find('.upload-status');

    if (isUploaded) {
        $status.html('<i class="fas fa-check-circle text-success"></i> Berhasil Upload');
        $uploadItem.addClass('completed');
    } else {
        $status.html('<i class="fas fa-circle text-danger"></i> Belum Upload');
        $uploadItem.removeClass('completed');
    }
}

function updateUploadProgress() {
    const count = uploadedDocuments.size;
    const percentage = (count / totalRequiredFiles) * 100;

    $('#uploadProgress').css('width', percentage + '%');
    $('#uploadProgressText').text(`${count} dari ${totalRequiredFiles} dokumen telah diupload`);
}

function updateDocumentChecklist() {
    documentTypes.forEach(docType => {
        const $item = $(`.checklist-item[data-doc="${docType}"]`);
        const $icon = $item.find('i');

        if (uploadedDocuments.has(docType)) {
            if (docType === 'DokumenOpsional') {
                // ⭐ KHUSUS UNTUK DOKUMEN OPSIONAL
                $icon.removeClass('fas fa-circle text-info fas fa-times-circle text-danger')
                    .addClass('fas fa-check-circle text-success');
                $item.addClass('completed');
            } else {
                // Dokumen wajib
                $icon.removeClass('fas fa-times-circle text-danger')
                    .addClass('fas fa-check-circle text-success');
                $item.addClass('completed');
            }
        } else {
            if (docType === 'DokumenOpsional') {
                // ⭐ DOKUMEN OPSIONAL BELUM UPLOAD
                $icon.removeClass('fas fa-check-circle text-success fas fa-times-circle text-danger')
                    .addClass('fas fa-circle text-info');
                $item.removeClass('completed');
            } else {
                // Dokumen wajib belum upload
                $icon.removeClass('fas fa-check-circle text-success')
                    .addClass('fas fa-times-circle text-danger');
                $item.removeClass('completed');
            }
        }
    });
}

// ===============================================
// DOCUMENT DETAILS VALIDATION
// ===============================================
function initializeDocumentDetailsValidation() {
    const documentsWithDetails = ['SuratPermohonan', 'RekomendasiDinasProv', 'RekomendasiDaerahTujuan'];

    documentsWithDetails.forEach(docType => {
        const fileInput = document.getElementById(docType);
        const dateInput = document.getElementById(docType + 'Tanggal');
        const numberInput = document.getElementById(docType + 'Nomor');

        if (fileInput && dateInput && numberInput) {
            fileInput.addEventListener('change', function () {
                if (this.files && this.files.length > 0) {
                    validateDocumentDetails(docType);
                } else {
                    clearDocumentDetailsValidation(docType);
                }
            });

            dateInput.addEventListener('change', function () {
                validateDocumentDate(docType, this.value);
            });

            numberInput.addEventListener('input', function () {
                validateDocumentNumber(docType, this.value);
            });
        }
    });

    // ⭐ TAMBAH HANDLER UNTUK DOKUMEN OPSIONAL
    const optionalFileInput = document.getElementById('DokumenOpsional');
    const optionalNameInput = document.getElementById('DokumenOpsionalNama');
    const optionalDateInput = document.getElementById('DokumenOpsionalTanggal');
    const optionalNumberInput = document.getElementById('DokumenOpsionalNomor');

    if (optionalFileInput && optionalNameInput) {
        optionalFileInput.addEventListener('change', function () {
            if (this.files && this.files.length > 0) {
                validateOptionalDocumentDetails();
            } else {
                clearDocumentFieldError(optionalNameInput);
                if (optionalDateInput) clearDocumentFieldError(optionalDateInput);
                if (optionalNumberInput) clearDocumentFieldError(optionalNumberInput);
            }
        });

        optionalNameInput.addEventListener('input', function () {
            validateOptionalDocumentDetails();
        });

        if (optionalDateInput) {
            optionalDateInput.addEventListener('change', function () {
                validateOptionalDocumentDetails();
            });
        }

        if (optionalNumberInput) {
            optionalNumberInput.addEventListener('input', function () {
                validateOptionalDocumentDetails();
            });
        }
    }
}

function validateDocumentDetails(documentType) {
    const dateInput = document.getElementById(documentType + 'Tanggal');
    const numberInput = document.getElementById(documentType + 'Nomor');
    const fileInput = document.getElementById(documentType);

    let isValid = true;

    if (fileInput && fileInput.files && fileInput.files.length > 0) {
        if (!dateInput.value) {
            showDocumentFieldError(dateInput, 'Tanggal pengajuan harus diisi');
            isValid = false;
        } else {
            clearDocumentFieldError(dateInput);

            const inputDate = new Date(dateInput.value);
            const today = new Date();
            today.setHours(0, 0, 0, 0);

            if (inputDate > today) {
                showDocumentFieldError(dateInput, 'Tanggal tidak boleh di masa depan');
                isValid = false;
            }
        }

        if (!numberInput.value || numberInput.value.trim() === '') {
            showDocumentFieldError(numberInput, 'Nomor dokumen harus diisi');
            isValid = false;
        } else {
            clearDocumentFieldError(numberInput);
        }
    }

    return isValid;
}

function validateDocumentDate(documentType, dateValue) {
    const dateInput = document.getElementById(documentType + 'Tanggal');
    const fileInput = document.getElementById(documentType);

    if (!dateValue && fileInput && fileInput.files && fileInput.files.length > 0) {
        showDocumentFieldError(dateInput, 'Tanggal pengajuan harus diisi');
        return false;
    } else if (dateValue) {
        const inputDate = new Date(dateValue);
        const today = new Date();
        today.setHours(0, 0, 0, 0);

        if (inputDate > today) {
            showDocumentFieldError(dateInput, 'Tanggal tidak boleh di masa depan');
            return false;
        } else {
            clearDocumentFieldError(dateInput);
        }
    }

    return true;
}

function validateDocumentNumber(documentType, numberValue) {
    const numberInput = document.getElementById(documentType + 'Nomor');
    const fileInput = document.getElementById(documentType);

    if (!numberValue || numberValue.trim() === '') {
        if (fileInput && fileInput.files && fileInput.files.length > 0) {
            showDocumentFieldError(numberInput, 'Nomor dokumen harus diisi');
            return false;
        }
    } else {
        clearDocumentFieldError(numberInput);
    }

    return true;
}

function showDocumentFieldError(field, message) {
    clearDocumentFieldError(field);

    const errorElement = document.createElement('span');
    errorElement.className = 'text-danger document-field-error';
    errorElement.style.fontSize = '0.75rem';
    errorElement.style.marginTop = '0.25rem';
    errorElement.style.display = 'block';
    errorElement.textContent = message;

    field.parentNode.appendChild(errorElement);
    field.classList.add('is-invalid');
}

function clearDocumentFieldError(field) {
    const existingError = field.parentNode.querySelector('.document-field-error');
    if (existingError) {
        existingError.remove();
    }
    field.classList.remove('is-invalid');
}

function clearDocumentDetailsValidation(documentType) {
    const dateInput = document.getElementById(documentType + 'Tanggal');
    const numberInput = document.getElementById(documentType + 'Nomor');

    if (dateInput) clearDocumentFieldError(dateInput);
    if (numberInput) clearDocumentFieldError(numberInput);
}

// ===============================================
// EVENT HANDLERS
// ===============================================
function initializeEventHandlers() {
    console.log('🔧 Initializing event handlers...');

    // Navigation buttons
    $('#nextBtn').on('click', nextStep);
    $('#prevBtn').on('click', prevStep);

    // Radio button selection for ApplicantType
    $('input[name="ApplicantType"]').on('change', function() {
        const selectedType = $(this).val();
        console.log('🔘 ApplicantType changed to:', selectedType);
        
        // Remove active class from all radio items
        $('.radio-item').removeClass('active');
        
        // Add active class to selected radio item
        $(this).closest('.radio-item').addClass('active');
        
        if (selectedType === 'Individual') {
            $('#noSelectionMessage').removeClass('show').addClass('hide').fadeOut(300);
            $('#companyForm').removeClass('show').fadeOut(300, function() {
                $('#individualForm').removeClass('hide').addClass('show').fadeIn(300);
                
                // Fill individual data from profile after form is shown
                setTimeout(() => {
                    fillIndividualDataFromProfile();
                }, 100);
            });
            console.log('👤 Showing Individual form, hiding Company form and message');
        } else if (selectedType === 'Company') {
            $('#noSelectionMessage').removeClass('show').addClass('hide').fadeOut(300);
            $('#individualForm').removeClass('show').fadeOut(300, function() {
                $('#companyForm').removeClass('hide').addClass('show').fadeIn(300);
            });
            console.log('🏢 Showing Company form, hiding Individual form and message');
        }
        
        // Update hidden field for form submission
        $('#applicantTypeHidden').val(selectedType);
        
        // Clear any existing validation errors when switching forms
        clearFormValidation('#individualForm');
        clearFormValidation('#companyForm');
        
        // Don't trigger validation automatically - let user fill the form first
        console.log('✅ Form switched, validation will be triggered on next button click');
    });

    
    console.log('🔍 Radio button elements found:', $('input[name="ApplicantType"]').length);
    $('input[name="ApplicantType"]').each(function(index) {
        console.log(`  Radio ${index + 1}:`, $(this).attr('id'), 'value:', $(this).val());
    });

    // Additional click handler for radio labels
    $('.radio-label').on('click', function(e) {
        const radioInput = $(this).prev('input[type="radio"]');
        if (radioInput.length > 0) {
            radioInput.prop('checked', true).trigger('change');
            console.log('🖱️ Radio label clicked, triggering change event');
        }
    });

    // Form submission with enhanced validation
    $('#permitForm').on('submit', function (e) {
        console.log('📤 Form submission initiated...');


        const formData = new FormData(this);
        for (let [key, value] of formData.entries()) {
            console.log(`  ${key}: '${value}'`);
        }


        console.log('  CompanyName:', $('[name="CompanyName"]').val());
        console.log('  IndividualName:', $('[name="IndividualName"]').val());
        console.log('  ApplicantType:', $('input[name="ApplicantType"]:checked').val());
        console.log('  CompanyProvince:', $('[name="CompanyProvince"]').val());
        console.log('  IndividualProvince:', $('[name="IndividualProvince"]').val());
        console.log('  CompanyRegency:', $('[name="CompanyRegency"]').val());
        console.log('  IndividualRegency:', $('[name="IndividualRegency"]').val());
        console.log('  AddressStreet:', $('[name="AddressStreet"]').val());
        console.log('  IndividualAddress:', $('[name="IndividualAddress"]').val());

        const companyNameField = $('[name="CompanyName"]');
        const individualNameField = $('[name="IndividualName"]');
        console.log('  CompanyName field exists:', companyNameField.length > 0);
        console.log('  IndividualName field exists:', individualNameField.length > 0);

        // Final validation check
        if (!validateAllSteps()) {
            e.preventDefault();
            showAlert('Mohon lengkapi semua langkah sebelum submit', 'danger');
            return false;
        }

        // Additional check for applicant type
        const applicantType = $('input[name="ApplicantType"]:checked').val() || $('#applicantTypeHidden').val();
        if (!applicantType) {
            e.preventDefault();
            showAlert('Tipe pemohon belum dipilih. Silakan pilih "Perorangan" atau "Perusahaan".', 'danger');
            return false;
        }

        // Validate applicant-specific data with improved logic
        if (applicantType === 'Individual') {
            const name = $('input[name="IndividualName"]').val()?.trim();
            const address = $('textarea[name="IndividualAddress"]').val()?.trim();

            console.log('Individual form data check:', { name, address });

            if (!name || !address) {
                e.preventDefault();
                showAlert('Mohon lengkapi nama lengkap dan alamat untuk pemohon perorangan.', 'danger');
                showStep(1); // Navigate back to step 1
                return false;
            }
            
            // Clear company fields for Individual applicant
            $('[name="CompanyName"]').val('');
            $('[name="CompanyProvince"]').val('');
            $('[name="CompanyRegency"]').val('');
            $('[name="AddressStreet"]').val('');
            
            // Clear individual province/regency fields if dropdowns are hidden
            if (!$('#individualProvinceGroup').is(':visible')) {
                $('[name="IndividualProvince"]').val('');
            }
            if (!$('#individualRegencyGroup').is(':visible')) {
                $('[name="IndividualRegency"]').val('');
            }
            
            console.log('✅ Individual form validation passed');
        } else if (applicantType === 'Company') {
            const companyName = $('input[name="CompanyName"]').val()?.trim();
            const companyProvince = $('input[name="CompanyProvince"]').val()?.trim();
            const companyRegency = $('input[name="CompanyRegency"]').val()?.trim();
            const district = $('input[name="AddressDistrict"]').val()?.trim();
            const village = $('input[name="AddressVillage"]').val()?.trim();
            const street = $('input[name="AddressStreet"]').val()?.trim();
            const rt = $('input[name="AddressRT"]').val()?.trim();
            const rw = $('input[name="AddressRW"]').val()?.trim();

            console.log('Company form data check:', { companyName, companyProvince, companyRegency, district, village, street, rt, rw });

            if (!companyName || !companyProvince || !companyRegency || !district || !village || !street || !rt || !rw) {
                e.preventDefault();
                showAlert('Mohon lengkapi semua field untuk perusahaan (Nama, Provinsi, Kabupaten, Kecamatan, Desa/Kelurahan, Nama Jalan, RT, RW).', 'danger');
                showStep(1); // Navigate back to step 1
                return false;
            }
            
            // Clear individual fields for Company applicant
            $('[name="IndividualName"]').val('');
            $('[name="IndividualProvince"]').val('');
            $('[name="IndividualRegency"]').val('');
            $('[name="IndividualAddress"]').val('');
            
            // Clear individual province/regency fields if dropdowns are hidden
            if (!$('#individualProvinceGroup').is(':visible')) {
                $('[name="IndividualProvince"]').val('');
            }
            if (!$('#individualRegencyGroup').is(':visible')) {
                $('[name="IndividualRegency"]').val('');
            }
            
            console.log('✅ Company form validation passed');
        }

        // Show loading state
        $('#submitBtn').html('<i class="fas fa-spinner fa-spin"></i> Menyimpan...').prop('disabled', true);
        showAlert('Sedang memproses permohonan...', 'info');


        console.log('🔍 Final form data before submission:');
        console.log('ApplicantType:', $('input[name="ApplicantType"]:checked').val());
        console.log('CompanyName:', $('[name="CompanyName"]').val());
        console.log('IndividualName:', $('[name="IndividualName"]').val());
        console.log('CompanyProvince:', $('[name="CompanyProvince"]').val());
        console.log('IndividualProvince:', $('[name="IndividualProvince"]').val());
        console.log('CompanyRegency:', $('[name="CompanyRegency"]').val());
        console.log('IndividualRegency:', $('[name="IndividualRegency"]').val());
        console.log('AddressStreet:', $('[name="AddressStreet"]').val());
        console.log('IndividualAddress:', $('[name="IndividualAddress"]').val());

        console.log('✅ Form validation passed, proceeding with submission...');
        
        // Don't prevent default - let the form submit normally
        // The server will handle validation and return appropriate response
    });

    // Field validation on blur - only for required fields, don't show errors immediately
    $('input[required], textarea[required], select[required]').on('blur', function () {
        const value = $(this).val()?.trim();
        
        // Only show valid state if there's a value, don't show invalid state on blur
        if (value) {
            $(this).removeClass('is-invalid').addClass('is-valid');
        } else {
            // Remove both classes to keep field neutral
            $(this).removeClass('is-invalid is-valid');
        }
    });

    // Monitor CompanyName field - only show visual feedback, don't trigger validation
    $('[name="CompanyName"]').on('input change', function() {
        const applicantType = $('input[name="ApplicantType"]:checked').val();
        const value = $(this).val()?.trim();
        
        // Only show visual feedback if applicant type is Company
        if (applicantType === 'Company') {
            console.log('🔍 CompanyName field changed:', value);
            
            // Remove validation classes to clear previous state
            $(this).removeClass('is-invalid is-valid');
            
            // Only add valid class if there's a value (don't add invalid class automatically)
            if (value) {
                $(this).addClass('is-valid');
                console.log('✅ CompanyName field has value');
            }
        } else {
            // Clear validation for Individual applicant type
            $(this).removeClass('is-invalid is-valid');
        }
    });

    // Monitor AddressStreet field - dynamic color feedback
    $('[name="AddressStreet"]').on('input change', function() {
        const applicantType = $('input[name="ApplicantType"]:checked').val();
        const value = $(this).val()?.trim();
        
        // Only show visual feedback if applicant type is Company
        if (applicantType === 'Company') {
            console.log('🔍 AddressStreet field changed:', value);
            
            // Remove validation classes to clear previous state
            $(this).removeClass('is-invalid is-valid');
            
            // Only add valid class if there's a value (don't add invalid class automatically)
            if (value) {
                $(this).addClass('is-valid');
                console.log('✅ AddressStreet field has value');
            }
        } else {
            // Clear validation for Individual applicant type
            $(this).removeClass('is-invalid is-valid');
        }
    });

    // Monitor AddressRT field - dynamic color feedback
    $('[name="AddressRT"]').on('input change', function() {
        const applicantType = $('input[name="ApplicantType"]:checked').val();
        const value = $(this).val()?.trim();
        
        // Only show visual feedback if applicant type is Company
        if (applicantType === 'Company') {
            console.log('🔍 AddressRT field changed:', value);
            
            // Remove validation classes to clear previous state
            $(this).removeClass('is-invalid is-valid');
            
            // Only add valid class if there's a value (don't add invalid class automatically)
            if (value) {
                $(this).addClass('is-valid');
                console.log('✅ AddressRT field has value');
            }
        } else {
            // Clear validation for Individual applicant type
            $(this).removeClass('is-invalid is-valid');
        }
    });

    // Monitor AddressRW field - dynamic color feedback
    $('[name="AddressRW"]').on('input change', function() {
        const applicantType = $('input[name="ApplicantType"]:checked').val();
        const value = $(this).val()?.trim();
        
        // Only show visual feedback if applicant type is Company
        if (applicantType === 'Company') {
            console.log('🔍 AddressRW field changed:', value);
            
            // Remove validation classes to clear previous state
            $(this).removeClass('is-invalid is-valid');
            
            // Only add valid class if there's a value (don't add invalid class automatically)
            if (value) {
                $(this).addClass('is-valid');
                console.log('✅ AddressRW field has value');
            }
        } else {
            // Clear validation for Individual applicant type
            $(this).removeClass('is-invalid is-valid');
        }
    });

    // Monitor AddressDistrict dropdown - dynamic color feedback
    $('[name="AddressDistrict"]').on('change', function() {
        const applicantType = $('input[name="ApplicantType"]:checked').val();
        const value = $(this).val()?.trim();
        
        // Only show visual feedback if applicant type is Company
        if (applicantType === 'Company') {
            console.log('🔍 AddressDistrict field changed:', value);
            
            // Remove validation classes to clear previous state
            $('#companyDistrictSelect').removeClass('is-invalid is-valid');
            
            // Only add valid class if there's a value (don't add invalid class automatically)
            if (value) {
                $('#companyDistrictSelect').addClass('is-valid');
                console.log('✅ AddressDistrict field has value');
            }
        } else {
            // Clear validation for Individual applicant type
            $('#companyDistrictSelect').removeClass('is-invalid is-valid');
        }
    });

    // Monitor AddressVillage dropdown - dynamic color feedback
    $('[name="AddressVillage"]').on('change', function() {
        const applicantType = $('input[name="ApplicantType"]:checked').val();
        const value = $(this).val()?.trim();
        
        // Only show visual feedback if applicant type is Company
        if (applicantType === 'Company') {
            console.log('🔍 AddressVillage field changed:', value);
            
            // Remove validation classes to clear previous state
            $('#companyVillageSelect').removeClass('is-invalid is-valid');
            
            // Only add valid class if there's a value (don't add invalid class automatically)
            if (value) {
                $('#companyVillageSelect').addClass('is-valid');
                console.log('✅ AddressVillage field has value');
            }
        } else {
            // Clear validation for Individual applicant type
            $('#companyVillageSelect').removeClass('is-invalid is-valid');
        }
    });

    // Monitor CompanyProvince dropdown - dynamic color feedback
    $('[name="CompanyProvince"]').on('change', function() {
        const applicantType = $('input[name="ApplicantType"]:checked').val();
        const value = $(this).val()?.trim();
        
        // Only show visual feedback if applicant type is Company
        if (applicantType === 'Company') {
            console.log('🔍 CompanyProvince field changed:', value);
            
            // Remove validation classes to clear previous state
            $('#companyProvinceSelect').removeClass('is-invalid is-valid');
            
            // Only add valid class if there's a value (don't add invalid class automatically)
            if (value) {
                $('#companyProvinceSelect').addClass('is-valid');
                console.log('✅ CompanyProvince field has value');
            }
        } else {
            // Clear validation for Individual applicant type
            $('#companyProvinceSelect').removeClass('is-invalid is-valid');
        }
    });

    // Monitor CompanyRegency dropdown - dynamic color feedback
    $('[name="CompanyRegency"]').on('change', function() {
        const applicantType = $('input[name="ApplicantType"]:checked').val();
        const value = $(this).val()?.trim();
        
        // Only show visual feedback if applicant type is Company
        if (applicantType === 'Company') {
            console.log('🔍 CompanyRegency field changed:', value);
            
            // Remove validation classes to clear previous state
            $('#companyRegencySelect').removeClass('is-invalid is-valid');
            
            // Only add valid class if there's a value (don't add invalid class automatically)
            if (value) {
                $('#companyRegencySelect').addClass('is-valid');
                console.log('✅ CompanyRegency field has value');
            }
        } else {
            // Clear validation for Individual applicant type
            $('#companyRegencySelect').removeClass('is-invalid is-valid');
        }
    });

    // Additional monitoring for all form fields
    $('input, textarea, select').on('change', function() {
        const fieldName = $(this).attr('name');
        const fieldValue = $(this).val();
        console.log(`🔍 Field changed - ${fieldName}: '${fieldValue}'`);
    });

    // Remove auto-fill for CompanyName field to prevent validation issues
    // setTimeout(function() {
    //     const companyNameField = $('[name="CompanyName"]');
    //     if (companyNameField.length > 0 && !companyNameField.val()) {
    //         console.log('🔧 Setting default value for CompanyName field');
    //         companyNameField.val('Test Company Name');
    //         companyNameField.trigger('change');
    //     }
    // }, 1000);

    // Keyboard shortcuts
    $(document).on('keydown', function (e) {
        if (e.ctrlKey) {
            switch (e.key) {
                case 'ArrowLeft':
                    e.preventDefault();
                    if (currentStep > 1) prevStep();
                    break;
                case 'ArrowRight':
                    e.preventDefault();
                    if (currentStep < totalSteps && validateCurrentStep()) nextStep();
                    break;
            }
        }
    });

    console.log('✅ Event handlers initialized');
}

// Function validateField removed - validation now only happens on form submission or next button click

// ===============================================
// PROFILE DATA FUNCTIONS
// ===============================================
function fillIndividualDataFromProfile() {
    console.log('🔧 Filling individual data from profile...');
    
    // Check if userProfileData is available (defined in Create.cshtml)
    if (typeof userProfileData !== 'undefined') {
        console.log('📋 User profile data found:', userProfileData);
        
        // Fill nama lengkap from profile
        if (userProfileData.namaLengkap && userProfileData.namaLengkap !== '' && userProfileData.namaLengkap !== 'null') {
            $('[name="IndividualName"]').val(userProfileData.namaLengkap);
            console.log('✅ Nama lengkap diisi dari profil:', userProfileData.namaLengkap);
        } else {
            console.log('⚠️ Nama lengkap tidak tersedia di profil');
        }
        
        // Fill alamat from profile
        if (userProfileData.alamat && userProfileData.alamat !== '' && userProfileData.alamat !== 'null') {
            $('[name="IndividualAddress"]').val(userProfileData.alamat);
            console.log('✅ Alamat diisi dari profil:', userProfileData.alamat);
        } else {
            console.log('⚠️ Alamat tidak tersedia di profil');
        }
    } else {
        console.log('⚠️ userProfileData tidak tersedia');
    }
    
    // Log current field values after filling
    console.log('📝 Current field values after profile fill:');
    console.log('  IndividualName:', $('[name="IndividualName"]').val());
    console.log('  IndividualAddress:', $('[name="IndividualAddress"]').val());
}

// ===============================================
// UTILITY FUNCTIONS
// ===============================================
function formatFileSize(bytes) {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
}

function showAlert(message, type) {
    // Remove existing alerts
    $('.custom-alert').remove();

    const alertClass = {
        'success': 'alert-success',
        'danger': 'alert-danger',
        'warning': 'alert-warning',
        'info': 'alert-info',
        'error': 'alert-danger'
    }[type] || 'alert-info';

    const icon = {
        'success': 'fa-check-circle',
        'danger': 'fa-exclamation-circle',
        'warning': 'fa-exclamation-triangle',
        'info': 'fa-info-circle',
        'error': 'fa-exclamation-circle'
    }[type] || 'fa-info-circle';

    // Convert markdown-style formatting to HTML
    let formattedMessage = message
        .replace(/\*\*(.*?)\*\*/g, '<strong>$1</strong>') // Bold text
        .replace(/\n/g, '<br>') // Line breaks
        .replace(/•/g, '•') // Bullet points
        .replace(/└─/g, '└─'); // Tree structure

    const alert = $(`
        <div class="alert ${alertClass} alert-dismissible fade show custom-alert"
             style="position: fixed; top: 20px; right: 20px; z-index: 9999; max-width: 500px; 
                    box-shadow: 0 4px 15px rgba(0,0,0,0.2); white-space: pre-line;">
            <div style="display: flex; align-items: flex-start; gap: 10px;">
                <i class="fas ${icon}" style="margin-top: 2px; flex-shrink: 0;"></i>
                <div style="flex: 1; line-height: 1.5;">${formattedMessage}</div>
            </div>
            <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close">
                <span aria-hidden="true">&times;</span>
            </button>
        </div>
    `);

    $('body').append(alert);

    // Longer delay for quota-related alerts
    const delay = (type === 'danger' || type === 'error') ? 8000 : 4000;
    setTimeout(() => {
        alert.fadeOut(300, () => alert.remove());
    }, delay);
}

// ===============================================
// PUBLIC API
// ===============================================
window.MultiStepForm = {
    nextStep,
    prevStep,
    showStep,
    validateCurrentStep,
    validateAllSteps,
    currentStep: () => currentStep,
    totalSteps: () => totalSteps,
    uploadedDocuments: () => uploadedDocuments,
    validateStep1: validateCompanyStep,
    validateStep2: validateShippingStep,
    validateStep3: validateLivestockStep,
    validateStep4: validateDocumentStep,
    checkApplicantType: function () {
        return {
            radioValue: $('input[name="ApplicantType"]:checked').val(),
            hiddenValue: $('#applicantTypeHidden').val(),
            formsVisible: {
                individual: $('#individualForm').is(':visible'),
                company: $('#companyForm').is(':visible')
            }
        };
    }
};

// ===============================================
// INTEGRATION HELPERS
// ===============================================
// These functions help integrate with the main script in Create.cshtml

// Called from main script when quota validation changes
window.onQuotaValidationChange = function (hasErrors) {
    console.log(`🔄 Quota validation status changed: ${hasErrors ? 'Has Errors' : 'No Errors'}`);
    // This allows the main script to communicate validation status to the multi-step form
};

// Called from main script when shipping details are validated
window.onShippingValidationChange = function (isValid) {
    console.log(`🔄 Shipping validation status changed: ${isValid ? 'Valid' : 'Invalid'}`);
    // This allows the main script to communicate validation status to the multi-step form
};

