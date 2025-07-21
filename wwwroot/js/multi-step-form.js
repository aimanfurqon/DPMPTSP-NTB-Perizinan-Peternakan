// Multi-Step Form JavaScript

// Global Variables
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
    1: ['CompanyName', 'CompanyAddress'],
    2: ['OriginLocation', 'DestinationLocation', 'DeparturePort', 'ArrivalPort'],
    3: validateLivestockStep,
    4: validateDocumentStep
};

$(document).ready(function () {
    initializeForm();
    initializeFileUpload();
    initializeEventHandlers();
    updateFormState();
});

function initializeForm() {
    showStep(1);
    updateLivestockNumbers();
    updateLivestockSummary();
    updateUploadProgress();
    updateDocumentChecklist();
    updateNavigationButtons();
    updateStepProgress();
}

function initializeFileUpload() {
    $('.file-input').each(function () {
        const $input = $(this);
        const documentType = $input.attr('id');
        const $uploadArea = $input.closest('.upload-area');

        $input.on('change', function () {
            if (this.files && this.files[0]) {
                handleFileSelection(this, this.files[0]);
            }
        });

        $uploadArea.find('.upload-placeholder').on('click', function (e) {
            e.preventDefault();
            $input[0].click();
        });

        $uploadArea.find('.btn-remove').on('click', function (e) {
            e.preventDefault();
            removeFile($input);
        });

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

            const files = e.originalEvent.dataTransfer.files;
            if (files.length > 0) {
                $input[0].files = files;
                handleFileSelection($input[0], files[0]);
            }
        });
    });
}

function initializeEventHandlers() {
    $('#nextBtn').on('click', nextStep);
    $('#prevBtn').on('click', prevStep);

    $('#addLivestock').on('click', addLivestockItem);
    $(document).on('click', '.remove-livestock', function (e) {
        e.preventDefault();
        $(this).closest('.livestock-item').remove();
        updateLivestockNumbers();
        updateLivestockSummary();
    });

    $(document).on('input', '.livestock-quantity, .livestock-type', updateLivestockSummary);

    $('#permitForm').on('submit', function (e) {
        if (!validateCurrentStep()) {
            e.preventDefault();
            showAlert('Mohon lengkapi semua field yang diperlukan', 'danger');
            return false;
        }

        if (!validateAllSteps()) {
            e.preventDefault();
            showAlert('Mohon lengkapi semua langkah sebelum submit', 'danger');
            return false;
        }

        $('#submitBtn').html('<i class="fas fa-spinner fa-spin"></i> Menyimpan...').prop('disabled', true);
        showAlert('Sedang memproses permohonan...', 'info');
    });

    $('input, textarea, select').on('blur', function () {
        validateField($(this));
    });
}

function nextStep() {
    if (validateCurrentStep()) {
        if (currentStep < totalSteps) {
            showStep(currentStep + 1);
        }
    } else {
        showAlert('Mohon lengkapi semua field yang diperlukan sebelum melanjutkan', 'warning');
    }
}

function prevStep() {
    if (currentStep > 1) {
        showStep(currentStep - 1);
    }
}

function showStep(step) {
    if (step < 1 || step > totalSteps) return;

    const direction = step > currentStep ? 'next' : 'prev';
    currentStep = step;
    $('#currentStepNumber').text(step);

    $('.form-step').removeClass('active prev');

    const $targetStep = $(`.form-step[data-step="${step}"]`);
    $targetStep.addClass(`active ${direction === 'prev' ? 'prev' : ''}`);

    updateStepProgress();
    updateNavigationButtons();
    
    $('.create-container').animate({ scrollTop: 0 }, 300);
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

function validateCurrentStep() {
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
        if (!$field.val()?.trim()) {
            $field.addClass('is-invalid');
            isValid = false;
        } else {
            $field.removeClass('is-invalid');
        }
    });

    return isValid;
}

function validateLivestockStep() {
    let hasValidLivestock = false;

    $('.livestock-item').each(function () {
        const type = $(this).find('.livestock-type').val();
        const quantity = parseInt($(this).find('.livestock-quantity').val()) || 0;

        if (type && quantity > 0) {
            hasValidLivestock = true;
        }
    });

    if (!hasValidLivestock) {
        showAlert('Minimal harus ada satu jenis ternak dengan jumlah yang valid', 'warning');
        return false;
    }

    return true;
}

