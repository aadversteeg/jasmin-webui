using Blazing.Mvvm;
using Core.Infrastructure.BlazorApp;
using Core.Infrastructure.BlazorApp.ViewModels;
using Core.Infrastructure.JasminClient;
using Core.Infrastructure.LocalStorage;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Register services
builder.Services.AddJasminClient();
builder.Services.AddLocalStorage();

// Register ViewModels (Blazing.Mvvm)
builder.Services.AddMvvm();
builder.Services.AddScoped<ConfigurationViewModel>();
builder.Services.AddScoped<EventFilterViewModel>();
builder.Services.AddScoped<EventViewerViewModel>();
builder.Services.AddScoped<SidePanelViewModel>();
builder.Services.AddScoped<LeftPanelViewModel>();
builder.Services.AddScoped<McpServerListViewModel>();

await builder.Build().RunAsync();
