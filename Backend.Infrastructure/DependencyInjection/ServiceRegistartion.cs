using Backend.Application.Interfaces;
using Backend.Domain.IRepositories;
using Backend.Infrastructure.Data;
using Backend.Infrastructure.Repositories;
using Backend.Infrastructure.Services.DocumentSummary;
using Backend.Infrastructure.Services.Poker;
using Backend.Infrastructure.Services.Recipes;
using Backend.Shared.Models;
using Backend.Shared.Models.DocumentSummary;
using Backend.Shared.Models.Poker;
using Backend.Shared.Models.Recipes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backend.Infrastructure.DependencyInjection
{
    public static class ServiceRegistration
    {
        public static void AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

            services.AddScoped<IUnitOfWork, UnitOfWork>();

            services
                .AddScoped<IOpenAiClient<RecipeRequestDto, RecipesResponseDto>, OpenAiRecipesClient>()
                .AddScoped<IOpenAiClient<DocumentSummaryRequest, DocumentSummaryResponse>, OpenAiSummaryClient>()
                .AddScoped<IOpenAiClient<HintRequest, HintResponse>, OpenAiHintClient>();

            services.AddPokerServices(configuration);
            services.AddDocumentSummaryServices(configuration);
            services.AddRecipesServices(configuration);
        }
    }
}
