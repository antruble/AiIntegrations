using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backend.Shared.Models.DocumentSummary
{
    public class DocumentSummaryRequest
    {
        public string Text { get; init; }
        public string Style { get; init; }
    }

    public class DocumentSummaryResponse
    {
        public string ShortSummary { get; set; }
        public string DetailedSummary { get; set; }
    }
    public record DocumentSummaryApiResult(
        string ShortSummary,
        string DetailedSummary
    );
}
