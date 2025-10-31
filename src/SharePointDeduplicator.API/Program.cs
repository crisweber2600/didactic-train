using Azure.Identity;
using Microsoft.Graph;
using SharePointDeduplicator.API.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// Add CORS for Blazor frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazorWasm",
        policy => policy
            .WithOrigins("https://localhost:7001", "http://localhost:5001")
            .AllowAnyMethod()
            .AllowAnyHeader());
});

// Configure Microsoft Graph client
builder.Services.AddSingleton<GraphServiceClient>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    
    // Read configuration from appsettings.json or environment variables
    var tenantId = configuration["AzureAd:TenantId"];
    var clientId = configuration["AzureAd:ClientId"];
    var clientSecret = configuration["AzureAd:ClientSecret"];

    if (string.IsNullOrEmpty(tenantId) || string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
    {
        throw new InvalidOperationException("Azure AD configuration is missing. Please configure AzureAd:TenantId, AzureAd:ClientId, and AzureAd:ClientSecret.");
    }

    var clientSecretCredential = new ClientSecretCredential(tenantId, clientId, clientSecret);
    var scopes = new[] { "https://graph.microsoft.com/.default" };

    return new GraphServiceClient(clientSecretCredential, scopes);
});

// Register services
builder.Services.AddScoped<ISharePointScannerService, SharePointScannerService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors("AllowBlazorWasm");
app.UseAuthorization();
app.MapControllers();

app.Run();
