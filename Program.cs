using CoreHRAPI.Data;
using CoreHRAPI.Models.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using DotNetEnv;
using Dapper;
using Npgsql;
using System.Data;
using CoreHRAPI.Handlers;

var builder = WebApplication.CreateBuilder(args);

// Load .env file
DotNetEnv.Env.Load();

// Add configuration sources
builder.Configuration
    .SetBasePath(builder.Environment.ContentRootPath)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddEnvironmentVariables();

// Add HttpClient services
builder.Services.AddHttpClient();                      // Generic HttpClient
builder.Services.AddHttpClient<GeminiHelper>();        // Register GeminiHelper with DI

// Add services to the container.
builder.Services.AddControllers();

// Configure PostgreSQL connection string
var connectionString = $"Host={Environment.GetEnvironmentVariable("DB_HOST")};" +
                      $"Database={Environment.GetEnvironmentVariable("DB_NAME")};" +
                      $"Username={Environment.GetEnvironmentVariable("DB_USER")};" +
                      $"Password={Environment.GetEnvironmentVariable("DB_PASSWORD")};" +
                      $"Maximum Pool Size=100;" +
                      $"Timeout=30;" +
                      $"Command Timeout=30;";

// Configure Dapper with PostgreSQL
SqlMapper.AddTypeHandler(new PostgresTypeHandlers.JsonTypeHandler());
SqlMapper.RemoveTypeMap(typeof(DateTime));
SqlMapper.AddTypeHandler(new PostgresTypeHandlers.DateTimeHandler());
SqlMapper.AddTypeHandler(new PostgresTypeHandlers.ArrayTypeHandler<int>());
SqlMapper.AddTypeHandler(new PostgresTypeHandlers.ArrayTypeHandler<string>());

// Register the DatabaseContext with the environment-based connection string
builder.Services.AddScoped<DatabaseContext>(provider =>
    new DatabaseContext(connectionString));

// Register repositories
builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<CIRSMasterListRepository>();
builder.Services.AddScoped<UserCredentialsRepository>();
builder.Services.AddScoped<GlobalRepository>();
builder.Services.AddScoped<EmployeeRepository>();
builder.Services.AddScoped<UAMRepository>();
builder.Services.AddScoped<RequestsHandler>();

// Register SMTP email service
var smtpSettings = new SmtpSettings
{
    Server = Environment.GetEnvironmentVariable("SMTP_SERVER"),
    Port = int.Parse(Environment.GetEnvironmentVariable("SMTP_PORT") ?? "587"),
    User = Environment.GetEnvironmentVariable("SMTP_USER"),
    Pass = Environment.GetEnvironmentVariable("SMTP_PASS")
};
builder.Services.AddSingleton(new EmailService(
    smtpSettings.Server,
    smtpSettings.Port,
    smtpSettings.User,
    smtpSettings.Pass
));

// Add session services
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.None;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

// Add Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// JWT Authentication with environment variables
var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY");
var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER");
var jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhost3000",
        builder => builder
            .WithOrigins(Environment.GetEnvironmentVariable("CORS_ORIGINS")?.Split(",") ?? new[] { "http://localhost:3000" })
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials());
});

// Build and configure app
var app = builder.Build();

app.UseCors("AllowLocalhost3000");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
