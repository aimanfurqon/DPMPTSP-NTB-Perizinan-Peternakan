using System.Drawing;
using System.Drawing.Imaging;
using System.Text;

namespace PerizinanPeternakan.Services
{
    public interface IBarcodeGeneratorService
    {
        Task<string> GenerateBarcodeAsync(string applicationNumber, string permitId);
        Task<byte[]> GenerateBarcodeImageAsync(string text);
    }

    public class BarcodeGeneratorService : IBarcodeGeneratorService
    {
        public async Task<string> GenerateBarcodeAsync(string applicationNumber, string permitId)
        {
            try
            {
                // Create unique barcode data
                var barcodeData = $"PERMIT-{applicationNumber}-{permitId}-{DateTime.Now:yyyyMMdd}";

                // Generate barcode image
                var barcodeBytes = await GenerateBarcodeImageAsync(barcodeData);

                // Save to file
                var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "documents", "barcodes");
                if (!Directory.Exists(uploadsPath))
                {
                    Directory.CreateDirectory(uploadsPath);
                }

                var fileName = $"barcode_{applicationNumber.Replace("/", "_")}_{DateTime.Now:yyyyMMddHHmmss}.png";
                var filePath = Path.Combine(uploadsPath, fileName);

                await File.WriteAllBytesAsync(filePath, barcodeBytes);

                return $"/documents/barcodes/{fileName}";
            }
            catch (Exception ex)
            {
                throw new Exception($"Error generating barcode: {ex.Message}", ex);
            }
        }

        public async Task<byte[]> GenerateBarcodeImageAsync(string text)
        {
            return await Task.Run(() =>
            {
                // Create a simple barcode image with text
                int width = 300;
                int height = 100;

                using var bitmap = new Bitmap(width, height);
                using var graphics = Graphics.FromImage(bitmap);

                // Fill background
                graphics.FillRectangle(Brushes.White, 0, 0, width, height);

                // Draw border
                graphics.DrawRectangle(Pens.Black, 0, 0, width - 1, height - 1);

                // Draw barcode pattern (simple vertical lines)
                var random = new Random(text.GetHashCode());
                for (int i = 10; i < width - 10; i += 3)
                {
                    int lineHeight = random.Next(20, 60);
                    graphics.DrawLine(Pens.Black, i, 20, i, 20 + lineHeight);
                }

                // Draw text
                using var font = new Font("Arial", 8, FontStyle.Bold);
                var textSize = graphics.MeasureString(text, font);
                var textX = (width - textSize.Width) / 2;
                graphics.DrawString(text, font, Brushes.Black, textX, height - 25);

                using var stream = new MemoryStream();
                bitmap.Save(stream, ImageFormat.Png);
                return stream.ToArray();
            });
        }
    }
}