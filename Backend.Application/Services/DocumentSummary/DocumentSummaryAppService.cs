using Backend.Application.Interfaces.DocumentSummary;
using Microsoft.AspNetCore.Http;
using System;
using System.Text;
using System.Threading.Tasks;
using UglyToad.PdfPig;
using Spire.Doc;

namespace Backend.Application.Services.DocumentSummary
{
    public class DocumentSummaryAppService : IDocumentSummaryService
    {
        public async Task<string> ExtractTextAsync(IFormFile file)
        {
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (extension == ".txt")
            {
                using (var stream = file.OpenReadStream())
                using (var reader = new StreamReader(stream))
                {
                    return await reader.ReadToEndAsync();
                }
            }
            else if (extension == ".pdf")
            {
                using (var stream = file.OpenReadStream())
                {
                    using (var pdf = PdfDocument.Open(stream))
                    {
                        StringBuilder sb = new StringBuilder();
                        foreach (var page in pdf.GetPages())
                        {
                            sb.AppendLine(page.Text);
                        }
                        return sb.ToString();
                    }
                }
            }
            else if (extension == ".doc" || extension == ".docx")
            {
                using (var stream = file.OpenReadStream())
                {
                    var doc = new Document(stream);
                    return doc.GetText();
                }
            }

            return string.Empty;
        }
    }
}
