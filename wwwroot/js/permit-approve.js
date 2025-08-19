// Permit Approval Page JavaScript
// This file contains all JavaScript functionality for the permit approval page

class PermitApprovalManager {
    constructor() {
        this.selectedAction = '';
        this.currentPreviewPath = '';
        this.currentImagePath = '';
        this.userRole = document.body.getAttribute('data-user-role') || '';
        this.requiredDocsCount = parseInt(document.body.getAttribute('data-required-docs-count') || '0');
        this.permitId = document.body.getAttribute('data-permit-id') || '';
        
        this.init();
    }

    init() {
        this.bindEvents();
        this.initializeVerifikatorExperience();
    }

    bindEvents() {
        // Modal cleanup events
        const documentPreviewModal = document.getElementById('documentPreviewModal');
        const imagePreviewModal = document.getElementById('imagePreviewModal');
        const commentsTextarea = document.getElementById('Comments');

        if (documentPreviewModal) {
            documentPreviewModal.addEventListener('hidden.bs.modal', () => {
                const iframe = document.getElementById('previewFrame');
                if (iframe) {
                    iframe.src = 'about:blank';
                }
                this.currentPreviewPath = '';
            });
        }

        if (imagePreviewModal) {
            imagePreviewModal.addEventListener('hidden.bs.modal', () => {
                const previewImage = document.getElementById('previewImage');
                if (previewImage) {
                    previewImage.src = '';
                }
                this.currentImagePath = '';
            });
        }

        if (commentsTextarea) {
            commentsTextarea.addEventListener('input', function() {
                this.style.height = 'auto';
                this.style.height = (this.scrollHeight) + 'px';
            });
        }

        // Initialize when DOM is loaded
        document.addEventListener('DOMContentLoaded', () => {
            this.enhanceVerifikatorExperience();
            this.updateVerifikatorProgress();
            this.initializeVerifikatorPdfContentView();
        });
    }

    // Image Preview Functions
    previewImageById(documentId, imageName) {
        const modal = new bootstrap.Modal(document.getElementById('imagePreviewModal'));

        // Reset modal state
        document.getElementById('imageModalTitle').innerHTML = `<i class="fas fa-file-image text-primary"></i> ${imageName}`;
        document.getElementById('imageLoader').style.display = 'flex';
        document.getElementById('previewImage').style.display = 'none';
        document.getElementById('imageError').style.display = 'none';

        // Load image using the preview endpoint
        const img = document.getElementById('previewImage');
        const previewUrl = `/Permit/PreviewDocument/${documentId}`;
        
        img.onload = function() {
            document.getElementById('imageLoader').style.display = 'none';
            img.style.display = 'block';
            this.currentImagePath = previewUrl;
        }.bind(this);

        img.onerror = function() {
            document.getElementById('imageLoader').style.display = 'none';
            document.getElementById('imageError').style.display = 'block';
        };

        img.src = previewUrl;
        modal.show();
    }

    previewImage(imagePath, imageName) {
        this.currentImagePath = imagePath;
        const modal = new bootstrap.Modal(document.getElementById('imagePreviewModal'));

        // Reset modal state
        document.getElementById('imageModalTitle').innerHTML = `<i class="fas fa-file-image text-primary"></i> ${imageName}`;
        document.getElementById('imageLoader').style.display = 'flex';
        document.getElementById('previewImage').style.display = 'none';
        document.getElementById('imageError').style.display = 'none';

        // Load image
        const img = document.getElementById('previewImage');
        img.onload = function() {
            document.getElementById('imageLoader').style.display = 'none';
            img.style.display = 'block';
        };

        img.onerror = function() {
            document.getElementById('imageLoader').style.display = 'none';
            document.getElementById('imageError').style.display = 'block';
        };

        img.src = imagePath;
        modal.show();
    }

    downloadFromImagePreview() {
        if (this.currentImagePath) {
            // If it's already a preview URL, convert it to download URL
            if (this.currentImagePath.includes('/PreviewDocument/')) {
                const documentId = this.currentImagePath.split('/').pop().split('?')[0];
                window.open(`/Permit/DownloadDocument/${documentId}`, '_blank');
            } else {
                window.open(this.currentImagePath, '_blank');
            }
        }
    }

