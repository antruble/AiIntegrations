using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backend.Shared.Models.DocumentSummary
{

    public record DocumentSummaryApiResult(
        string ShortSummary,
        string DetailedSummary
    );
}
