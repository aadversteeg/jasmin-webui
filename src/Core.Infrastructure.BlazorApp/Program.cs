using Blazing.Mvvm;
using Core.Application.Storage;
using Core.Infrastructure.BlazorApp;
using Core.Infrastructure.BlazorApp.Services;
using Core.Infrastructure.BlazorApp.ViewModels;
using Core.Infrastructure.JasminClient;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Register services
builder.Services.AddJasminClient();
builder.Services.AddScoped<ILocalStorageService, LocalStorageService>();

// Register ViewModels (Blazing.Mvvm)
builder.Services.AddMvvm();
builder.Services.AddScoped<ConfigurationViewModel>();
builder.Services.AddScoped<EventFilterViewModel>();
builder.Services.AddScoped<EventViewerViewModel>();
builder.Services.AddScoped<SidePanelViewModel>();

await builder.Build().RunAsync();