    // Document Preview Functions
    previewDocumentById(documentId, documentName) {
        this.currentPreviewPath = `/Permit/PreviewDocument/${documentId}`;
        const modal = new bootstrap.Modal(document.getElementById('documentPreviewModal'));
        
        // Reset modal state
        document.getElementById('previewDocumentName').textContent = documentName;
        document.getElementById('previewLoader').style.display = 'flex';
        document.getElementById('previewFrame').style.display = 'none';
        document.getElementById('previewError').style.display = 'none';

        // Load document in iframe using the preview endpoint
        const iframe = document.getElementById('previewFrame');
        const previewUrl = `/Permit/PreviewDocument/${documentId}`;
        
        // Add timestamp to prevent caching issues
        const timestamp = new Date().getTime();
        const urlWithTimestamp = previewUrl + '?_t=' + timestamp;
        iframe.src = urlWithTimestamp;

        // Handle iframe load
        iframe.onload = function() {
            document.getElementById('previewLoader').style.display = 'none';
            iframe.style.display = 'block';
            
            // For verifikator, add success indicator and update progress
            if (this.userRole === 'Verifikator') {
                const documentCard = document.querySelector(`[onclick*="${documentId}"]`).closest('.document-preview-card');
                if (documentCard) {
                    documentCard.classList.add('completed');
                    this.updateVerifikatorProgress();
                    console.log('✅ Document preview loaded successfully for verifikator');
                }
            }
        }.bind(this);

        iframe.onerror = function() {
            document.getElementById('previewLoader').style.display = 'none';
            document.getElementById('previewError').style.display = 'flex';
        };

        // Fallback timeout
        setTimeout(() => {
            if (document.getElementById('previewLoader').style.display !== 'none') {
                document.getElementById('previewLoader').style.display = 'none';
                iframe.style.display = 'block';
            }
        }, 5000);

        modal.show();
    }

    previewDocument(filePath, documentName) {
        // Extract document ID from the filePath or use a different approach
        // For now, we'll use the direct file path but with proper error handling
        this.currentPreviewPath = filePath;
        const modal = new bootstrap.Modal(document.getElementById('documentPreviewModal'));
        
        // Reset modal state
        document.getElementById('previewDocumentName').textContent = documentName;
        document.getElementById('previewLoader').style.display = 'flex';
        document.getElementById('previewFrame').style.display = 'none';
        document.getElementById('previewError').style.display = 'none';

        // Load document in iframe
        const iframe = document.getElementById('previewFrame');
        
        // Add timestamp to prevent caching issues
        const timestamp = new Date().getTime();
        const urlWithTimestamp = filePath + (filePath.includes('?') ? '&' : '?') + '_t=' + timestamp;
        iframe.src = urlWithTimestamp;

        // Handle iframe load
        iframe.onload = function() {
            document.getElementById('previewLoader').style.display = 'none';
            iframe.style.display = 'block';
        };

        iframe.onerror = function() {
            document.getElementById('previewLoader').style.display = 'none';
            document.getElementById('previewError').style.display = 'flex';
        };

        // Fallback timeout
        setTimeout(() => {
            if (document.getElementById('previewLoader').style.display !== 'none') {
                document.getElementById('previewLoader').style.display = 'none';
                iframe.style.display = 'block';
            }
        }, 5000);

        modal.show();
    }

    downloadFromPreview() {
        if (this.currentPreviewPath) {
            // If it's already a preview URL, convert it to download URL
            if (this.currentPreviewPath.includes('/PreviewDocument/')) {
                const documentId = this.currentPreviewPath.split('/').pop().split('?')[0];
                window.open(`/Permit/DownloadDocument/${documentId}`, '_blank');
            } else {
                window.open(this.currentPreviewPath, '_blank');
            }
        }
    }

    previewPdfAsAdmin(permitId) {
        const modal = new bootstrap.Modal(document.getElementById('documentPreviewModal'));
        
        // Reset modal state
        document.getElementById('previewModalTitle').innerHTML = '<i class="fas fa-file-pdf text-danger"></i> Preview PDF - Admin';
        document.getElementById('previewDocumentName').textContent = 'Memuat preview PDF...';
        document.getElementById('previewLoader').style.display = 'flex';
        document.getElementById('previewFrame').style.display = 'none';
        document.getElementById('previewError').style.display = 'none';

        // Load PDF in iframe
        const iframe = document.getElementById('previewFrame');
        iframe.src = `/Permit/PreviewPdf/${permitId}`;

        // Handle iframe load
        iframe.onload = function() {
            document.getElementById('previewLoader').style.display = 'none';
            iframe.style.display = 'block';
            document.getElementById('previewDocumentName').textContent = 'Dokumen Izin Pengeluaran Ternak';
        };

        iframe.onerror = function() {
            document.getElementById('previewLoader').style.display = 'none';
            document.getElementById('previewError').style.display = 'flex';
        };

        // Fallback timeout
        setTimeout(() => {
            if (document.getElementById('previewLoader').style.display !== 'none') {
                document.getElementById('previewLoader').style.display = 'none';
                iframe.style.display = 'block';
            }
        }, 3000);

        modal.show();
    }