function validateDocumentStep() {
    const uploadedCount = uploadedDocuments.size;

    if (uploadedCount < totalRequiredFiles) {
        showAlert(`Dokumen belum lengkap. ${uploadedCount}/${totalRequiredFiles} dokumen telah diupload`, 'warning');
        return false;
    }

    return true;
}

function validateAllSteps() {
    for (let step = 1; step <= totalSteps; step++) {
        const rules = stepValidationRules[step];

        if (typeof rules === 'function') {
            if (!rules()) return false;
        } else if (Array.isArray(rules)) {
            if (!validateRequiredFields(rules)) return false;
        }
    }

    return true;
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

function handleFileSelection(inputElement, file) {
    const $input = $(inputElement);
    const documentType = $input.attr('id');
    const maxSize = 5242880;
    const allowedTypes = ['application/pdf', 'image/jpeg', 'image/jpg', 'image/png'];

    if (file.size > maxSize) {
        showAlert('File terlalu besar. Maksimal ukuran file adalah 5MB', 'danger');
        $input.val('');
        return;
    }

    if (!allowedTypes.includes(file.type)) {
        showAlert('Format file tidak didukung. Gunakan PDF, JPG, JPEG, atau PNG', 'danger');
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

    showAlert(`File "${file.name}" berhasil dipilih`, 'success');
}

function updateFilePreview($input, file) {
    const $uploadArea = $input.closest('.upload-area');
    const $placeholder = $uploadArea.find('.upload-placeholder');
    const $preview = $uploadArea.find('.file-preview');

    $preview.find('.file-name').text(file.name);
    $preview.find('.file-size').text(formatFileSize(file.size));

    const documentType = $input.attr('id');
    const $status = $uploadArea.closest('.upload-item').find('.upload-status');
    $status.html('<i class="fas fa-circle text-success"></i> Sudah Upload');

    $placeholder.hide();
    $preview.show();
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

    showAlert('File berhasil dihapus', 'info');
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

function addLivestockItem() {
    const template = `
        <div class="livestock-item" data-index="${livestockIndex}">
            <div class="livestock-header">
                <h4>Ternak #<span class="livestock-number">${livestockIndex + 1}</span></h4>
                <button type="button" class="btn btn-sm btn-outline-danger remove-livestock">
                    <i class="fas fa-trash"></i>
                </button>
            </div>
            <div class="livestock-form">
                <div class="form-group">
                    <label class="form-label required">
                        <i class="fas fa-paw"></i>
                        Jenis Ternak
                    </label>
                    <select name="LivestockDetails[${livestockIndex}].LivestockType" class="form-control livestock-type" required>
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
                    </label>
                    <input type="number" name="LivestockDetails[${livestockIndex}].Quantity"
                           class="form-control livestock-quantity" min="1" max="10000"
                           placeholder="Masukkan jumlah ternak" required />
                </div>
                <div class="form-group">
                    <label class="form-label">
                        <i class="fas fa-comment"></i>
                        Keterangan
                    </label>
                    <textarea name="LivestockDetails[${livestockIndex}].Description"
                              class="form-control" rows="2"
                              placeholder="Keterangan tambahan (opsional)"></textarea>
                </div>
            </div>
        </div>
    `;

    $('#livestockContainer').append(template);
    livestockIndex++;
    updateLivestockNumbers();
    updateLivestockSummary();

    $('.livestock-item').last().addClass('fade-in');
}

function updateLivestockNumbers() {
    $('.livestock-item').each(function (index) {
        $(this).find('.livestock-number').text(index + 1);
    });
}

function updateLivestockSummary() {
    const totalTypes = $('.livestock-item').length;
    let totalQuantity = 0;

    $('.livestock-quantity').each(function () {
        const quantity = parseInt($(this).val()) || 0;
        totalQuantity += quantity;
    });

    $('#totalTypes').text(totalTypes);
    $('#totalQuantity').text(totalQuantity.toLocaleString());
}

function formatFileSize(bytes) {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
}

function showAlert(message, type) {
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

function updateFormState() {
}

function debugFormState() {
}

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

    if (e.key === 'Escape' && e.shiftKey) {
        debugFormState();
    }
});

function autoSave() {
    const formData = {
        step: currentStep,
        timestamp: new Date().toISOString(),
        data: $('#permitForm').serializeArray()
    };

    localStorage.setItem('permitFormDraft', JSON.stringify(formData));
}

function loadDraft() {
    const saved = localStorage.getItem('permitFormDraft');
    if (saved) {
        try {
            const data = JSON.parse(saved);
        } catch (e) {
        }
    }
}

setInterval(autoSave, 30000);

window.MultiStepForm = {
    nextStep,
    prevStep,
    showStep,
    validateCurrentStep,
    validateAllSteps,
    debugFormState,
    currentStep: () => currentStep,
    totalSteps: () => totalSteps
};

// Added New

// ADD to existing multi-step-form.js file

// Enhanced Document Details Validation
function initializeDocumentDetailsValidation() {
    console.log('🔧 Initializing document details validation...');

    const documentsWithDetails = ['SuratPermohonan', 'RekomendasiDinasProv', 'RekomendasiDaerahTujuan'];

    documentsWithDetails.forEach(docType => {
        const fileInput = document.getElementById(docType);
        const dateInput = document.getElementById(docType + 'Tanggal');
        const numberInput = document.getElementById(docType + 'Nomor');

        if (fileInput && dateInput && numberInput) {
            // Validate when file is selected
            fileInput.addEventListener('change', function () {
                if (this.files && this.files.length > 0) {
                    validateDocumentDetails(docType);
                } else {
                    clearDocumentDetailsValidation(docType);
                }
            });

            // Validate when date changes
            dateInput.addEventListener('change', function () {
                validateDocumentDate(docType, this.value);
            });

            // Validate when number changes
            numberInput.addEventListener('input', function () {
                validateDocumentNumber(docType, this.value);
            });

            // Real-time validation
            dateInput.addEventListener('blur', function () {
                validateDocumentDate(docType, this.value);
            });

            numberInput.addEventListener('blur', function () {
                validateDocumentNumber(docType, this.value);
            });
        }
    });
}

function validateDocumentDetails(documentType) {
    console.log(`📋 Validating document details for: ${documentType}`);

    const fileInput = document.getElementById(documentType);
    const dateInput = document.getElementById(documentType + 'Tanggal');
    const numberInput = document.getElementById(documentType + 'Nomor');

    let isValid = true;

    // Check if file is uploaded
    if (fileInput && fileInput.files && fileInput.files.length > 0) {
        // Validate date
        if (!dateInput.value) {
            showDocumentFieldError(dateInput, 'Tanggal pengajuan harus diisi');
            isValid = false;
        } else {
            clearDocumentFieldError(dateInput);
            // Validate date is not in future
            const inputDate = new Date(dateInput.value);
            const today = new Date();
            today.setHours(0, 0, 0, 0);

            if (inputDate > today) {
                showDocumentFieldError(dateInput, 'Tanggal tidak boleh di masa depan');
                isValid = false;
            }
        }

        // Validate number
        if (!numberInput.value || numberInput.value.trim() === '') {
            showDocumentFieldError(numberInput, 'Nomor dokumen harus diisi');
            isValid = false;
        } else {
            clearDocumentFieldError(numberInput);
            // Validate format (basic validation)
            const numberPattern = /^[a-zA-Z0-9\/\-\.\_\s]+$/;
            if (!numberPattern.test(numberInput.value)) {
                showDocumentFieldError(numberInput, 'Format nomor dokumen tidak valid');
                isValid = false;
            }
        }
    }

    return isValid;
}

function validateDocumentDate(documentType, dateValue) {
    const dateInput = document.getElementById(documentType + 'Tanggal');

    if (!dateValue) {
        // Only show error if file is uploaded
        const fileInput = document.getElementById(documentType);
        if (fileInput && fileInput.files && fileInput.files.length > 0) {
            showDocumentFieldError(dateInput, 'Tanggal pengajuan harus diisi');
            return false;
        }
    } else {
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

    if (!numberValue || numberValue.trim() === '') {
        // Only show error if file is uploaded
        const fileInput = document.getElementById(documentType);
        if (fileInput && fileInput.files && fileInput.files.length > 0) {
            showDocumentFieldError(numberInput, 'Nomor dokumen harus diisi');
            return false;
        }
    } else {
        // Basic format validation
        const numberPattern = /^[a-zA-Z0-9\/\-\.\_\s]+$/;
        if (!numberPattern.test(numberValue)) {
            showDocumentFieldError(numberInput, 'Format nomor dokumen tidak valid');
            return false;
        } else {
            clearDocumentFieldError(numberInput);
        }
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

// Enhanced step validation for step 4 (documents)
function validateDocumentStep() {
    console.log('📋 Validating document step...');

    let isValid = true;
    const requiredDocuments = [
        'SuratPermohonan',
        'RekomendasiDinasProv',
        'RekomendasiDaerahTujuan',
        'SKKHKabupatenAsal',
        'SKKHDinasProvinsi',
        'SuratJalanTernak',
        'HasilPemeriksaanFisik'
    ];

    // Check if all required documents are uploaded
    for (const docType of requiredDocuments) {
        const fileInput = document.getElementById(docType);
        if (!fileInput || !fileInput.files || fileInput.files.length === 0) {
            console.log(`❌ Missing document: ${docType}`);
            isValid = false;
        } else {
            // For documents with details, validate the details
            const documentsWithDetails = ['SuratPermohonan', 'RekomendasiDinasProv', 'RekomendasiDaerahTujuan'];
            if (documentsWithDetails.includes(docType)) {
                if (!validateDocumentDetails(docType)) {
                    isValid = false;
                }
            }
        }
    }

    if (!isValid) {
        showValidationMessage('Semua dokumen wajib harus diupload dengan informasi yang lengkap');
    }

    return isValid;
}

// Enhanced form submission validation
function validateFormBeforeSubmit() {
    console.log('🔍 Final form validation before submit...');

    let isValid = true;
    const errors = [];

    // Validate basic company info
    const companyName = document.querySelector('[name="CompanyName"]');
    if (!companyName || !companyName.value.trim()) {
        errors.push('Nama perusahaan harus diisi');
        isValid = false;
    }

    const companyAddress = document.querySelector('[name="CompanyAddress"]');
    if (!companyAddress || !companyAddress.value.trim()) {
        errors.push('Alamat perusahaan harus diisi');
        isValid = false;
    }

    // Validate livestock details
    const livestockTypes = document.querySelectorAll('.livestock-type');
    const livestockQuantities = document.querySelectorAll('.livestock-quantity');

    if (livestockTypes.length === 0) {
        errors.push('Minimal harus ada 1 detail ternak');
        isValid = false;
    } else {
        for (let i = 0; i < livestockTypes.length; i++) {
            if (!livestockTypes[i].value.trim()) {
                errors.push(`Jenis ternak #${i + 1} harus diisi`);
                isValid = false;
            }
            if (!livestockQuantities[i] || parseInt(livestockQuantities[i].value) <= 0) {
                errors.push(`Jumlah ternak #${i + 1} harus lebih dari 0`);
                isValid = false;
            }
        }
    }

    // Validate documents
    if (!validateDocumentStep()) {
        isValid = false;
    }

    // Show errors if any
    if (errors.length > 0) {
        const errorMessage = errors.join('\\n');
        alert('Terdapat kesalahan pada form:\\n\\n' + errorMessage);
    }

    return isValid;
}

// UPDATE existing initialization function
$(document).ready(function () {
    console.log('🚀 Initializing Enhanced Multi-Step Form...');

    initializeForm();
    initializeFileUpload();
    initializeEventHandlers();
    initializeDocumentDetailsValidation(); // NEW
    updateFormState();

    console.log('✅ Enhanced Multi-Step Form initialized successfully');
});

// UPDATE form submission handler
$('#multiStepForm').on('submit', function (e) {
    console.log('📤 Form submission attempt...');

    if (!validateFormBeforeSubmit()) {
        e.preventDefault();
        console.log('❌ Form validation failed, preventing submission');
        return false;
    }

    console.log('✅ Form validation passed, proceeding with submission');

    // Show loading state
    const submitBtn = $('#submitBtn');
    const originalText = submitBtn.html();
    submitBtn.html('<i class="fas fa-spinner fa-spin"></i> Mengirim...');
    submitBtn.prop('disabled', true);

    // Optional: Reset button after timeout (in case of server error)
    setTimeout(() => {
        submitBtn.html(originalText);
        submitBtn.prop('disabled', false);
    }, 30000); // 30 seconds timeout
});

// Enhanced file upload handler with document details validation
function initializeFileUpload() {
    console.log('🔧 Initializing enhanced file upload system...');

    $('.file-input').each(function () {
        const $input = $(this);
        const documentType = $input.attr('id');
        const $uploadArea = $input.closest('.upload-area');
        const $uploadItem = $input.closest('.upload-item');

        // File selection handler
        $input.on('change', function (e) {
            const file = e.target.files[0];
            const maxSize = parseInt($input.data('max-size')) || 5242880; // 5MB default

            if (file) {
                console.log(`📄 File selected for ${documentType}:`, file.name);

                // Validate file
                if (!validateFile(file, maxSize)) {
                    $input.val('');
                    return;
                }

                // Show file preview
                displayFilePreview($uploadArea, file);

                // Update upload status
                updateUploadStatus($uploadItem, true);

                // Update progress
                updateUploadProgress();
                updateDocumentChecklist();

                // Validate document details for specific documents
                const documentsWithDetails = ['SuratPermohonan', 'RekomendasiDinasProv', 'RekomendasiDaerahTujuan'];
                if (documentsWithDetails.includes(documentType)) {
                    setTimeout(() => {
                        validateDocumentDetails(documentType);
                    }, 100);
                }

                console.log(`✅ File ${file.name} uploaded successfully for ${documentType}`);
            }
        });

        // Remove file handler
        $uploadArea.on('click', '.btn-remove', function () {
            console.log(`🗑️ Removing file for ${documentType}`);

            $input.val('');
            hideFilePreview($uploadArea);
            updateUploadStatus($uploadItem, false);
            updateUploadProgress();
            updateDocumentChecklist();

            // Clear document details validation
            clearDocumentDetailsValidation(documentType);

            console.log(`✅ File removed for ${documentType}`);
        });

        // Drag and drop handlers
        $uploadArea.on('dragover', function (e) {
            e.preventDefault();
            $(this).addClass('drag-over');
        });

        $uploadArea.on('dragleave', function (e) {
            e.preventDefault();
            $(this).removeClass('drag-over');
        });

        $uploadArea.on('drop', function (e) {
            e.preventDefault();
            $(this).removeClass('drag-over');

            const files = e.originalEvent.dataTransfer.files;
            if (files.length > 0) {
                $input[0].files = files;
                $input.trigger('change');
            }
        });

        // Click to upload
        $uploadArea.on('click', '.upload-placeholder', function () {
            $input.click();
        });
    });
}

// Enhanced file validation
function validateFile(file, maxSize) {
    const allowedTypes = ['application/pdf', 'image/jpeg', 'image/jpg', 'image/png'];
    const allowedExtensions = ['.pdf', '.jpg', '.jpeg', '.png'];

    // Check file size
    if (file.size > maxSize) {
        const maxSizeMB = (maxSize / 1024 / 1024).toFixed(1);
        alert(`Ukuran file terlalu besar. Maksimal ${maxSizeMB}MB`);
        return false;
    }

    // Check file type
    const fileExtension = '.' + file.name.split('.').pop().toLowerCase();
    if (!allowedTypes.includes(file.type) && !allowedExtensions.includes(fileExtension)) {
        alert('Format file tidak didukung. Gunakan PDF, JPG, JPEG, atau PNG');
        return false;
    }

    return true;
}

// Enhanced file preview display
function displayFilePreview($uploadArea, file) {
    const $placeholder = $uploadArea.find('.upload-placeholder');
    const $preview = $uploadArea.find('.file-preview');

    // Hide placeholder and show preview
    $placeholder.hide();
    $preview.show();

    // Update file info
    $preview.find('.file-name').text(file.name);
    $preview.find('.file-size').text(formatFileSize(file.size));

    // Add file type icon
    const fileIcon = getFileIcon(file.type, file.name);
    $preview.find('.file-info i').attr('class', fileIcon);
}

function hideFilePreview($uploadArea) {
    const $placeholder = $uploadArea.find('.upload-placeholder');
    const $preview = $uploadArea.find('.file-preview');

    $preview.hide();
    $placeholder.show();
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

function formatFileSize(bytes) {
    const sizes = ['B', 'KB', 'MB', 'GB'];
    if (bytes === 0) return '0 B';
    const i = Math.floor(Math.log(bytes) / Math.log(1024));
    return Math.round(bytes / Math.pow(1024, i) * 100) / 100 + ' ' + sizes[i];
}

// Enhanced upload progress tracking
function updateUploadProgress() {
    const totalFiles = 7;
    const uploadedFiles = $('.upload-item').filter(function () {
        const $input = $(this).find('.file-input');
        return $input[0] && $input[0].files && $input[0].files.length > 0;
    }).length;

    const progressPercentage = Math.round((uploadedFiles / totalFiles) * 100);

    $('#uploadProgress').css('width', progressPercentage + '%');
    $('#uploadProgressText').text(`${uploadedFiles} dari ${totalFiles} dokumen telah diupload`);

    console.log(`📊 Upload progress: ${uploadedFiles}/${totalFiles} (${progressPercentage}%)`);
}

function updateDocumentChecklist() {
    $('.checklist-item').each(function () {
        const docType = $(this).data('doc');
        const $input = $(`#${docType}`);
        const hasFile = $input[0] && $input[0].files && $input[0].files.length > 0;

        if (hasFile) {
            $(this).addClass('completed');
            $(this).find('i').attr('class', 'fas fa-check-circle text-success');
        } else {
            $(this).removeClass('completed');
            $(this).find('i').attr('class', 'fas fa-times-circle text-danger');
        }
    });
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

// Enhanced validation message display
function showValidationMessage(message, type = 'error') {
    // Remove existing messages
    $('.validation-message').remove();

    const alertClass = type === 'error' ? 'alert-danger' : 'alert-info';
    const iconClass = type === 'error' ? 'fa-exclamation-triangle' : 'fa-info-circle';

    const messageHtml = `
        <div class="alert ${alertClass} validation-message" role="alert">
            <i class="fas ${iconClass}"></i>
            <span>${message}</span>
        </div>
    `;

    $('.form-step.active .step-content').prepend(messageHtml);

    // Auto-remove after 10 seconds
    setTimeout(() => {
        $('.validation-message').fadeOut(500, function () {
            $(this).remove();
        });
    }, 10000);
}

// Add CSS for enhanced styling
const additionalCSS = `
<style>
.drag-over {
    border-color: #007bff !important;
    background-color: #f8f9ff !important;
}

.upload-item.completed {
    border-color: #28a745;
    background-color: #f8fff9;
}

.file-input.is-invalid {
    border-color: #dc3545;
}

.document-field-error {
    color: #dc3545;
    font-size: 0.75rem;
    margin-top: 0.25rem;
    display: block;
}

.validation-message {
    margin-bottom: 1rem;
    border-radius: 8px;
    border: none;
    box-shadow: 0 2px 4px rgba(0,0,0,0.1);
}

.validation-message i {
    margin-right: 0.5rem;
}

.upload-status .fas.fa-check-circle {
    color: #28a745;
}

.upload-status .fas.fa-circle {
    color: #dc3545;
}

.checklist-item.completed {
    background-color: #d4edda;
    color: #155724;
}

.checklist-item.completed i {
    color: #28a745;
}

@keyframes pulse {
    0% { transform: scale(1); }
    50% { transform: scale(1.05); }
    100% { transform: scale(1); }
}

.upload-item:hover {
    animation: pulse 0.3s ease-in-out;
}
</style>
`;

// Inject additional CSS
if (!document.getElementById('enhanced-upload-styles')) {
    const styleSheet = document.createElement('style');
    styleSheet.id = 'enhanced-upload-styles';
    styleSheet.innerHTML = additionalCSS.replace(/<\/?style>/g, '');
    document.head.appendChild(styleSheet);
}