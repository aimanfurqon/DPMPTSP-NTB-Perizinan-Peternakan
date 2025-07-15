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

// Step validation rules
const stepValidationRules = {
    1: ['CompanyName', 'CompanyAddress'],
    2: ['OriginLocation', 'DestinationLocation', 'DeparturePort', 'ArrivalPort'],
    3: validateLivestockStep,
    4: validateDocumentStep
};

// Initialize when document is ready
$(document).ready(function () {
    console.log('🚀 Initializing Multi-Step Form...');

    initializeForm();
    initializeFileUpload();
    initializeEventHandlers();
    updateFormState();

    console.log('✅ Multi-Step Form initialized successfully');
});

// Initialize Form
function initializeForm() {
    // Show first step
    showStep(1);

    // Update livestock numbers and summary
    updateLivestockNumbers();
    updateLivestockSummary();
    updateUploadProgress();
    updateDocumentChecklist();

    // Set initial form state
    updateNavigationButtons();
    updateStepProgress();
}

// Initialize File Upload System
function initializeFileUpload() {
    console.log('🔧 Initializing file upload system...');

    $('.file-input').each(function () {
        const $input = $(this);
        const documentType = $input.attr('id');
        const $uploadArea = $input.closest('.upload-area');

        // File input change event
        $input.on('change', function () {
            if (this.files && this.files[0]) {
                handleFileSelection(this, this.files[0]);
            }
        });

        // Upload area click handler
        $uploadArea.find('.upload-placeholder').on('click', function (e) {
            e.preventDefault();
            $input[0].click();
        });

        // Remove file handler
        $uploadArea.find('.btn-remove').on('click', function (e) {
            e.preventDefault();
            removeFile($input);
        });

        // Drag and drop handlers
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

// Initialize Event Handlers
function initializeEventHandlers() {
    // Navigation button handlers
    $('#nextBtn').on('click', nextStep);
    $('#prevBtn').on('click', prevStep);

    // Livestock handlers
    $('#addLivestock').on('click', addLivestockItem);
    $(document).on('click', '.remove-livestock', function (e) {
        e.preventDefault();
        $(this).closest('.livestock-item').remove();
        updateLivestockNumbers();
        updateLivestockSummary();
    });

    $(document).on('input', '.livestock-quantity, .livestock-type', updateLivestockSummary);

    // Form submission
    $('#permitForm').on('submit', function (e) {
        console.log('📤 Form submission started...');

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

    // Real-time validation
    $('input, textarea, select').on('blur', function () {
        validateField($(this));
    });
}

// Step Navigation Functions
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


    // Hide all steps
    $('.form-step').removeClass('active prev');

    // Show target step with animation
    const $targetStep = $(`.form-step[data-step="${step}"]`);
    $targetStep.addClass(`active ${direction === 'prev' ? 'prev' : ''}`);

    // Update step indicators
    updateStepProgress();

    // Update navigation buttons
    updateNavigationButtons();

    // Update current step
    
    // Scroll to top
    $('.create-container').animate({ scrollTop: 0 }, 300);

    console.log(`📍 Moved to step ${step}`);
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

    // Show/hide previous button
    if (currentStep === 1) {
        $prevBtn.hide();
    } else {
        $prevBtn.show();
    }

    // Show/hide next and submit buttons
    if (currentStep === totalSteps) {
        $nextBtn.hide();
        $submitBtn.show();
    } else {
        $nextBtn.show();
        $submitBtn.hide();
    }
}

// Validation Functions
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

// File Upload Functions
function handleFileSelection(inputElement, file) {
    const $input = $(inputElement);
    const documentType = $input.attr('id');
    const maxSize = 5242880; // 5MB
    const allowedTypes = ['application/pdf', 'image/jpeg', 'image/jpg', 'image/png'];

    console.log(`📄 Processing file: ${file.name} for ${documentType}`);

    // Validate file size
    if (file.size > maxSize) {
        showAlert('File terlalu besar. Maksimal ukuran file adalah 5MB', 'danger');
        $input.val('');
        return;
    }

    // Validate file type
    if (!allowedTypes.includes(file.type)) {
        showAlert('Format file tidak didukung. Gunakan PDF, JPG, JPEG, atau PNG', 'danger');
        $input.val('');
        return;
    }

    // Update UI
    updateFilePreview($input, file);

    // Track uploaded file
    uploadedDocuments.set(documentType, {
        file: file,
        name: file.name,
        size: file.size
    });

    updateUploadProgress();
    updateDocumentChecklist();

    console.log(`✅ File processed successfully: ${documentType}`);
    showAlert(`File "${file.name}" berhasil dipilih`, 'success');
}

function updateFilePreview($input, file) {
    const $uploadArea = $input.closest('.upload-area');
    const $placeholder = $uploadArea.find('.upload-placeholder');
    const $preview = $uploadArea.find('.file-preview');

    // Update file information
    $preview.find('.file-name').text(file.name);
    $preview.find('.file-size').text(formatFileSize(file.size));

    // Update upload status
    const documentType = $input.attr('id');
    const $status = $uploadArea.closest('.upload-item').find('.upload-status');
    $status.html('<i class="fas fa-circle text-success"></i> Sudah Upload');

    // Show/hide elements
    $placeholder.hide();
    $preview.show();
}

function removeFile($input) {
    const documentType = $input.attr('id');
    const $uploadArea = $input.closest('.upload-area');

    // Clear input
    $input.val('');

    // Update UI
    $uploadArea.find('.file-preview').hide();
    $uploadArea.find('.upload-placeholder').show();

    // Update upload status
    const $status = $uploadArea.closest('.upload-item').find('.upload-status');
    $status.html('<i class="fas fa-circle text-danger"></i> Belum Upload');

    // Remove from tracking
    uploadedDocuments.delete(documentType);

    updateUploadProgress();
    updateDocumentChecklist();

    console.log(`🗑️ File removed: ${documentType}`);
    showAlert('File berhasil dihapus', 'info');
}

function updateUploadProgress() {
    const count = uploadedDocuments.size;
    const percentage = (count / totalRequiredFiles) * 100;

    $('#uploadProgress').css('width', percentage + '%');
    $('#uploadProgressText').text(`${count} dari ${totalRequiredFiles} dokumen telah diupload`);

    console.log(`📊 Upload progress: ${count}/${totalRequiredFiles} (${percentage.toFixed(1)}%)`);
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

// Livestock Functions
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

    // Add fade in animation
    $('.livestock-item').last().addClass('fade-in');

    console.log(`➕ Added livestock item #${livestockIndex}`);
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

// Utility Functions
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

    // Auto remove after delay
    const delay = type === 'danger' ? 6000 : 4000;
    setTimeout(() => {
        alert.fadeOut(300, () => alert.remove());
    }, delay);
}

function updateFormState() {
    // Update any global form state
    console.log('🔄 Form state updated');
}

function debugFormState() {
    console.log('🔍 === FORM DEBUG INFO ===');
    console.log(`Current Step: ${currentStep}/${totalSteps}`);
    console.log(`Livestock Items: ${$('.livestock-item').length}`);
    console.log(`Uploaded Documents: ${uploadedDocuments.size}/${totalRequiredFiles}`);
    console.log('Documents:', Array.from(uploadedDocuments.keys()));

    // Check file inputs
    $('.file-input').each(function () {
        const hasFile = this.files && this.files.length > 0;
        console.log(`${this.id}: ${hasFile ? this.files[0].name : 'NONE'}`);
    });
}

// Keyboard shortcuts
$(document).on('keydown', function (e) {
    // Ctrl + Arrow keys for navigation
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

    // ESC key to show debug info
    if (e.key === 'Escape' && e.shiftKey) {
        debugFormState();
    }
});

// Auto-save functionality (optional)
function autoSave() {
    const formData = {
        step: currentStep,
        timestamp: new Date().toISOString(),
        data: $('#permitForm').serializeArray()
    };

    localStorage.setItem('permitFormDraft', JSON.stringify(formData));
    console.log('💾 Form auto-saved');
}

// Load saved draft (optional)
function loadDraft() {
    const saved = localStorage.getItem('permitFormDraft');
    if (saved) {
        try {
            const data = JSON.parse(saved);
            console.log('📋 Draft found:', data.timestamp);
            // Implement draft loading logic if needed
        } catch (e) {
            console.warn('Failed to load draft:', e);
        }
    }
}

// Auto-save every 30 seconds
setInterval(autoSave, 30000);

// Export functions for external use
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