    // Approval Functions
    submitApproval(action) {
        this.selectedAction = action;

        // Validate comments for rejection
        if (action === 'Reject') {
            const comments = document.getElementById('Comments').value.trim();
            if (!comments) {
                alert('Komentar wajib diisi untuk penolakan permohonan');
                document.getElementById('Comments').focus();
                return;
            }
        }

        // Check document completeness for admin approval
        if (this.userRole === "Admin") {
            if (action === 'Approve' && this.requiredDocsCount < 7) {
                alert('Dokumen wajib belum lengkap. Permohonan tidak dapat disetujui.');
                return;
            }
        }

        // Show confirmation modal
        this.showConfirmationModal(action);
    }

    showConfirmationModal(action) {
        const modal = new bootstrap.Modal(document.getElementById('confirmationModal'));
        const modalTitle = document.getElementById('modalTitle');
        const confirmationIcon = document.getElementById('confirmationIcon');
        const confirmationMessage = document.getElementById('confirmationMessage');
        const warningText = document.getElementById('warningText');
        const confirmButton = document.getElementById('confirmButton');

        if (action === 'Approve') {
            modalTitle.textContent = 'Konfirmasi Persetujuan';
            confirmationIcon.innerHTML = '<i class="fas fa-check-circle text-success"></i>';
            confirmationMessage.textContent = 'Apakah Anda yakin ingin menyetujui permohonan ini?';
            confirmButton.className = 'btn btn-success';
            confirmButton.innerHTML = '<i class="fas fa-check"></i> Ya, Setujui';
            warningText.style.display = 'none';
        } else {
            modalTitle.textContent = 'Konfirmasi Penolakan';
            confirmationIcon.innerHTML = '<i class="fas fa-times-circle text-danger"></i>';
            confirmationMessage.textContent = 'Apakah Anda yakin ingin menolak permohonan ini?';
            confirmButton.className = 'btn btn-danger';
            confirmButton.innerHTML = '<i class="fas fa-times"></i> Ya, Tolak';
            warningText.style.display = 'block';
        }

        modal.show();
    }

    confirmAction() {
        // Set action value
        document.getElementById('actionInput').value = this.selectedAction;

        // Show loading state
        const approveBtn = document.getElementById('approveBtn');
        const rejectBtn = document.getElementById('rejectBtn');
        const confirmButton = document.getElementById('confirmButton');

        if (this.selectedAction === 'Approve') {
            approveBtn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Memproses...';
            approveBtn.disabled = true;
        } else {
            rejectBtn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Memproses...';
            rejectBtn.disabled = true;
        }

        confirmButton.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Memproses...';
        confirmButton.disabled = true;

        // Submit form
        document.getElementById('approvalForm').submit();
    }

    // Verifikator & Kepala Dinas Experience Functions
    initializeVerifikatorExperience() {
        this.enhanceVerifikatorExperience();
        this.updateVerifikatorProgress();
        this.initializeVerifikatorPdfContentView();
    }

    enhanceVerifikatorExperience() {
        if (this.userRole === 'Verifikator' || this.userRole === 'KepalaDinas') {
            const roleName = this.userRole === 'Verifikator' ? 'verifikator' : 'kepala dinas';
            console.log(`🔍 Enhancing ${roleName} experience...`);
            
            // Auto-focus on first PDF document
            const firstPdfButton = document.querySelector('.verifikator-documents-view .btn-preview.btn-primary');
            if (firstPdfButton) {
                setTimeout(() => {
                    firstPdfButton.focus();
                    console.log('🎯 Auto-focused on first PDF document');
                }, 500);
            }
            
            // Add keyboard shortcuts for verifikator
            document.addEventListener('keydown', function(e) {
                if (e.ctrlKey || e.metaKey) {
                    switch(e.key) {
                        case 'ArrowRight':
                            e.preventDefault();
                            this.navigateToNextDocument();
                            break;
                        case 'ArrowLeft':
                            e.preventDefault();
                            this.navigateToPreviousDocument();
                            break;
                        case 'Enter':
                            e.preventDefault();
                            this.openCurrentFocusedDocument();
                            break;
                    }
                }
            }.bind(this));
            
            // Add document navigation indicators
            this.addDocumentNavigationIndicators();
        }
    }
    
