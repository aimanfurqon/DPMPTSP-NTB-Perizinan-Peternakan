// Multi-Step Form JavaScript - Synchronized Version
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
    'HasilPemeriksaanFisik'
];

const stepValidationRules = {
    1: validateCompanyStep,  // Custom validation for step 1
    2: ['OriginLocation', 'DestinationLocation', 'DeparturePort', 'ArrivalPort'],
    3: validateLivestockStep,
    4: validateDocumentStep
};

// ===============================================
// INITIALIZATION
// ===============================================
$(document).ready(function () {
    console.log('🚀 Initializing Multi-Step Form Navigation...');

    initializeForm();
    initializeFileUpload(); // Add this back
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
            console.log(`📁 Drag over ${documentType}`);
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
        showAlert('Mohon lengkapi semua field yang diperlukan sebelum melanjutkan', 'warning');
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
// VALIDATION FUNCTIONS
// ===============================================
function validateCurrentStep() {
    console.log(`🔍 Validating step ${currentStep}`);

    const rules = stepValidationRules[currentStep];

    if (typeof rules === 'function') {
        return rules();
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

function validateLivestockStep() {
    console.log('🐄 Validating livestock step...');

    let hasValidLivestock = false;

    $('.livestock-item').each(function () {
        const type = $(this).find('.livestock-type').val();
        const quantity = parseInt($(this).find('.livestock-quantity').val()) || 0;

        if (type && quantity > 0) {
            hasValidLivestock = true;
        }
    });

    // Also check for quota validation errors (integration with main script)
    const hasQuotaErrors = $('.livestock-quantity.error').length > 0;

    if (!hasValidLivestock) {
        showAlert('Minimal harus ada satu jenis ternak dengan jumlah yang valid', 'warning');
        return false;
    }

    if (hasQuotaErrors) {
        showAlert('Masih ada masalah dengan kuota ternak. Silakan periksa kembali jumlah yang diminta.', 'warning');
        return false;
    }

    return true;
}

function validateCompanyStep() {
    console.log('🏢 Validating company step...');

    let isValid = true;
    const errors = [];

    // Validate company name
    const companyName = $('[name="CompanyName"]').val()?.trim();
    if (!companyName) {
        $('[name="CompanyName"]').addClass('is-invalid');
        errors.push('Nama perusahaan harus diisi');
        isValid = false;
    } else {
        $('[name="CompanyName"]').removeClass('is-invalid');
    }

    // Validate address components (since we use dropdowns)
    const province = $('[name="CompanyProvince"]').val()?.trim();
    const regency = $('[name="CompanyRegency"]').val()?.trim();
    const district = $('[name="AddressDistrict"]').val()?.trim();
    const village = $('[name="AddressVillage"]').val()?.trim();

    if (!province) {
        $('#companyProvinceSelect').addClass('is-invalid');
        errors.push('Provinsi perusahaan harus dipilih');
        isValid = false;
    } else {
        $('#companyProvinceSelect').removeClass('is-invalid');
    }

    if (!regency) {
        $('#companyRegencySelect').addClass('is-invalid');
        errors.push('Kabupaten/Kota perusahaan harus dipilih');
        isValid = false;
    } else {
        $('#companyRegencySelect').removeClass('is-invalid');
    }

    if (!district) {
        $('#companyDistrictSelect').addClass('is-invalid');
        errors.push('Kecamatan harus dipilih');
        isValid = false;
    } else {
        $('#companyDistrictSelect').removeClass('is-invalid');
    }

    if (!village) {
        $('#companyVillageSelect').addClass('is-invalid');
        errors.push('Desa/Kelurahan harus dipilih');
        isValid = false;
    } else {
        $('#companyVillageSelect').removeClass('is-invalid');
    }

    if (errors.length > 0) {
        showAlert('Mohon lengkapi: ' + errors.join(', '), 'warning');
    }

    return isValid;
}

function validateDocumentStep() {
    console.log('📄 Validating document step...');

    const uploadedCount = uploadedDocuments.size;
    const documentsWithDetails = ['SuratPermohonan', 'RekomendasiDinasProv', 'RekomendasiDaerahTujuan'];

    // Check if all documents are uploaded
    if (uploadedCount < totalRequiredFiles) {
        showAlert(`Dokumen belum lengkap. ${uploadedCount}/${totalRequiredFiles} dokumen telah diupload`, 'warning');
        return false;
    }

    // Check document details for specific documents
    let detailsValid = true;
    documentsWithDetails.forEach(docType => {
        if (!validateDocumentDetails(docType)) {
            detailsValid = false;
        }
    });

    if (!detailsValid) {
        showAlert('Mohon lengkapi tanggal dan nomor dokumen yang diperlukan', 'warning');
        return false;
    }

    return true;
}

function validateAllSteps() {
    console.log('🔍 Validating all steps before submission...');

    for (let step = 1; step <= totalSteps; step++) {
        const rules = stepValidationRules[step];

        if (typeof rules === 'function') {
            if (!rules()) {
                console.log(`❌ Step ${step} validation failed`);
                return false;
            }
        } else if (Array.isArray(rules)) {
            if (!validateRequiredFields(rules)) {
                console.log(`❌ Step ${step} required fields validation failed`);
                return false;
            }
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

    console.log(`📄 Processing file for ${documentType}: ${file.name} (${formatFileSize(file.size)})`);

    // Validate file
    if (!validateFile(file, maxSize)) {
        console.log(`❌ File validation failed for ${documentType}`);
        $input.val('');
        return;
    }

    // Update file preview
    updateFilePreview($input, file);

    // Store in uploaded documents map
    uploadedDocuments.set(documentType, {
        file: file,
        name: file.name,
        size: file.size
    });

    // Update UI
    updateUploadProgress();
    updateDocumentChecklist();

    // Update upload status
    const $uploadItem = $input.closest('.upload-item');
    updateUploadStatus($uploadItem, true);

    // Validate document details for specific documents
    const documentsWithDetails = ['SuratPermohonan', 'RekomendasiDinasProv', 'RekomendasiDaerahTujuan'];
    if (documentsWithDetails.includes(documentType)) {
        setTimeout(() => {
            validateDocumentDetails(documentType);
        }, 100);
    }

    showAlert(`File "${file.name}" berhasil dipilih`, 'success');
    console.log(`✅ File ${file.name} processed successfully for ${documentType}`);
}

function removeFile($input) {
    const documentType = $input.attr('id');
    const $uploadArea = $input.closest('.upload-area');

    console.log(`🗑️ Removing file for ${documentType}`);

    $input.val('');

    $uploadArea.find('.file-preview').hide();
    $uploadArea.find('.upload-placeholder').show();

    const $status = $uploadArea.closest('.upload-item').find('.upload-status');
    $status.html('<i class="fas fa-circle text-danger"></i> Belum Upload');

    uploadedDocuments.delete(documentType);

    updateUploadProgress();
    updateDocumentChecklist();

    // Clear document details validation
    clearDocumentDetailsValidation(documentType);

    showAlert('File berhasil dihapus', 'info');
}

function validateFile(file, maxSize) {
    const allowedTypes = ['application/pdf', 'image/jpeg', 'image/jpg', 'image/png'];
    const allowedExtensions = ['.pdf', '.jpg', '.jpeg', '.png'];

    // Check file size
    if (file.size > maxSize) {
        const maxSizeMB = (maxSize / 1024 / 1024).toFixed(1);
        showAlert(`Ukuran file terlalu besar. Maksimal ${maxSizeMB}MB`, 'danger');
        return false;
    }

    // Check file type
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

    console.log(`🖼️ Updating preview for: ${file.name}`);

    // Update file info
    $preview.find('.file-name').text(file.name);
    $preview.find('.file-size').text(formatFileSize(file.size));

    // Update file icon
    const fileIcon = getFileIcon(file.type, file.name);
    $preview.find('.file-info i').removeClass().addClass(fileIcon);

    // Update upload status
    const $status = $uploadArea.closest('.upload-item').find('.upload-status');
    $status.html('<i class="fas fa-check-circle text-success"></i> Sudah Upload');

    // Show preview, hide placeholder
    $placeholder.hide();
    $preview.show();

    console.log(`✅ Preview updated for: ${file.name}`);
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
            $icon.removeClass('fas fa-times-circle text-danger')
                .addClass('fas fa-check-circle text-success');
            $item.addClass('completed');
        } else {
            $icon.removeClass('fas fa-check-circle text-success')
                .addClass('fas fa-times-circle text-danger');
            $item.removeClass('completed');
        }
    });
}

// ===============================================
// DOCUMENT DETAILS VALIDATION
// ===============================================
function initializeDocumentDetailsValidation() {
    console.log('🔧 Initializing document details validation...');

    const documentsWithDetails = ['SuratPermohonan', 'RekomendasiDinasProv', 'RekomendasiDaerahTujuan'];

    documentsWithDetails.forEach(docType => {
        const fileInput = document.getElementById(docType);
        const dateInput = document.getElementById(docType + 'Tanggal');
        const numberInput = document.getElementById(docType + 'Nomor');

        if (fileInput && dateInput && numberInput) {
            // File change handler
            fileInput.addEventListener('change', function () {
                if (this.files && this.files.length > 0) {
                    validateDocumentDetails(docType);
                } else {
                    clearDocumentDetailsValidation(docType);
                }
            });

            // Date and number validation
            dateInput.addEventListener('change', function () {
                validateDocumentDate(docType, this.value);
            });

            numberInput.addEventListener('input', function () {
                validateDocumentNumber(docType, this.value);
            });
        }
    });
}

function validateDocumentDetails(documentType) {
    const dateInput = document.getElementById(documentType + 'Tanggal');
    const numberInput = document.getElementById(documentType + 'Nomor');
    const fileInput = document.getElementById(documentType);

    let isValid = true;

    // Only validate if file is uploaded
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

    // Form submission
    $('#permitForm').on('submit', function (e) {
        console.log('📤 Form submission attempt...');

        if (!validateAllSteps()) {
            e.preventDefault();
            showAlert('Mohon lengkapi semua langkah sebelum submit', 'danger');
            return false;
        }

        // Show loading state
        $('#submitBtn').html('<i class="fas fa-spinner fa-spin"></i> Menyimpan...').prop('disabled', true);
        showAlert('Sedang memproses permohonan...', 'info');
    });

    // Field validation on blur
    $('input, textarea, select').on('blur', function () {
        validateField($(this));
    });

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
}

function validateField($field) {
    const value = $field.val()?.trim();
    const isRequired = $field.attr('required') !== undefined;

    if (isRequired && !value) {
        $field.addClass('is-invalid');
        return false;
    } else {
        $field.removeClass('is-invalid');
        return true;
    }
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
    console.log(`🔔 Alert (${type}): ${message}`);

    $('.custom-alert').remove();

    const alertClass = {
        'success': 'alert-success',
        'danger': 'alert-danger',
        'warning': 'alert-warning',
        'info': 'alert-info'
    }[type] || 'alert-info';

    const icon = {
        'success': 'fa-check-circle',
        'danger': 'fa-exclamation-circle',
        'warning': 'fa-exclamation-triangle',
        'info': 'fa-info-circle'
    }[type] || 'fa-info-circle';

    const alert = $(`
        <div class="alert ${alertClass} alert-dismissible fade show custom-alert"
             style="position: fixed; top: 20px; right: 20px; z-index: 9999; max-width: 400px; 
                    box-shadow: 0 4px 15px rgba(0,0,0,0.2);">
            <div style="display: flex; align-items: center; gap: 10px;">
                <i class="fas ${icon}"></i>
                <span>${message}</span>
            </div>
            <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close">
                <span aria-hidden="true">&times;</span>
            </button>
        </div>
    `);

    $('body').append(alert);

    const delay = type === 'danger' ? 6000 : 4000;
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
    uploadedDocuments: () => uploadedDocuments
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

console.log('✅ Multi-Step Form module loaded successfully');