using Backend.Application.Interfaces.DocumentSummary;
using Backend.Application.Interfaces;
using Backend.Application.Services.DocumentSummary;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenAI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backend.Infrastructure.DependencyInjection
{
    public static class DocumentSummaryServiceRegistration
    {
        public static void AddDocumentSummaryServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<IDocumentSummaryService, DocumentSummaryAppService>();

        }
    }
}