    updateVerifikatorProgress() {
        if (this.userRole === 'Verifikator' || this.userRole === 'KepalaDinas') {
            const totalDocuments = document.querySelectorAll('.verifikator-documents-view .document-preview-card').length;
            const reviewedDocuments = document.querySelectorAll('.verifikator-documents-view .document-preview-card.completed').length;
            const progressPercentage = totalDocuments > 0 ? (reviewedDocuments / totalDocuments) * 100 : 0;
            
            const progressCount = document.querySelector('.verifikator-progress-count');
            const progressFill = document.querySelector('.verifikator-progress-fill');
            
            if (progressCount) {
                progressCount.textContent = `${reviewedDocuments} / ${totalDocuments} dokumen`;
            }
            
            if (progressFill) {
                progressFill.style.width = `${progressPercentage}%`;
            }
            
            const roleName = this.userRole === 'Verifikator' ? 'Verifikator' : 'Kepala Dinas';
            console.log(`📊 ${roleName} progress: ${reviewedDocuments}/${totalDocuments} (${progressPercentage.toFixed(1)}%)`);
        }
    }
    
    navigateToNextDocument() {
        const currentFocused = document.querySelector('.verifikator-documents-view .btn-preview:focus');
        if (currentFocused) {
            const nextButton = currentFocused.closest('.document-preview-card').nextElementSibling?.querySelector('.btn-preview');
            if (nextButton) {
                nextButton.focus();
                nextButton.scrollIntoView({ behavior: 'smooth', block: 'center' });
            }
        }
    }
    
    navigateToPreviousDocument() {
        const currentFocused = document.querySelector('.verifikator-documents-view .btn-preview:focus');
        if (currentFocused) {
            const prevButton = currentFocused.closest('.document-preview-card').previousElementSibling?.querySelector('.btn-preview');
            if (prevButton) {
                prevButton.focus();
                prevButton.scrollIntoView({ behavior: 'smooth', block: 'center' });
            }
        }
    }
    
    openCurrentFocusedDocument() {
        const currentFocused = document.querySelector('.verifikator-documents-view .btn-preview:focus');
        if (currentFocused) {
            currentFocused.click();
        }
    }
    
    addDocumentNavigationIndicators() {
        const documentCards = document.querySelectorAll('.verifikator-documents-view .document-preview-card');
        documentCards.forEach((card, index) => {
            const indicator = document.createElement('div');
            indicator.className = 'document-navigation-indicator';
            indicator.innerHTML = `
                <span class="indicator-number">${index + 1}</span>
                <span class="indicator-total">/ ${documentCards.length}</span>
            `;
            card.appendChild(indicator);
        });
    }

    // PDF View Functions
    initializeVerifikatorPdfView() {
        if (this.userRole === 'Verifikator' || this.userRole === 'KepalaDinas') {
            const roleName = this.userRole === 'Verifikator' ? 'verifikator' : 'kepala dinas';
            console.log(`📄 Initializing ${roleName} PDF view...`);
            
            const pdfFrame = document.querySelector('.pdf-preview-frame iframe');
            const pdfContainer = document.querySelector('.pdf-preview-frame');
            
            if (pdfFrame && pdfContainer) {
                // Add loading state
                pdfContainer.classList.add('loading');
                
                // Handle iframe load
                pdfFrame.onload = function() {
                    console.log('✅ PDF loaded successfully');
                    pdfContainer.classList.remove('loading');
                    pdfContainer.classList.add('loaded');
                    
                    // Mark as completed
                    const pdfSection = document.querySelector('.verifikator-pdf-section');
                    if (pdfSection) {
                        pdfSection.classList.add('completed');
                    }
                };
                
                // Handle iframe error
                pdfFrame.onerror = function() {
                    console.log('❌ PDF failed to load');
                    pdfContainer.classList.remove('loading');
                    pdfContainer.classList.add('error');
                    
                    // Show error message
                    const errorMsg = document.createElement('div');
                    errorMsg.className = 'pdf-error-message';
                    errorMsg.innerHTML = `
                        <i class="fas fa-exclamation-triangle text-warning"></i>
                        <p>Gagal memuat dokumen PDF. Silakan coba buka di tab baru atau download.</p>
                    `;
                    pdfContainer.appendChild(errorMsg);
                };
                
                // Add keyboard shortcuts for PDF navigation
                document.addEventListener('keydown', function(e) {
                    if (e.ctrlKey || e.metaKey) {
                        switch(e.key) {
                            case 'KeyO':
                                e.preventDefault();
                                document.querySelector('.pdf-actions .btn-primary').click();
                                break;
                            case 'KeyD':
                                e.preventDefault();
                                document.querySelector('.pdf-actions .btn-outline-secondary').click();
                                break;
                        }
                    }
                });
            }
        }
    }

