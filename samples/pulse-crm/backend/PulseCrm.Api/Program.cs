using System.Text.Json.Serialization;
using Kyvo.AspNetCore;
using Kyvo.Client;
using Microsoft.EntityFrameworkCore;
using PulseCrm.Api.Configuration;
using PulseCrm.Api.Data;
using PulseCrm.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<CorsOptions>(builder.Configuration.GetSection(CorsOptions.Section));

var allowInvalidKyvoCertificate = builder.Configuration.GetValue(
    "Kyvo:AllowInvalidKyvoCertificate",
    builder.Environment.IsDevelopment());

builder.Services
    .AddKyvoAuthentication(builder.Configuration, KyvoOptions.SectionName)
    .PostConfigure<KyvoOptions>(o => o.AllowInvalidCertificate = allowInvalidKyvoCertificate);

builder.Services
    .AddKyvoClient(builder.Configuration)
    .PostConfigure<KyvoClientOptions>(o => o.AllowInvalidCertificate = allowInvalidKyvoCertificate);

builder.Services.AddDbContext<PulseCrmDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("PulseCrm")));

builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<PulseCrmTenantScope>();

builder.Services
    .AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var corsOrigins = builder.Configuration.GetSection(CorsOptions.Section).Get<CorsOptions>()?.AllowedOrigins
    ?? ["http://localhost:5173"];

builder.Services.AddCors(options =>
{
    options.AddPolicy("PulseCrmSpa", policy =>
        policy.WithOrigins(corsOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PulseCrmDbContext>();
    db.Database.EnsureCreated();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("PulseCrmSpa");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
