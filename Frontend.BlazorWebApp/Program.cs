using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Frontend.BlazorWebApp.Components;
using Serilog.Events;
using Serilog;
using Frontend.BlazorWebApp.StateServices;

namespace Frontend.BlazorWebApp;

public class Program
{
    private static string BaseUrl = "https://localhost:7292";
    public static void Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Console() // konzolra is írunk
            .WriteTo.File(
                path: "Logs/log-.txt",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                restrictedToMinimumLevel: LogEventLevel.Information)
            .CreateLogger();

        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();

        builder.Host.UseSerilog();

        builder.Services.AddSingleton<GameStateService>();
        builder.Services.AddHttpClient("PokerClient", client =>
        {
            client.BaseAddress = new Uri($"{BaseUrl}/api/poker/");
        });
        builder.Services.AddHttpClient("DocumentSummaryClient", client =>
        {
            client.BaseAddress = new Uri($"{BaseUrl}/api/documentsummary/");
        });
        builder.Services.AddHttpClient("RecipesClient", client =>
        {
            client.BaseAddress = new Uri($"{BaseUrl}/api/recipes/");
        });

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseMigrationsEndPoint();
        }
        else
        {
            app.UseExceptionHandler("/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseHttpsRedirection();

        app.UseAntiforgery();

        app.MapStaticAssets();
        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();


        app.Run();
    }
}
