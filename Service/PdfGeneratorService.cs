using PerizinanPeternakan.Models;
using System.Text;
using System.Globalization;

namespace PerizinanPeternakan.Services
{
    public interface IPdfGeneratorService
    {
        Task<byte[]> GeneratePermitPdf(LivestockPermitApplication permit);
    }

    public class PdfGeneratorService : IPdfGeneratorService
    {
        public async Task<byte[]> GeneratePermitPdf(LivestockPermitApplication permit)
        {
            // Untuk sementara, kita buat PDF sederhana menggunakan HTML to PDF
            // Nantinya bisa diganti dengan Syncfusion atau library lain

            var html = GeneratePermitHtml(permit);

            // Menggunakan library sederhana untuk convert HTML ke PDF
            // Anda bisa install SelectPdf atau DinkToPdf
            // Untuk demo ini, kita return HTML sebagai PDF (simplified)

            return Encoding.UTF8.GetBytes(html);
        }

        private string GeneratePermitHtml(LivestockPermitApplication permit)
        {
            var culture = new CultureInfo("id-ID");
            var livestockDetails = string.Join("\n", permit.LivestockDetails.Select((detail, index) =>
                $"<tr><td>{index + 1}.</td><td>{detail.LivestockType}</td><td>{detail.Quantity} ({NumberToWords(detail.Quantity)}) ekor</td><td>{detail.Description ?? ""}</td></tr>"));

            var html = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <title>Surat Izin Pengeluaran Ternak Potong</title>
    <style>
        @page {{
            size: A4;
            margin: 2cm;
        }}
        body {{ 
            font-family: 'Times New Roman', serif; 
            font-size: 12pt; 
            line-height: 1.4; 
            margin: 0;
            padding: 0;
            color: #000;
        }}
        .header {{ 
            text-align: center; 
            margin-bottom: 30px; 
            border-bottom: 3px solid #000;
            padding-bottom: 15px;
        }}
        .header img {{ 
            width: 80px; 
            height: 80px; 
            margin-bottom: 10px;
        }}
        .header h1 {{ 
            font-size: 16pt; 
            font-weight: bold; 
            margin: 8px 0; 
            line-height: 1.2;
        }}
        .header h2 {{ 
            font-size: 14pt; 
            font-weight: bold; 
            margin: 4px 0; 
        }}
        .header p {{ 
            font-size: 11pt; 
            margin: 4px 0; 
            font-weight: bold;
        }}
        .title {{ 
            text-align: center; 
            font-weight: bold; 
            margin: 25px 0; 
            line-height: 1.3;
        }}
        .title p {{
            margin: 5px 0;
            font-size: 14pt;
        }}
        .content {{ 
            text-align: justify; 
            line-height: 1.5;
        }}
        .section-table {{ 
            width: 100%; 
            margin: 15px 0; 
            border-collapse: collapse;
        }}
        .section-table td {{ 
            padding: 8px 5px; 
            vertical-align: top; 
            border: none;
        }}
        .section-label {{
            width: 18%;
            font-weight: bold;
        }}
        .section-colon {{
            width: 3%;
            text-align: center;
        }}
        .section-content {{
            width: 79%;
            text-align: justify;
        }}
        .livestock-table {{ 
            width: 100%; 
            border-collapse: collapse; 
            margin: 20px 0; 
            border: 2px solid #000;
        }}
        .livestock-table th, .livestock-table td {{ 
            border: 1px solid #000; 
            padding: 8px; 
            text-align: center;
        }}
        .livestock-table th {{ 
            background-color: #f0f0f0; 
            font-weight: bold; 
        }}
        .livestock-table td:first-child {{
            width: 8%;
        }}
        .livestock-table td:nth-child(2) {{
            width: 35%;
        }}
        .livestock-table td:nth-child(3) {{
            width: 35%;
        }}
        .livestock-table td:last-child {{
            width: 22%;
        }}
        .location-table {{
            width: 100%;
            margin: 15px 0;
            border-collapse: collapse;
        }}
        .location-table td {{
            padding: 5px;
            border: none;
            vertical-align: top;
        }}
        .conditions {{
            margin: 20px 0;
            text-align: justify;
        }}
        .conditions p {{
            margin: 8px 0;
            text-indent: 0;
        }}
        .signature {{ 
            margin-top: 40px; 
            page-break-inside: avoid;
        }}
        .signature-right {{ 
            float: right; 
            text-align: center; 
            width: 280px; 
            margin-top: 20px;
        }}
        .signature-space {{
            height: 60px;
            margin: 15px 0;
        }}
        .clear {{ 
            clear: both; 
        }}
        .tembusan {{
            margin-top: 60px;
            clear: both;
            page-break-inside: avoid;
        }}
        .tembusan p {{
            margin: 4px 0;
        }}
        .company-info {{
            margin: 15px 0;
            font-weight: bold;
        }}
        .permit-header {{
            text-align: center;
            font-weight: bold;
            font-size: 13pt;
            margin: 20px 0;
        }}
    </style>
</head>
<body>
    <div class=""header"">
        <img src=""data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg=="" alt=""Logo NTB"">
        <h1>PEMERINTAH PROVINSI NUSA TENGGARA BARAT</h1>
        <h2>DINAS PENANAMAN MODAL</h2>
        <h2>DAN PELAYANAN TERPADU SATU PINTU</h2>
        <p>Jalan Udayana No. 4 Mataram Telpon (0370) 631060 Fax (0370) 647022</p>
    </div>

    <div class=""title"">
        <p><strong><u>SURAT IZIN</u></strong></p>
        <p><strong><u>PENGELUARAN TERNAK POTONG UNTUK KEPERLUAN PERDAGANGAN</u></strong></p>
        <p><strong><u>ANTAR PULAU/ANTAR PROVINSI</u></strong></p>
        <p><strong><u>NOMOR : {permit.ApplicationNumber}</u></strong></p>
    </div>

    <div class=""content"">
        <div class=""permit-header"">
            <p><strong>KEPALA DINAS PENANAMAN MODAL DAN PELAYANAN TERPADU SATU PINTU PROVINSI NUSA TENGGARA BARAT</strong></p>
        </div>

        <table class=""section-table"">
            <tr>
                <td class=""section-label"">Memperhatikan</td>
                <td class=""section-colon"">:</td>
                <td class=""section-content"">
                    a. Surat Permohonan Izin dari <strong>{permit.CompanyName}</strong> Tanggal {permit.SubmissionDate.ToString("dd MMMM yyyy", culture)} yang disertai dengan kelengkapan dokumen yang dipersyaratkan;<br><br>
                    <strong>b. Surat Rekomendasi dari Dinas Peternakan dan Kesehatan Hewan Provinsi NTB Nomor : 500.7.2.1/1337/2025 tanggal {DateTime.Now.ToString("dd MMMM yyyy", culture)}.</strong>
                </td>
            </tr>
            <tr>
                <td class=""section-label"">Menimbang</td>
                <td class=""section-colon"">:</td>
                <td class=""section-content"">
                    Bahwa guna menjaga keberlanjutan budidaya peternakan di Provinsi Nusa Tenggara Barat perlu dikendalikan populasinya melalui pemberian Izin Penjatahan Ternak Potong yang dapat dikeluarkan dari Provinsi Nusa Tenggara Barat dari tiap-tiap Kabupaten/Kota.
                </td>
            </tr>
            <tr>
                <td class=""section-label"">Mengingat</td>
                <td class=""section-colon"">:</td>
                <td class=""section-content"">
                    a. Undang-Undang Nomor 18 Tahun 2009 tentang Peternakan dan Kesehatan Hewan;<br>
                    b. Peraturan Pemerintah No. 15 Tahun 1977 tentang Penolakan, Pencegahan, Pemberantasan dan Pengobatan Penyakit Hewan;<br>
                    c. Peraturan Presiden Republik Indonesia Nomor 91 Tahun 2017 tentang Percepatan Pelaksanaan Berusaha;<br>
                    d. Peraturan Daerah Provinsi Nusa Tenggara Barat Nomor 4 Tahun 2020 tentang Tata Niaga Ternak;<br>
                    e. Peraturan Gubernur Nusa Tenggara Barat Nomor 38 Tahun 2020 tentang Penyelenggaraan Pelayanan Terpadu Satu Pintu;<br>
                    <strong>f. Keputusan Gubernur Nusa Tenggara Barat Nomor 500.7-841 Tahun 2025 Tanggal 30 Desember 2024 tentang Kuota Pengeluaran Sapi dan Kerbau Potong Dalam dan Keluar Daerah serta Pemasukan Sapi Eksotik di Provinsi Nusa Tenggara Barat Tahun 2025.</strong>
                </td>
            </tr>
        </table>

        <div class=""company-info"">
            <p><strong>MEMBERIKAN IZIN PENGELUARAN</strong></p>
            <p>Kepada : <strong>{permit.CompanyName}</strong></p>
            <p>Alamat : {permit.CompanyAddress}</p>
        </div>

        <table class=""livestock-table"">
            <thead>
                <tr>
                    <th><strong>No.</strong></th>
                    <th><strong>JENIS TERNAK POTONG</strong></th>
                    <th><strong>BANYAKNYA</strong></th>
                    <th><strong>KETERANGAN</strong></th>
                </tr>
            </thead>
            <tbody>
                {livestockDetails}
            </tbody>
        </table>

        <table class=""location-table"">
            <tr>
                <td style=""width: 10%; font-weight: bold;"">Asal</td>
                <td style=""width: 5%; text-align: center;"">:</td>
                <td style=""width: 35%; font-weight: bold;"">{permit.OriginLocation}</td>
                <td style=""width: 50%;"">Melalui pelabuhan Asal : <strong>{permit.DeparturePort}</strong></td>
            </tr>
            <tr>
                <td style=""font-weight: bold;"">Tujuan</td>
                <td style=""text-align: center;"">:</td>
                <td style=""font-weight: bold;"">{permit.DestinationLocation}</td>
                <td>Melalui pelabuhan Bongkar : <strong>{permit.ArrivalPort}</strong></td>
            </tr>
        </table>

        <div class=""conditions"">
            <p><strong>Dengan syarat-syarat sebagai berikut :</strong></p>
            <p>a. Ternak potong tersebut harus memenuhi persyaratan teknis (umur dan berat) serta administrasi (surat pemilikan) pada saat pengecekan di <em>Holding Ground</em> yang ditunjuk;</p>
            <p>b. Pedagang harus tercatat sebagai pedagang ternak di Kantor Dinas Peternakan dan Kesehatan Hewan Provinsi NTB dan Kantor Dinas Peternakan Kabupaten/Kota setempat;</p>
            <p>c. Senantiasa taat mengikuti semua peraturan yang ditentukan dan tidak menjual belikan atau mengalihkan izin pengeluaran ternak potong yang telah diterimanya kepada perusahaan lain;</p>
            <p>d. Surat Izin ini dapat diubah dan diatur kembali, apabila terdapat kekeliruan;</p>
            <p>e. Surat izin ini berlaku sekali muat terhitung mulai tanggal <strong>{permit.ValidFrom?.ToString("dd MMMM", culture)} sampai dengan {permit.ValidUntil?.ToString("dd MMMM yyyy", culture)}</strong>.</p>
        </div>

        <div class=""signature"">
            <div class=""signature-right"">
                <p><strong>DITETAPKAN DI : MATARAM</strong></p>
                <p><strong>PADA TANGGAL : {permit.FinalApprovalDate?.ToString("dd MMMM yyyy", culture)}</strong></p>
                <p><strong>Plt. Kepala Dinas</strong></p>
                <div class=""signature-space""></div>
                <p><strong><u>{(permit.KepalaDinas?.NamaLengkap ?? "Hj. Eva Dewiyani, SP")}</u></strong></p>
                <p><strong>Pembina Utama Muda (IV/c)</strong></p>
                <p><strong>NIP. 19701210 199803 2 006</strong></p>
            </div>
            <div class=""clear""></div>
        </div>

        <div class=""tembusan"">
            <p><strong>Tembusan</strong> disampaikan kepada Yth :</p>
            <p>1. Kepala Dinas Peternakan Provinsi/Kab/Kota Daerah Asal dan Daerah Tujuan.</p>
            <p>2. Kepala Balai/Stasiun Karantina Daerah Asal dan Daerah Tujuan.</p>
            <p>3. Penanggung Jawab Wilker Pelabuhan Daerah Asal dan Daerah Tujuan.</p>
            <p>4. Perusahaan yang bersangkutan untuk dimaklumi dan diindahkan.</p>
            <p>5. Arsip.</p>
        </div>
    </div>
</body>
</html>";

            return html;
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

    // Alternative implementation using DinkToPdf (if you want to install it)
    // Uncomment and install DinkToPdf package if you want actual PDF generation
    /*
    public class DinkToPdfService : IPdfGeneratorService
    {
        public async Task<byte[]> GeneratePermitPdf(LivestockPermitApplication permit)
        {
            var converter = new BasicConverter(new PdfTools());
            
            var html = GeneratePermitHtml(permit);
            
            var doc = new HtmlToPdfDocument()
            {
                GlobalSettings = {
                    ColorMode = ColorMode.Color,
                    Orientation = Orientation.Portrait,
                    PaperSize = PaperKind.A4,
                    Margins = new MarginSettings { Top = 20, Bottom = 20, Left = 20, Right = 20 }
                },
                Objects = {
                    new ObjectSettings() {
                        PagesCount = true,
                        HtmlContent = html,
                        WebSettings = { DefaultEncoding = "utf-8" }
                    }
                }
            };

            return converter.Convert(doc);
        }
        
        private string GeneratePermitHtml(LivestockPermitApplication permit)
        {
            // Same HTML generation logic as above
            var pdfService = new PdfGeneratorService();
            return pdfService.GeneratePermitHtml(permit);
        }
    }
    */
}