    initializeVerifikatorPdfOnlyView() {
        if (this.userRole === 'Verifikator' || this.userRole === 'KepalaDinas') {
            const roleName = this.userRole === 'Verifikator' ? 'verifikator' : 'kepala dinas';
            console.log(`📄 Initializing ${roleName} PDF-only view...`);
            
            const pdfContainer = document.querySelector('.pdf-preview-container');
            const pdfFrame = document.querySelector('.pdf-preview-container iframe');
            
            if (pdfContainer && pdfFrame) {
                // Add loading state
                pdfContainer.classList.add('loading');
                
                // Handle iframe load
                pdfFrame.onload = function() {
                    console.log('✅ PDF loaded successfully in full-screen view');
                    pdfContainer.classList.remove('loading');
                    pdfContainer.classList.add('loaded');
                };
                
                // Handle iframe error
                pdfFrame.onerror = function() {
                    console.log('❌ PDF failed to load in full-screen view');
                    pdfContainer.classList.remove('loading');
                    pdfContainer.classList.add('error');
                };
                
                // Add keyboard shortcuts for PDF navigation
                document.addEventListener('keydown', function(e) {
                    if (e.ctrlKey || e.metaKey) {
                        switch(e.key) {
                            case 'KeyO':
                                e.preventDefault();
                                document.querySelector('.pdf-floating-actions .btn-primary').click();
                                break;
                            case 'KeyD':
                                e.preventDefault();
                                document.querySelector('.pdf-floating-actions .btn-outline-secondary').click();
                                break;
                            case 'Escape':
                                e.preventDefault();
                                // Optionally add a way to exit full-screen mode
                                break;
                        }
                    }
                });
                
                // Auto-focus on PDF iframe for better UX
                setTimeout(() => {
                    pdfFrame.focus();
                }, 1000);
            }
        }
    }

    initializeVerifikatorPdfContentView() {
        if (this.userRole === 'Verifikator' || this.userRole === 'KepalaDinas') {
            const roleName = this.userRole === 'Verifikator' ? 'verifikator' : 'kepala dinas';
            console.log(`📄 Initializing ${roleName} PDF content view...`);
            
            // Add keyboard shortcuts for PDF actions
            document.addEventListener('keydown', function(e) {
                if (e.ctrlKey || e.metaKey) {
                    switch(e.key) {
                        case 'KeyP':
                            e.preventDefault();
                            this.printDocument();
                            break;
                        case 'KeyD':
                            e.preventDefault();
                            this.downloadPDF();
                            break;
                    }
                }
            }.bind(this));
        }
    }

    // Utility Functions
    printDocument() {
        console.log('🖨️ Printing document...');
        window.print();
    }

    downloadPDF() {
        console.log('📥 Downloading PDF...');
        window.open(`/Permit/DownloadPdfFile/${this.permitId}`, '_blank');
    }

    // PDF Navigation Functions for Verifikator
    scrollToTop() {
        console.log('⬆️ Scrolling to top of PDF content...');
        const pdfContainer = document.querySelector('.pdf-content-container');
        if (pdfContainer) {
            pdfContainer.scrollTo({
                top: 0,
                behavior: 'smooth'
            });
        }
    }

    scrollToBottom() {
        console.log('⬇️ Scrolling to bottom of PDF content...');
        const pdfContainer = document.querySelector('.pdf-content-container');
        if (pdfContainer) {
            pdfContainer.scrollTo({
                top: pdfContainer.scrollHeight,
                behavior: 'smooth'
            });
        }
    }

