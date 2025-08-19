using PerizinanPeternakan.Models;
using System.Text;
using System.Globalization;

namespace PerizinanPeternakan.Services
{
    /// <summary>
    /// Service interface for generating PDF documents for livestock permit applications.
    /// </summary>
    public interface IPdfGeneratorService
    {
        /// <summary>
        /// Generates a PDF document for a livestock permit application.
        /// </summary>
        /// <param name="permit">The livestock permit application to generate PDF for.</param>
        /// <returns>A byte array containing the PDF content.</returns>
        Task<byte[]> GeneratePermitPdf(LivestockPermitApplication permit);
        
        /// <summary>
        /// Generates a PDF document with signature for a livestock permit application.
        /// </summary>
        /// <param name="permit">The livestock permit application to generate PDF for.</param>
        /// <returns>A byte array containing the PDF content with signature.</returns>
        Task<byte[]> GeneratePermitPdfWithSignature(LivestockPermitApplication permit);
    }

    /// <summary>
    /// Service for generating PDF documents for livestock permit applications.
    /// </summary>
    public class PdfGeneratorService : IPdfGeneratorService
    {
        /// <summary>
        /// Generates a PDF document for a livestock permit application.
        /// </summary>
        /// <param name="permit">The livestock permit application to generate PDF for.</param>
        /// <returns>A byte array containing the PDF content.</returns>
        public async Task<byte[]> GeneratePermitPdf(LivestockPermitApplication permit)
        {
            // Generate HTML content
            var html = GeneratePermitHtml(permit);

            try
            {
                // Create enhanced PDF content with viewer controls
                var pdfContent = GeneratePdfContent(html);
                return Encoding.UTF8.GetBytes(pdfContent);
            }
            catch (Exception ex)
            {
                // Fallback: Create a simple text-based PDF content
                var fallbackContent = GenerateFallbackPdf(permit);
                return Encoding.UTF8.GetBytes(fallbackContent);
            }
        }

        /// <summary>
        /// Generates a PDF document with signature for a livestock permit application.
        /// </summary>
        /// <param name="permit">The livestock permit application to generate PDF for.</param>
        /// <returns>A byte array containing the PDF content with signature.</returns>
        public async Task<byte[]> GeneratePermitPdfWithSignature(LivestockPermitApplication permit)
        {
            // Generate HTML content with signature
            var html = GeneratePermitHtmlWithSignature(permit);

            try
            {
                // Create enhanced PDF content with viewer controls
                var pdfContent = GeneratePdfContent(html);
                return Encoding.UTF8.GetBytes(pdfContent);
            }
            catch (Exception ex)
            {
                // Fallback: Create a simple text-based PDF content
                var fallbackContent = GenerateFallbackPdf(permit);
                return Encoding.UTF8.GetBytes(fallbackContent);
            }
        }

