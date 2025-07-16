/**
 * Simple Permit Index DataTable JavaScript
 * Fokus pada fungsionalitas utama dengan performa optimal
 */

let currentPermitId = null;
let currentApplicationNumber = null;
let table = null;

$(document).ready(function () {
    initializeDataTable();
    initializeFilters();
    initializeTooltips();
    setTimeout(animateProgressBars, 500);
});

/**
 * Initialize DataTable
 */
function initializeDataTable() {
    table = $('#permitsTable').DataTable({
        // Konfigurasi dasar
        pageLength: 10,
        responsive: true,
        ordering: true,
        searching: true,
        info: true,
        lengthChange: true,
        autoWidth: false,

        // Kolom yang bisa disortir
        columnDefs: [
            { orderable: false, targets: [6] }, // Action column
            { className: "text-center", targets: [3, 6] } // Center align
        ],

        // Default sorting
        order: [[4, "desc"]], // Sort by date

        // Bahasa Indonesia
        language: {
            emptyTable: "Tidak ada data yang tersedia",
            info: "Menampilkan _START_ sampai _END_ dari _TOTAL_ entri",
            infoEmpty: "Menampilkan 0 sampai 0 dari 0 entri",
            infoFiltered: "(difilter dari _MAX_ total entri)",
            lengthMenu: "Tampilkan _MENU_ entri",
            loadingRecords: "Memuat...",
            processing: "Memproses...",
            search: "Cari:",
            zeroRecords: "Tidak ditemukan data yang sesuai",
            paginate: {
                first: "Pertama",
                last: "Terakhir",
                next: "Berikutnya",
                previous: "Sebelumnya"
            }
        },

        // Callback setelah table di-render
        drawCallback: function () {
            updateTableStats();
            animateProgressBars();
            initializeTooltips();
        }
    });
}

/**
 * Initialize filters
 */
function initializeFilters() {
    // Status filter
    $('#statusFilter').on('change', function () {
        const value = this.value;
        table.column(3).search(value).draw();
    });

    // Date filter
    $('#dateFilter').on('change', function () {
        const value = this.value;
        if (value) {
            const formattedDate = formatDateForFilter(value);
            table.column(4).search(formattedDate).draw();
        } else {
            table.column(4).search('').draw();
        }
    });

    // Search filter dengan debounce
    let searchTimeout;
    $('#searchFilter').on('keyup', function () {
        const value = this.value;
        clearTimeout(searchTimeout);
        searchTimeout = setTimeout(() => {
            table.search(value).draw();
        }, 300);
    });
}

/**
 * Format date untuk filter
 */
function formatDateForFilter(dateValue) {
    const date = new Date(dateValue);
    const day = date.getDate().toString().padStart(2, '0');
    const month = (date.getMonth() + 1).toString().padStart(2, '0');
    const year = date.getFullYear();
    return `${day}/${month}/${year}`;
}

/**
 * Reset semua filter
 */
function resetFilters() {
    $('#statusFilter').val('');
    $('#dateFilter').val('');
    $('#searchFilter').val('');
    table.search('').columns().search('').draw();
    showNotification('Filter berhasil direset', 'success');
}

/**
 * Update statistik table
 */
function updateTableStats() {
    const info = table.page.info();
    $('#totalCount').text(info.recordsDisplay);

    // Hitung status dari data yang terfilter
    let pendingCount = 0;
    let approvedCount = 0;

    table.rows({ search: 'applied' }).every(function () {
        const data = this.data();
        const statusCell = $(data[3]);

        if (statusCell.find('.badge-pending, .badge-warning').length > 0) {
            pendingCount++;
        } else if (statusCell.find('.badge-success, .badge-approved').length > 0) {
            approvedCount++;
        }
    });

    $('#pendingCount').text(pendingCount);
    $('#approvedCount').text(approvedCount);
}

/**
 * Animate progress bars
 */
function animateProgressBars() {
    $('.progress-bar').each(function () {
        const $bar = $(this);
        const targetWidth = $bar.css('width');

        // Reset dan animate
        $bar.css('width', '0%');
        setTimeout(() => {
            $bar.css('width', targetWidth);
        }, 100);
    });
}

/**
 * Initialize tooltips
 */
function initializeTooltips() {
    // Hapus tooltip lama
    $('[data-bs-toggle="tooltip"]').tooltip('dispose');

    // Tambah tooltip baru
    $('[data-bs-toggle="tooltip"]').tooltip({
        placement: 'top',
        delay: { show: 500, hide: 100 }
    });

    // Auto-add tooltips untuk tombol
    $('.btn-sm').each(function () {
        if (!$(this).attr('title')) {
            const icon = $(this).find('i').attr('class') || '';
            let title = 'Aksi';

            if (icon.includes('fa-eye')) title = 'Lihat Detail';
            else if (icon.includes('fa-check-circle')) title = 'Proses Permohonan';
            else if (icon.includes('fa-download')) title = 'Download Dokumen';
            else if (icon.includes('fa-file-pdf')) title = 'Lihat Dokumen';

            $(this).attr('title', title).attr('data-bs-toggle', 'tooltip');
        }
    });
}

/**
 * Document viewer modal
 */
function viewDocumentModal(permitId, applicationNumber) {
    currentPermitId = permitId;
    currentApplicationNumber = applicationNumber;

    // Show modal
    const modal = new bootstrap.Modal(document.getElementById('documentModal'));
    modal.show();

    // Reset state
    resetModalState(applicationNumber);
    updateApproveButton(permitId);
    loadDocumentInModal(permitId);
}

function resetModalState(applicationNumber) {
    $('#documentTitle').text(`Dokumen ${applicationNumber}`);
    $('#documentLoader').show();
    $('#documentFrame').hide();
    $('#documentError').hide();
}

