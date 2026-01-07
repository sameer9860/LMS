using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using System.IO;
using System.Threading.Tasks;

namespace LMS.Helpers
{
    public static class PdfHelper
    {
        public static async Task<string> ExtractTextAsync(string filePath)
        {
            using var pdfReader = new PdfReader(filePath);
            using var pdfDoc = new PdfDocument(pdfReader);

            var text = string.Empty;

            for (int page = 1; page <= pdfDoc.GetNumberOfPages(); page++)
            {
                text += PdfTextExtractor.GetTextFromPage(pdfDoc.GetPage(page));
            }

            return await Task.FromResult(text);
        }
    }
}