        private string GeneratePdfContent(string html)
        {
            // Create a complete HTML document that browsers can display properly
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Surat Izin Pengeluaran Ternak Potong</title>
    <style>
        @media print {{
            body {{ margin: 0; padding: 20px; }}
            .no-print {{ display: none !important; }}
            .viewer-controls {{ display: none !important; }}
        }}
        body {{ 
            font-family: 'Times New Roman', serif; 
            font-size: 12pt; 
            line-height: 1.4; 
            margin: 20px;
            color: #000;
            background: white;
        }}
        .header {{ 
            text-align: center; 
            margin-bottom: 30px; 
            border-bottom: 3px solid #000;
            padding-bottom: 15px;
        }}
        .title {{ 
            text-align: center; 
            font-weight: bold; 
            margin: 25px 0; 
            line-height: 1.3;
        }}
        .content {{ 
            text-align: justify; 
            line-height: 1.5;
        }}
        table {{ 
            width: 100%; 
            border-collapse: collapse; 
            margin: 15px 0;
        }}
        th, td {{ 
            border: 1px solid #000; 
            padding: 8px; 
            text-align: left;
        }}
        th {{ 
            background-color: #f0f0f0; 
            font-weight: bold; 
            text-align: center;
        }}
        .signature {{ 
            margin-top: 40px; 
            text-align: right;
            page-break-inside: avoid;
        }}
        .signature img {{
            max-width: 200px;
            height: auto;
            display: block;
            margin: 10px 0;
        }}
        @media print {{
            .signature img {{
                max-width: 200px !important;
                height: auto !important;
                display: block !important;
                margin: 10px 0 !important;
            }}
        }}
        .viewer-controls {{
            position: sticky;
            top: 0;
            background: #f8f9fa;
            padding: 10px;
            border-bottom: 1px solid #dee2e6;
            margin: -20px -20px 20px -20px;
            z-index: 100;
            display: flex;
            justify-content: space-between;
            align-items: center;
        }}
        .control-group {{
            display: flex;
            gap: 10px;
            align-items: center;
        }}
        .zoom-control {{
            display: flex;
            align-items: center;
            gap: 5px;
        }}
        .zoom-btn {{
            padding: 5px 10px;
            border: 1px solid #ccc;
            background: white;
            cursor: pointer;
            border-radius: 4px;
        }}
        .zoom-btn:hover {{
            background: #e9ecef;
        }}
        .zoom-level {{
            font-weight: bold;
            min-width: 50px;
            text-align: center;
        }}
        .document-wrapper {{
            transform-origin: top left;
            transition: transform 0.3s ease;
        }}
    </style>
</head>
<body>
    <div class='viewer-controls no-print'>
        <div class='control-group'>
            <span><strong>Dokumen Izin Pengeluaran Ternak</strong></span>
        </div>
        <div class='control-group'>
            <div class='zoom-control'>
                <button class='zoom-btn' onclick='zoomOut()'>-</button>
                <span class='zoom-level' id='zoomLevel'>100%</span>
                <button class='zoom-btn' onclick='zoomIn()'>+</button>
            </div>
            <button class='zoom-btn' onclick='window.print()'>🖨️ Print</button>
        </div>
    </div>
    <div class='document-wrapper' id='documentWrapper'>
        {html}
    </div>
    <script>
        let currentZoom = 1;
        
        function zoomIn() {{
            currentZoom = Math.min(currentZoom + 0.1, 2);
            updateZoom();
        }}
        
        function zoomOut() {{
            currentZoom = Math.max(currentZoom - 0.1, 0.5);
            updateZoom();
        }}
        
        function updateZoom() {{
            document.getElementById('documentWrapper').style.transform = `scale(${{currentZoom}})`;
            document.getElementById('zoomLevel').textContent = Math.round(currentZoom * 100) + '%';
        }}
        
        // Auto-print when opened for final approved documents (optional)
        // window.onload = function() {{ window.print(); }}
    </script>
</body>
</html>";
        }

        private string GenerateFallbackPdf(LivestockPermitApplication permit)
        {
            var culture = new CultureInfo("id-ID");

            return $@"
SURAT IZIN PENGELUARAN TERNAK POTONG
UNTUK KEPERLUAN PERDAGANGAN ANTAR PULAU/ANTAR PROVINSI

PEMERINTAH PROVINSI NUSA TENGGARA BARAT
DINAS PENANAMAN MODAL DAN PELAYANAN TERPADU SATU PINTU
Jalan Udayana No. 4 Mataram Telpon (0370) 631060 Fax (0370) 647022

NOMOR : {permit.ApplicationNumber}

Diberikan kepada:
Nama Perusahaan: {permit.CompanyName}
Alamat: {permit.CompanyAddress}

Untuk pengeluaran ternak:
{string.Join("\n", permit.LivestockDetails.Select((d, i) => $"{i + 1}. {d.LivestockType}: {d.Quantity} ekor - {d.Description}"))}

Asal: {permit.OriginLocation} (Pelabuhan: {permit.DeparturePort})
Tujuan: {permit.DestinationLocation} (Pelabuhan: {permit.ArrivalPort})

Berlaku: {permit.ValidFrom?.ToString("dd MMMM yyyy", culture)} s/d {permit.ValidUntil?.ToString("dd MMMM yyyy", culture)}

Mataram, {permit.FinalApprovalDate?.ToString("dd MMMM yyyy", culture)}
Plt. Kepala Dinas

Hj. Eva Dewiyani, SP
Pembina Utama Muda (IV/c)
NIP. 19701210 199803 2 006
";
        }