    // Enhanced Verifikator & Kepala Dinas PDF Experience
    initializeVerifikatorPdfContentView() {
        if (this.userRole !== 'Verifikator' && this.userRole !== 'KepalaDinas') return;

        const pdfContainer = document.querySelector('.pdf-content-container');
        if (!pdfContainer) return;

        // Add loading state
        pdfContainer.classList.add('loading');
        
        // Simulate loading completion after a short delay
        setTimeout(() => {
            pdfContainer.classList.remove('loading');
            pdfContainer.classList.add('loaded');
        }, 1000);

        // Add scroll progress indicator
        this.addScrollProgressIndicator(pdfContainer);
        
        // Add keyboard shortcuts for PDF navigation
        this.addPdfKeyboardShortcuts();
    }

    addScrollProgressIndicator(container) {
        const progressBar = document.createElement('div');
        progressBar.className = 'pdf-scroll-progress';
        progressBar.style.cssText = `
            position: absolute;
            top: 0;
            left: 0;
            height: 3px;
            background: var(--primary-color);
            transition: width 0.1s ease;
            z-index: 10;
            border-radius: 0 2px 2px 0;
        `;
        
        container.style.position = 'relative';
        container.appendChild(progressBar);

        container.addEventListener('scroll', () => {
            const scrollTop = container.scrollTop;
            const scrollHeight = container.scrollHeight - container.clientHeight;
            const progress = (scrollTop / scrollHeight) * 100;
            progressBar.style.width = `${progress}%`;
        });
    }

    addPdfKeyboardShortcuts() {
        document.addEventListener('keydown', (e) => {
            // Only activate shortcuts when PDF container is focused or visible
            const pdfContainer = document.querySelector('.pdf-content-container');
            if (!pdfContainer || !pdfContainer.offsetParent) return;

            switch (e.key) {
                case 'Home':
                    e.preventDefault();
                    this.scrollToTop();
                    break;
                case 'End':
                    e.preventDefault();
                    this.scrollToBottom();
                    break;
                case 'PageUp':
                    e.preventDefault();
                    pdfContainer.scrollBy({
                        top: -pdfContainer.clientHeight,
                        behavior: 'smooth'
                    });
                    break;
                case 'PageDown':
                    e.preventDefault();
                    pdfContainer.scrollBy({
                        top: pdfContainer.clientHeight,
                        behavior: 'smooth'
                    });
                    break;
            }
        });
    }
}

// Initialize the permit approval manager when DOM is loaded
document.addEventListener('DOMContentLoaded', function() {
    window.permitApprovalManager = new PermitApprovalManager();
});

// Global functions for backward compatibility
function previewImageById(documentId, imageName) {
    if (window.permitApprovalManager) {
        window.permitApprovalManager.previewImageById(documentId, imageName);
    }
}

function previewImage(imagePath, imageName) {
    if (window.permitApprovalManager) {
        window.permitApprovalManager.previewImage(imagePath, imageName);
    }
}

function downloadFromImagePreview() {
    if (window.permitApprovalManager) {
        window.permitApprovalManager.downloadFromImagePreview();
    }
}

function submitApproval(action) {
    if (window.permitApprovalManager) {
        window.permitApprovalManager.submitApproval(action);
    }
}

function showConfirmationModal(action) {
    if (window.permitApprovalManager) {
        window.permitApprovalManager.showConfirmationModal(action);
    }
}

function confirmAction() {
    if (window.permitApprovalManager) {
        window.permitApprovalManager.confirmAction();
    }
}

function previewDocumentById(documentId, documentName) {
    if (window.permitApprovalManager) {
        window.permitApprovalManager.previewDocumentById(documentId, documentName);
    }
}

function previewDocument(filePath, documentName) {
    if (window.permitApprovalManager) {
        window.permitApprovalManager.previewDocument(filePath, documentName);
    }
}

function downloadFromPreview() {
    if (window.permitApprovalManager) {
        window.permitApprovalManager.downloadFromPreview();
    }
}

function previewPdfAsAdmin(permitId) {
    if (window.permitApprovalManager) {
        window.permitApprovalManager.previewPdfAsAdmin(permitId);
    }
}

function printDocument() {
    if (window.permitApprovalManager) {
        window.permitApprovalManager.printDocument();
    }
}

function downloadPDF() {
    if (window.permitApprovalManager) {
        window.permitApprovalManager.downloadPDF();
    }
}

// PDF Navigation Functions for Verifikator
function scrollToTop() {
    if (window.permitApprovalManager) {
        window.permitApprovalManager.scrollToTop();
    }
}

function scrollToBottom() {
    if (window.permitApprovalManager) {
        window.permitApprovalManager.scrollToBottom();
    }
}