function updateApproveButton(permitId) {
    const approveBtn = $('#approveFromModal');
    const userRole = window.userRole || 'User';

    if (['Verifikator', 'KepalaDinas', 'Admin'].includes(userRole)) {
        approveBtn.attr('href', `/Permit/Approve/${permitId}`).show();
    } else {
        approveBtn.hide();
    }
}

function loadDocumentInModal(permitId) {
    const iframe = $('#documentFrame')[0];
    const loader = $('#documentLoader');
    const errorDiv = $('#documentError');

    // Set source
    iframe.src = `/Permit/GetDocumentFile/${permitId}`;

    // Handle load
    iframe.onload = function () {
        loader.hide();
        $(iframe).show();
        errorDiv.hide();
    };

    // Handle error
    iframe.onerror = function () {
        loader.hide();
        $(iframe).hide();
        errorDiv.show();
    };

    // Fallback timeout
    setTimeout(() => {
        if (loader.is(':visible')) {
            loader.hide();
            $(iframe).show();
        }
    }, 5000);
}

function downloadFromModal() {
    if (currentPermitId) {
        window.open(`/Permit/Download/${currentPermitId}`, '_blank');
        showNotification('Download dimulai...', 'info');
    }
}

function printFromModal() {
    const iframe = $('#documentFrame')[0];
    try {
        if (iframe.contentWindow) {
            iframe.contentWindow.print();
        } else {
            const printWindow = window.open(`/Permit/GetDocumentFile/${currentPermitId}`, '_blank');
            printWindow.onload = () => printWindow.print();
        }
    } catch (e) {
        showNotification('Tidak dapat mencetak. Silakan buka di tab baru.', 'warning');
    }
}

function openInNewTab() {
    if (currentPermitId) {
        window.open(`/Permit/ViewDocument/${currentPermitId}`, '_blank');
    }
}

function retryLoadDocument() {
    if (currentPermitId) {
        loadDocumentInModal(currentPermitId);
    }
}

/**
 * Export to CSV
 */
function exportToCSV() {
    let csv = 'No. Aplikasi,Perusahaan,Pemohon,Status,Tanggal,Asal,Tujuan\n';

    table.rows({ search: 'applied' }).every(function () {
        const data = this.data();
        const $row = $(data);

        // Extract data dari setiap kolom
        const appNumber = $row.find('td:eq(0) strong').text().replace(/"/g, '""');
        const company = $row.find('td:eq(1) .company-name').text().replace(/"/g, '""');
        const applicant = $row.find('td:eq(2)').text().trim().replace(/"/g, '""');
        const status = $row.find('td:eq(3) .status-badge').text().replace(/"/g, '""');
        const date = $row.find('td:eq(4)').text().split('\n')[0].trim().replace(/"/g, '""');
        const origin = $row.find('td:eq(5) .location-item:first .location-text').text().replace(/"/g, '""');
        const destination = $row.find('td:eq(5) .location-item:last .location-text').text().replace(/"/g, '""');

        csv += `"${appNumber}","${company}","${applicant}","${status}","${date}","${origin}","${destination}"\n`;
    });

    // Download
    const blob = new Blob([csv], { type: 'text/csv;charset=utf-8;' });
    const link = document.createElement('a');
    const url = URL.createObjectURL(blob);
    link.setAttribute('href', url);
    link.setAttribute('download', `daftar_permohonan_${new Date().toISOString().split('T')[0]}.csv`);
    link.style.visibility = 'hidden';
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);

    showNotification('Data berhasil diekspor ke CSV', 'success');
}

/**
 * Print table
 */
function printTable() {
    window.print();
}

/**
 * Notification system
 */
function showNotification(message, type = 'info') {
    const iconMap = {
        success: 'fa-check-circle',
        warning: 'fa-exclamation-triangle',
        danger: 'fa-times-circle',
        info: 'fa-info-circle'
    };

    const notification = $(`
        <div class="alert alert-${type} alert-dismissible fade show" 
             style="position: fixed; top: 20px; right: 20px; z-index: 9999; max-width: 400px;" 
             role="alert">
            <i class="fas ${iconMap[type] || iconMap.info}"></i>
            ${message}
            <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
        </div>
    `);

    $('body').append(notification);

    // Auto-hide
    setTimeout(() => {
        notification.fadeOut(() => notification.remove());
    }, 5000);
}

/**
 * Keyboard shortcuts
 */
$(document).on('keydown', function (e) {
    // Ctrl+F: Focus search
    if (e.ctrlKey && e.key === 'f') {
        e.preventDefault();
        $('#searchFilter').focus();
    }

    // Ctrl+R: Reset filters
    if (e.ctrlKey && e.key === 'r') {
        e.preventDefault();
        resetFilters();
    }

    // Ctrl+E: Export
    if (e.ctrlKey && e.key === 'e') {
        e.preventDefault();
        exportToCSV();
    }

    // Ctrl+P: Print
    if (e.ctrlKey && e.key === 'p') {
        e.preventDefault();
        printTable();
    }
});

/**
 * Modal cleanup
 */
$('#documentModal').on('hidden.bs.modal', function () {
    $('#documentFrame')[0].src = 'about:blank';
    currentPermitId = null;
    currentApplicationNumber = null;
});

/**
 * Refresh data
 */
function refreshData() {
    table.ajax.reload(null, false);
    showNotification('Data diperbarui', 'success');
}

/**
 * Toggle column visibility
 */
function toggleColumn(columnIndex) {
    const column = table.column(columnIndex);
    column.visible(!column.visible());
}

// Auto-refresh setiap 5 menit (opsional)
// setInterval(refreshData, 5 * 60 * 1000);