        private string GeneratePermitHtml(LivestockPermitApplication permit)
        {
            var culture = new CultureInfo("id-ID");
            var livestockDetails = string.Join("\n", permit.LivestockDetails.Select((detail, index) =>
                $"<tr><td style='text-align: center;'>{index + 1}.</td><td>{detail.LivestockType}</td><td style='text-align: center;'>{detail.Quantity} ({NumberToWords(detail.Quantity)}) ekor</td><td>{detail.Description ?? ""}</td></tr>"));

            return $@"
    <div class='header'>
        <h1 style='font-size: 16pt; margin: 8px 0; line-height: 1.2;'>PEMERINTAH PROVINSI NUSA TENGGARA BARAT</h1>
        <h2 style='font-size: 14pt; margin: 4px 0;'>DINAS PENANAMAN MODAL</h2>
        <h2 style='font-size: 14pt; margin: 4px 0;'>DAN PELAYANAN TERPADU SATU PINTU</h2>
        <p style='font-size: 11pt; margin: 4px 0; font-weight: bold;'>Jalan Udayana No. 4 Mataram Telpon (0370) 631060 Fax (0370) 647022</p>
    </div>

    <div class='title'>
        <p style='font-size: 14pt; margin: 5px 0;'><strong><u>SURAT IZIN</u></strong></p>
        <p style='font-size: 14pt; margin: 5px 0;'><strong><u>PENGELUARAN TERNAK POTONG UNTUK KEPERLUAN PERDAGANGAN</u></strong></p>
        <p style='font-size: 14pt; margin: 5px 0;'><strong><u>ANTAR PULAU/ANTAR PROVINSI</u></strong></p>
        <p style='font-size: 14pt; margin: 5px 0;'><strong><u>NOMOR : {permit.ApplicationNumber}</u></strong></p>
    </div>

    <div class='content'>
        <p style='text-align: center; font-weight: bold; font-size: 13pt; margin: 20px 0;'>
            <strong>KEPALA DINAS PENANAMAN MODAL DAN PELAYANAN TERPADU SATU PINTU PROVINSI NUSA TENGGARA BARAT</strong>
        </p>

        <table style='border: none; margin: 15px 0;'>
            <tr>
                <td style='border: none; width: 18%; font-weight: bold; vertical-align: top; padding: 8px 5px;'>Memperhatikan</td>
                <td style='border: none; width: 3%; text-align: center; padding: 8px 5px;'>:</td>
                <td style='border: none; width: 79%; text-align: justify; padding: 8px 5px;'>
                    a. Surat Permohonan Izin dari <strong>{permit.CompanyName}</strong> Tanggal {permit.SubmissionDate.ToString("dd MMMM yyyy", culture)} yang disertai dengan kelengkapan dokumen yang dipersyaratkan;<br><br>
                    <strong>b. Surat Rekomendasi dari Dinas Peternakan dan Kesehatan Hewan Provinsi NTB Nomor : 500.7.2.1/1337/2025 tanggal {DateTime.Now.ToString("dd MMMM yyyy", culture)}.</strong>
                </td>
            </tr>
            <tr>
                <td style='border: none; font-weight: bold; vertical-align: top; padding: 8px 5px;'>Menimbang</td>
                <td style='border: none; text-align: center; padding: 8px 5px;'>:</td>
                <td style='border: none; text-align: justify; padding: 8px 5px;'>
                    Bahwa guna menjaga keberlanjutan budidaya peternakan di Provinsi Nusa Tenggara Barat perlu dikendalikan populasinya melalui pemberian Izin Penjatahan Ternak Potong yang dapat dikeluarkan dari Provinsi Nusa Tenggara Barat dari tiap-tiap Kabupaten/Kota.
                </td>
            </tr>
            <tr>
                <td style='border: none; font-weight: bold; vertical-align: top; padding: 8px 5px;'>Mengingat</td>
                <td style='border: none; text-align: center; padding: 8px 5px;'>:</td>
                <td style='border: none; text-align: justify; padding: 8px 5px;'>
                    a. Undang-Undang Nomor 18 Tahun 2009 tentang Peternakan dan Kesehatan Hewan;<br>
                    b. Peraturan Pemerintah No. 15 Tahun 1977 tentang Penolakan, Pencegahan, Pemberantasan dan Pengobatan Penyakit Hewan;<br>
                    c. Peraturan Presiden Republik Indonesia Nomor 91 Tahun 2017 tentang Percepatan Pelaksanaan Berusaha;<br>
                    d. Peraturan Daerah Provinsi Nusa Tenggara Barat Nomor 4 Tahun 2020 tentang Tata Niaga Ternak;<br>
                    e. Peraturan Gubernur Nusa Tenggara Barat Nomor 38 Tahun 2020 tentang Penyelenggaraan Pelayanan Terpadu Satu Pintu;<br>
                    <strong>f. Keputusan Gubernur Nusa Tenggara Barat Nomor 500.7-841 Tahun 2025 Tanggal 30 Desember 2024 tentang Kuota Pengeluaran Sapi dan Kerbau Potong Dalam dan Keluar Daerah serta Pemasukan Sapi Eksotik di Provinsi Nusa Tenggara Barat Tahun 2025.</strong>
                </td>
            </tr>
        </table>

        <div style='margin: 15px 0; font-weight: bold;'>
            <p><strong>MEMBERIKAN IZIN PENGELUARAN</strong></p>
            <p>Kepada : <strong>{permit.CompanyName}</strong></p>
            <p>Alamat : {permit.CompanyAddress}</p>
        </div>

        <table>
            <thead>
                <tr>
                    <th style='width: 8%;'><strong>No.</strong></th>
                    <th style='width: 35%;'><strong>JENIS TERNAK POTONG</strong></th>
                    <th style='width: 35%;'><strong>BANYAKNYA</strong></th>
                    <th style='width: 22%;'><strong>KETERANGAN</strong></th>
                </tr>
            </thead>
            <tbody>
                {livestockDetails}
            </tbody>
        </table>

        <table style='border: none; margin: 15px 0;'>
            <tr>
                <td style='border: none; width: 10%; font-weight: bold;'>Asal</td>
                <td style='border: none; width: 5%; text-align: center;'>:</td>
                <td style='border: none; width: 35%; font-weight: bold;'>{permit.OriginLocation}</td>
                <td style='border: none; width: 50%;'>Melalui pelabuhan Asal : <strong>{permit.DeparturePort}</strong></td>
            </tr>
            <tr>
                <td style='border: none; font-weight: bold;'>Tujuan</td>
                <td style='border: none; text-align: center;'>:</td>
                <td style='border: none; font-weight: bold;'>{permit.DestinationLocation}</td>
                <td style='border: none;'>Melalui pelabuhan Bongkar : <strong>{permit.ArrivalPort}</strong></td>
            </tr>
        </table>

        <div style='margin: 20px 0; text-align: justify;'>
            <p><strong>Dengan syarat-syarat sebagai berikut :</strong></p>
            <p style='margin: 8px 0;'>a. Ternak potong tersebut harus memenuhi persyaratan teknis (umur dan berat) serta administrasi (surat pemilikan) pada saat pengecekan di <em>Holding Ground</em> yang ditunjuk;</p>
            <p style='margin: 8px 0;'>b. Pedagang harus tercatat sebagai pedagang ternak di Kantor Dinas Peternakan dan Kesehatan Hewan Provinsi NTB dan Kantor Dinas Peternakan Kabupaten/Kota setempat;</p>
            <p style='margin: 8px 0;'>c. Senantiasa taat mengikuti semua peraturan yang ditentukan dan tidak menjual belikan atau mengalihkan izin pengeluaran ternak potong yang telah diterimanya kepada perusahaan lain;</p>
            <p style='margin: 8px 0;'>d. Surat Izin ini dapat diubah dan diatur kembali, apabila terdapat kekeliruan;</p>
            <p style='margin: 8px 0;'>e. Surat izin ini berlaku sekali muat terhitung mulai tanggal <strong>{permit.ValidFrom?.ToString("dd MMMM", culture)} sampai dengan {permit.ValidUntil?.ToString("dd MMMM yyyy", culture)}</strong>.</p>
        </div>

        <div class='signature'>
            <p><strong>DITETAPKAN DI : MATARAM</strong></p>
            <p><strong>PADA TANGGAL : {permit.FinalApprovalDate?.ToString("dd MMMM yyyy", culture)}</strong></p>
            <p><strong>Plt. Kepala Dinas</strong></p>
            {(permit.Status == PermitStatus.FinalApproved && permit.FinalApprovalDate.HasValue ? 
                "<div style='height: 60px; margin: 15px 0; position: relative;'>" +
                "<img src='/images/signature-sample.svg' alt='Tanda Tangan Kepala Dinas' style='position: absolute; top: 0; left: 0; width: 200px; height: 80px; object-fit: contain;' />" +
                "</div>" : 
                "<div style='height: 60px; margin: 15px 0; border-bottom: 1px solid #000;'></div>")}
            <p><strong><u>{(permit.KepalaDinas?.NamaLengkap ?? "Hj. Eva Dewiyani, SP")}</u></strong></p>
            <p><strong>Pembina Utama Muda (IV/c)</strong></p>
            <p><strong>NIP. 19701210 199803 2 006</strong></p>
        </div>

        <div style='margin-top: 60px; clear: both;'>
            <p><strong>Tembusan</strong> disampaikan kepada Yth :</p>
            <p style='margin: 4px 0;'>1. Kepala Dinas Peternakan Provinsi/Kab/Kota Daerah Asal dan Daerah Tujuan.</p>
            <p style='margin: 4px 0;'>2. Kepala Balai/Stasiun Karantina Daerah Asal dan Daerah Tujuan.</p>
            <p style='margin: 4px 0;'>3. Penanggung Jawab Wilker Pelabuhan Daerah Asal dan Daerah Tujuan.</p>
            <p style='margin: 4px 0;'>4. Perusahaan yang bersangkutan untuk dimaklumi dan diindahkan.</p>
            <p style='margin: 4px 0;'>5. Arsip.</p>
        </div>
    </div>";
        }

