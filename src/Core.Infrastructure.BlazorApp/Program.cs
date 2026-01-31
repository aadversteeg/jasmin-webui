using Core.Application.Storage;
using Core.Infrastructure.BlazorApp;
using Core.Infrastructure.BlazorApp.Services;
using Core.Infrastructure.JasminClient;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Register services
builder.Services.AddJasminClient();
builder.Services.AddScoped<ILocalStorageService, LocalStorageService>();
builder.Services.AddScoped<EventFilterState>();
builder.Services.AddScoped<EventViewerState>();

await builder.Build().RunAsync();
