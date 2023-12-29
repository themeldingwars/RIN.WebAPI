using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RIN.WebAPI.DB;
using RIN.WebAPI.DB.SDB;
using RIN.WebAPI.Models;
using RIN.WebAPI.Models.Config;
using RIN.WebAPI.Utils;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
using Serilog.Templates;

var loggerExpression = new ExpressionTemplate("{@t:HH:mm:ss} [{@l:u4}] [{Substring(SourceContext, LastIndexOf(SourceContext, '.') + 1)}] {@m}\n{@x}");
Log.Logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.File(loggerExpression, "./logs/log-.txt", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 5)
            .WriteTo.File(new CompactJsonFormatter(), "./logs/log-.json", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 5)
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, services, cfg) =>
{
    cfg
       .ReadFrom.Configuration(ctx.Configuration)
       .ReadFrom.Services(services).Enrich.FromLogContext()
       .WriteTo.Console()
       .WriteTo.File(loggerExpression, "./logs/log-.txt",
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 5)
       .WriteTo.File(new CompactJsonFormatter(), "./logs/log-.json", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 5)
       .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning);
});

builder.Services.AddControllers().AddJsonOptions(options =>
{
    //options.JsonSerializerOptions.IgnoreNullValues       = true;
    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

// Config sections
builder.Services.Configure<WebApiConfigSettings>(builder.Configuration.GetSection(WebApiConfigSettings.NAME));
builder.Services.Configure<DevServerSettings>(builder.Configuration.GetSection(DevServerSettings.NAME));
builder.Services.Configure<ServerDefaultsSettings>(builder.Configuration.GetSection(ServerDefaultsSettings.NAME));
builder.Services.Configure<DbConnectionSettings>(builder.Configuration.GetSection(DbConnectionSettings.NAME));

builder.Services.AddDistributedMemoryCache();

// Add services to the container.
builder.Services.AddSingleton<DB>();
builder.Services.AddSingleton<SDB>();
builder.Services.AddSingleton<SessionManager>();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseTmwExceptionHandler();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) {
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapFallbackToController("CatchAll", "Operator");
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.UseSerilogRequestLogging();

app.MapControllers();

app.Run();