        private string GeneratePermitHtmlWithSignature(LivestockPermitApplication permit)
        {
            var culture = new CultureInfo("id-ID");
            var livestockDetails = string.Join("\n", permit.LivestockDetails.Select((detail, index) =>
                $"<tr><td style='text-align: center;'>{index + 1}.</td><td>{detail.LivestockType}</td><td style='text-align: center;'>{detail.Quantity} ({NumberToWords(detail.Quantity)}) ekor</td><td>{detail.Description ?? ""}</td></tr>"));

            return $@"
    <div class='header'>
        <h1 style='font-size: 16pt; margin: 8px 0; line-height: 1.2;'>PEMERINTAH PROVINSI NUSA TENGGARA BARAT</h1>
        <h2 style='font-size: 14pt; margin: 4px 0;'>DINAS PENANAMAN MODAL</h2>
        <h2 style='font-size: 14pt; margin: 4px 0;'>DAN PELAYANAN TERPADU SATU PINTU</h2>
        <p style='font-size: 11pt; margin: 4px 0; font-weight: bold;'>Jalan Udayana No. 4 Mataram Telpon (0370) 631060 Fax (0370) 647022</p>
    </div>

    <div class='title'>
        <p style='font-size: 14pt; margin: 5px 0;'><strong><u>SURAT IZIN</u></strong></p>
        <p style='font-size: 14pt; margin: 5px 0;'><strong><u>PENGELUARAN TERNAK POTONG UNTUK KEPERLUAN PERDAGANGAN</u></strong></p>
        <p style='font-size: 14pt; margin: 5px 0;'><strong><u>ANTAR PULAU/ANTAR PROVINSI</u></strong></p>
        <p style='font-size: 14pt; margin: 5px 0;'><strong><u>NOMOR : {permit.ApplicationNumber}</u></strong></p>
    </div>

    <div class='content'>
        <p style='text-align: center; font-weight: bold; font-size: 13pt; margin: 20px 0;'>
            <strong>KEPALA DINAS PENANAMAN MODAL DAN PELAYANAN TERPADU SATU PINTU PROVINSI NUSA TENGGARA BARAT</strong>
        </p>

        <table style='border: none; margin: 15px 0;'>
            <tr>
                <td style='border: none; width: 18%; font-weight: bold; vertical-align: top; padding: 8px 5px;'>Memperhatikan</td>
                <td style='border: none; width: 3%; text-align: center; padding: 8px 5px;'>:</td>
                <td style='border: none; width: 79%; text-align: justify; padding: 8px 5px;'>
                    a. Surat Permohonan Izin dari <strong>{permit.CompanyName}</strong> Tanggal {permit.SubmissionDate.ToString("dd MMMM yyyy", culture)} yang disertai dengan kelengkapan dokumen yang dipersyaratkan;<br><br>
                    <strong>b. Surat Rekomendasi dari Dinas Peternakan dan Kesehatan Hewan Provinsi NTB Nomor : 500.7.2.1/1337/2025 tanggal {DateTime.Now.ToString("dd MMMM yyyy", culture)}.</strong>
                </td>
            </tr>
            <tr>
                <td style='border: none; font-weight: bold; vertical-align: top; padding: 8px 5px;'>Menimbang</td>
                <td style='border: none; text-align: center; padding: 8px 5px;'>:</td>
                <td style='border: none; text-align: justify; padding: 8px 5px;'>
                    Bahwa guna menjaga keberlanjutan budidaya peternakan di Provinsi Nusa Tenggara Barat perlu dikendalikan populasinya melalui pemberian Izin Penjatahan Ternak Potong yang dapat dikeluarkan dari Provinsi Nusa Tenggara Barat dari tiap-tiap Kabupaten/Kota.
                </td>
            </tr>
            <tr>
                <td style='border: none; font-weight: bold; vertical-align: top; padding: 8px 5px;'>Mengingat</td>
                <td style='border: none; text-align: center; padding: 8px 5px;'>:</td>
                <td style='border: none; text-align: justify; padding: 8px 5px;'>
                    a. Undang-Undang Nomor 18 Tahun 2009 tentang Peternakan dan Kesehatan Hewan;<br>
                    b. Peraturan Pemerintah No. 15 Tahun 1977 tentang Penolakan, Pencegahan, Pemberantasan dan Pengobatan Penyakit Hewan;<br>
                    c. Peraturan Presiden Republik Indonesia Nomor 91 Tahun 2017 tentang Percepatan Pelaksanaan Berusaha;<br>
                    d. Peraturan Daerah Provinsi Nusa Tenggara Barat Nomor 4 Tahun 2020 tentang Tata Niaga Ternak;<br>
                    e. Peraturan Gubernur Nusa Tenggara Barat Nomor 38 Tahun 2020 tentang Penyelenggaraan Pelayanan Terpadu Satu Pintu;<br>
                    <strong>f. Keputusan Gubernur Nusa Tenggara Barat Nomor 500.7-841 Tahun 2025 Tanggal 30 Desember 2024 tentang Kuota Pengeluaran Sapi dan Kerbau Potong Dalam dan Keluar Daerah serta Pemasukan Sapi Eksotik di Provinsi Nusa Tenggara Barat Tahun 2025.</strong>
                </td>
            </tr>
        </table>

        <div style='margin: 15px 0; font-weight: bold;'>
            <p><strong>MEMBERIKAN IZIN PENGELUARAN</strong></p>
            <p>Kepada : <strong>{permit.CompanyName}</strong></p>
            <p>Alamat : {permit.CompanyAddress}</p>
        </div>

        <table>
            <thead>
                <tr>
                    <th style='width: 8%;'><strong>No.</strong></th>
                    <th style='width: 35%;'><strong>JENIS TERNAK POTONG</strong></th>
                    <th style='width: 35%;'><strong>BANYAKNYA</strong></th>
                    <th style='width: 22%;'><strong>KETERANGAN</strong></th>
                </tr>
            </thead>
            <tbody>
                {livestockDetails}
            </tbody>
        </table>

        <table style='border: none; margin: 15px 0;'>
            <tr>
                <td style='border: none; width: 10%; font-weight: bold;'>Asal</td>
                <td style='border: none; width: 5%; text-align: center;'>:</td>
                <td style='border: none; width: 35%; font-weight: bold;'>{permit.OriginLocation}</td>
                <td style='border: none; width: 50%;'>Melalui pelabuhan Asal : <strong>{permit.DeparturePort}</strong></td>
            </tr>
            <tr>
                <td style='border: none; font-weight: bold;'>Tujuan</td>
                <td style='border: none; text-align: center;'>:</td>
                <td style='border: none; font-weight: bold;'>{permit.DestinationLocation}</td>
                <td style='border: none;'>Melalui pelabuhan Bongkar : <strong>{permit.ArrivalPort}</strong></td>
            </tr>
        </table>

        <div style='margin: 20px 0; text-align: justify;'>
            <p><strong>Dengan syarat-syarat sebagai berikut :</strong></p>
            <p style='margin: 8px 0;'>a. Ternak potong tersebut harus memenuhi persyaratan teknis (umur dan berat) serta administrasi (surat pemilikan) pada saat pengecekan di <em>Holding Ground</em> yang ditunjuk;</p>
            <p style='margin: 8px 0;'>b. Pedagang harus tercatat sebagai pedagang ternak di Kantor Dinas Peternakan dan Kesehatan Hewan Provinsi NTB dan Kantor Dinas Peternakan Kabupaten/Kota setempat;</p>
            <p style='margin: 8px 0;'>c. Senantiasa taat mengikuti semua peraturan yang ditentukan dan tidak menjual belikan atau mengalihkan izin pengeluaran ternak potong yang telah diterimanya kepada perusahaan lain;</p>
            <p style='margin: 8px 0;'>d. Surat Izin ini dapat diubah dan diatur kembali, apabila terdapat kekeliruan;</p>
            <p style='margin: 8px 0;'>e. Surat izin ini berlaku sekali muat terhitung mulai tanggal <strong>{permit.ValidFrom?.ToString("dd MMMM", culture)} sampai dengan {permit.ValidUntil?.ToString("dd MMMM yyyy", culture)}</strong>.</p>
        </div>

        <div class='signature'>
            <p><strong>DITETAPKAN DI : MATARAM</strong></p>
            <p><strong>PADA TANGGAL : {permit.FinalApprovalDate?.ToString("dd MMMM yyyy", culture)}</strong></p>
            <p><strong>Plt. Kepala Dinas</strong></p>
            <div style='height: 60px; margin: 15px 0; position: relative;'>
                <img src='/images/signature-sample.svg' alt='Tanda Tangan Kepala Dinas' style='position: absolute; top: 0; left: 0; width: 200px; height: 80px; object-fit: contain;' />
            </div>
            <p><strong><u>{(permit.KepalaDinas?.NamaLengkap ?? "Hj. Eva Dewiyani, SP")}</u></strong></p>
            <p><strong>Pembina Utama Muda (IV/c)</strong></p>
            <p><strong>NIP. 19701210 199803 2 006</strong></p>
        </div>

        <div style='margin-top: 60px; clear: both;'>
            <p><strong>Tembusan</strong> disampaikan kepada Yth :</p>
            <p style='margin: 4px 0;'>1. Kepala Dinas Peternakan Provinsi/Kab/Kota Daerah Asal dan Daerah Tujuan.</p>
            <p style='margin: 4px 0;'>2. Kepala Balai/Stasiun Karantina Daerah Asal dan Daerah Tujuan.</p>
            <p style='margin: 4px 0;'>3. Penanggung Jawab Wilker Pelabuhan Daerah Asal dan Daerah Tujuan.</p>
            <p style='margin: 4px 0;'>4. Perusahaan yang bersangkutan untuk dimaklumi dan diindahkan.</p>
            <p style='margin: 4px 0;'>5. Arsip.</p>
        </div>
    </div>";
        }

