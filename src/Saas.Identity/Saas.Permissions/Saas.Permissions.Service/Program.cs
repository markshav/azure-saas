using Azure.Identity;
using Microsoft.AspNetCore.HttpOverrides;
using Saas.Permissions.Service.Data;
using Saas.Permissions.Service.Interfaces;
using Saas.Permissions.Service.Models.AppSettings;
using Saas.Permissions.Service.Services;
using Saas.Permissions.Service.Utilities;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsProduction())
{
    // Get Secrets From Azure Key Vault if in production. If not in production, secrets are automatically loaded in from the .NET secrets manager
    // https://docs.microsoft.com/en-us/aspnet/core/security/key-vault-configuration?view=aspnetcore-6.0

    // We don't want to fetch all the secrets for the other microservices in the app/solution, so we only fetch the ones with the prefix of "permissions-".
    // https://docs.microsoft.com/en-us/aspnet/core/security/key-vault-configuration?view=aspnetcore-6.0#use-a-key-name-prefix

    builder.Configuration.AddAzureKeyVault(
        new Uri(builder.Configuration["KeyVault:Url"]), 
        new DefaultAzureCredential(), 
        new CustomPrefixKeyVaultSecretManager("permissions"));
}

// Add options using options pattern : https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/options?view=aspnetcore-6.0
builder.Services.Configure<AppSettings>(builder.Configuration.GetSection(nameof(AppSettings)));
builder.Services.Configure<AzureADB2COptions>(builder.Configuration.GetSection("AzureAdB2C"));


// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


builder.Services.AddDbContext<PermissionsContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("PermissionsContext"));
});

builder.Services.AddScoped<IPermissionsService, PermissionsService>();
builder.Services.AddScoped<IGraphAPIService, GraphAPIService>();

var app = builder.Build();
app.ConfigureDatabase();

if (app.Environment.IsDevelopment()) {
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseForwardedHeaders();

// Adds middleware to check for the presence of an API Key
app.UseMiddleware<ApiKeyMiddleware>();

app.MapControllers();

app.Run();
