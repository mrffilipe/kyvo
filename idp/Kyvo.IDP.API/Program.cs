using Kyvo.IDP.API.Components;
using Kyvo.IDP.Infrastructure.Extensions;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console());

    builder.Services
        .AddControllers();

    builder.Services.AddHttpContextAccessor();
    builder.Services.AddRazorComponents();
    builder.Services.AddAntiforgery();

    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddKyvoIdentity();
    builder.Services.AddKyvoOpenIddictServer(builder.Configuration);
    builder.Services.AddKyvoOpenIddictClient(builder.Configuration);

    builder.Services.AddAuthorization();
    builder.Services.AddHealthChecks()
        .AddDbContextCheck<Kyvo.IDP.Infrastructure.Persistence.ApplicationDbContext>("database");

    var app = builder.Build();

    app.UseSerilogRequestLogging();
    app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseAntiforgery();

    app.MapHealthChecks("/health");
    app.MapControllers();
    app.MapRazorComponents<App>();

    await app.Services.InitializeDatabaseAsync();

    Log.Information("Kyvo IDP starting on {Urls}", string.Join(", ", app.Urls));
    await app.RunAsync();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Kyvo IDP terminated unexpectedly");
    throw;
}
finally
{
    await Log.CloseAndFlushAsync();
}