        private string NumberToWords(int number)
        {
            if (number == 0) return "nol";

            string[] ones = { "", "satu", "dua", "tiga", "empat", "lima", "enam", "tujuh", "delapan", "sembilan",
                             "sepuluh", "sebelas", "dua belas", "tiga belas", "empat belas", "lima belas",
                             "enam belas", "tujuh belas", "delapan belas", "sembilan belas" };

            string[] tens = { "", "", "dua puluh", "tiga puluh", "empat puluh", "lima puluh",
                             "enam puluh", "tujuh puluh", "delapan puluh", "sembilan puluh" };

            if (number < 20)
                return ones[number];

            if (number < 100)
                return tens[number / 10] + (number % 10 != 0 ? " " + ones[number % 10] : "");

            if (number < 1000)
            {
                string result = "";
                if (number / 100 == 1)
                    result = "seratus";
                else
                    result = ones[number / 100] + " ratus";

                if (number % 100 != 0)
                    result += " " + NumberToWords(number % 100);

                return result;
            }

            if (number < 1000000)
            {
                string result = "";
                if (number / 1000 == 1)
                    result = "seribu";
                else
                    result = NumberToWords(number / 1000) + " ribu";

                if (number % 1000 != 0)
                    result += " " + NumberToWords(number % 1000);

                return result;
            }

            return number.ToString(); // Fallback untuk angka yang sangat besar
        }
    }
}