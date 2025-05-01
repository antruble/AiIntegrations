using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backend.Application.Interfaces.DocumentSummary
{
    public interface IDocumentSummaryService
    {
        Task<string> ExtractTextAsync(IFormFile file);
    }
}
