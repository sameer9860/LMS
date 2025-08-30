using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using System;
using System.Threading.Tasks;

namespace LMS.Helpers
{
    public static class PdfHelper
    {
        public static async Task<string> ExtractTextAsync(string filePath)
        {
            using var reader = new PdfReader(filePath);
            var text = string.Empty;

            for (int page = 1; page <= reader.NumberOfPages; page++)
            {
                text += PdfTextExtractor.GetTextFromPage(reader, page);
            }

            return await Task.FromResult(text);
        }
    }
}