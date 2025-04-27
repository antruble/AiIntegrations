using Backend.Application.Interfaces.Poker;
using Backend.Application.Services.Poker;
using Backend.Domain.IRepositories;
using Backend.Domain.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backend.Infrastructure.DependencyInjection
{
    public static class PokerServiceRegistration
    {
        public static void AddPokerServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<IGameService, GameAppService>();
            services.AddScoped<IBotService, BotAppService>();
            services.AddScoped<IPlayerService, PlayerAppService>();
            services.AddScoped<IHintService, HintAppService>();
            services.AddScoped<IPokerHandEvaluator, PokerHandEvaluator>();
            services.AddSingleton<IHandRankEvaluator, PokerHandEvaluator>();
            services.AddSingleton<IOddsCalculator, MonteCarloOddsCalculator>();
        }
    }
}
