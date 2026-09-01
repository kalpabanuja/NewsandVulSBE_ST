using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NotesAndFileBackend.Api.Services;
using NotesAndFileBackend.Api.Filters;
using NotesAndFileBackend.Application.Interfaces;
using NotesAndFileBackend.Infrastructure.Data;
using NotesAndFileBackend.Infrastructure.Services;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using NotesAndFileBackend.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel Size Limits (25MB max for imports)
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.MaxRequestBodySize = 25 * 1024 * 1024; // 25 MB
});

// Add services to the container.
// Memory Cache for Idempotency
builder.Services.AddMemoryCache();
builder.Services.AddScoped<IdempotencyFilterAttribute>();

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
});

// Add FluentValidation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// Add Standardized RFC 7807 ProblemDetails
builder.Services.AddProblemDetails();

// Add HealthChecks
builder.Services.AddHealthChecks()
    .AddCheck("live", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy())
    .AddDbContextCheck<AppDbContext>("ready");

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("StrictPolicy", policy =>
    {
        policy.WithOrigins("https://your-domain.example") // Adjust for production
              .AllowAnyHeader()
              .WithMethods("GET", "POST", "PUT", "DELETE");
    });
});

// Add Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("GlobalPolicy", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.PermitLimit = 100;
        opt.QueueLimit = 2;
    });
    
    options.AddFixedWindowLimiter("StrictPolicy", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.PermitLimit = 10;
        opt.QueueLimit = 0;
    });
});

builder.Services.AddOpenApi();

// Configure Forwarded Headers for Nginx reverse proxy (so Request.Scheme correctly shows 'https')
builder.Services.Configure<Microsoft.AspNetCore.Builder.ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto;
    // Trust all proxies since this runs inside a Docker network
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// Register DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register Services
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IStorageService, S3StorageService>();
builder.Services.AddScoped<NotesAndFileBackend.Application.Services.IInteractiveToolService, NotesAndFileBackend.Infrastructure.Services.InteractiveToolService>();
builder.Services.AddScoped<NotesAndFileBackend.Api.Services.IImportExportService, NotesAndFileBackend.Api.Services.ImportExportService>();

// Register Background Services
builder.Services.AddHostedService<ExpirationCleanupService>();

// Configure JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["JwtSettings:Issuer"] ?? "NotesAndFileBackend",
            ValidAudience = builder.Configuration["JwtSettings:Audience"] ?? "NotesAndFileBackend.Users",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:Secret"] ?? "fallback_secret_key_that_is_at_least_32_bytes_long_12345!"))
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/api/v1/note-attachments"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseForwardedHeaders();

// Add Correlation ID first so it covers all other middleware
app.UseMiddleware<CorrelationIdMiddleware>();

app.UseExceptionHandler(); // Maps exceptions to ProblemDetails

app.UseMiddleware<SecurityHeadersMiddleware>();

// Validate minimum client version early in the pipeline
app.UseMiddleware<ClientVersionValidationMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseCors("StrictPolicy");

app.UseRateLimiter();

// Use authentication and authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers().RequireRateLimiting("GlobalPolicy");

// Map Health Checks
app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");


// --- SIMPLE UI DASHBOARD ---
app.MapGet("/", () => 
{
    var html = @"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Admin Dashboard | Notes & File Backend</title>
    <style>
        :root {
            --bg-color: #0f172a;
            --glass-bg: rgba(30, 41, 59, 0.7);
            --glass-border: rgba(255, 255, 255, 0.1);
            --primary: #3b82f6;
            --primary-hover: #2563eb;
            --text-main: #f8fafc;
            --text-muted: #94a3b8;
            --success: #10b981;
            --danger: #ef4444;
        }
        * { box-sizing: border-box; margin: 0; padding: 0; font-family: 'Inter', system-ui, sans-serif; }
        body {
            background-color: var(--bg-color);
            background-image: radial-gradient(at 0% 0%, rgba(59, 130, 246, 0.15) 0px, transparent 50%),
                              radial-gradient(at 100% 100%, rgba(16, 185, 129, 0.15) 0px, transparent 50%);
            background-attachment: fixed;
            color: var(--text-main);
            min-height: 100vh;
            display: flex;
            flex-direction: column;
        }
        #login-container { display: flex; justify-content: center; align-items: center; flex-grow: 1; height: 100vh; }
        .glass-card {
            background: var(--glass-bg);
            backdrop-filter: blur(12px);
            border: 1px solid var(--glass-border);
            border-radius: 16px;
            padding: 2.5rem;
            box-shadow: 0 25px 50px -12px rgba(0, 0, 0, 0.5);
        }
        .login-card { width: 100%; max-width: 400px; }
        h2 { margin-bottom: 1.5rem; font-weight: 600; text-align: center; }
        .input-group { margin-bottom: 1.25rem; }
        .input-group label { display: block; margin-bottom: 0.5rem; font-size: 0.875rem; color: var(--text-muted); }
        .input-group input {
            width: 100%; padding: 0.75rem 1rem; background: rgba(15, 23, 42, 0.6);
            border: 1px solid var(--glass-border); border-radius: 8px; color: var(--text-main); font-size: 1rem;
        }
        button {
            width: 100%; padding: 0.75rem; background: var(--primary); color: white;
            border: none; border-radius: 8px; font-size: 1rem; font-weight: 600; cursor: pointer;
        }
        button:hover { background: var(--primary-hover); }
        .error-msg { color: var(--danger); font-size: 0.875rem; margin-top: 1rem; text-align: center; display: none; }
        #dashboard-container { display: none; padding: 2rem; max-width: 1200px; margin: 0 auto; width: 100%; }
        header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 2rem; padding-bottom: 1rem; border-bottom: 1px solid var(--glass-border); }
        .logout-btn { background: transparent; border: 1px solid var(--glass-border); width: auto; padding: 0.5rem 1rem; }
        .grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(250px, 1fr)); gap: 1.5rem; }
        .metric-card { display: flex; flex-direction: column; gap: 0.5rem; }
        .metric-title { color: var(--text-muted); font-size: 0.875rem; text-transform: uppercase; letter-spacing: 0.05em; }
        .metric-value { font-size: 2.5rem; font-weight: 700; color: var(--primary); }
        .loading { text-align: center; padding: 2rem; color: var(--text-muted); font-style: italic; }
    </style>
</head>
<body>
    <div id=""login-container"">
        <div class=""glass-card login-card"">
            <h2>Diagnostic Dashboard Login</h2>
            <div class=""input-group"">
                <label for=""email"">Email Address</label>
                <input type=""email"" id=""email"" placeholder=""admin@notesandfile.local"" value=""admin@notesandfile.local"">
            </div>
            <div class=""input-group"">
                <label for=""password"">Password</label>
                <input type=""password"" id=""password"" placeholder=""Enter generated password"">
            </div>
            <button id=""login-btn"">Sign In</button>
            <div class=""error-msg"" id=""login-error"">Invalid credentials</div>
        </div>
    </div>
    <div id=""dashboard-container"">
        <header>
            <h2>Diagnostic Overview</h2>
            <button class=""logout-btn"" id=""logout-btn"">Sign Out</button>
        </header>
        <div id=""metrics-loading"" class=""loading"">Loading metrics...</div>
        <div class=""grid"" id=""metrics-grid"" style=""display: none;"">
            <div class=""glass-card metric-card""><div class=""metric-title"">Total Users</div><div class=""metric-value"" id=""val-users"">0</div></div>
            <div class=""glass-card metric-card""><div class=""metric-title"">Active Files</div><div class=""metric-value"" id=""val-files"">0</div></div>
            <div class=""glass-card metric-card""><div class=""metric-title"">Storage Used</div><div class=""metric-value"" id=""val-storage"">0 MB</div></div>
            <div class=""glass-card metric-card""><div class=""metric-title"">Total Documents</div><div class=""metric-value"" id=""val-docs"">0</div></div>
        </div>
    </div>
    <script>
        const loginContainer = document.getElementById('login-container');
        const dashboardContainer = document.getElementById('dashboard-container');
        const loginBtn = document.getElementById('login-btn');
        const logoutBtn = document.getElementById('logout-btn');
        const errorMsg = document.getElementById('login-error');

        if (localStorage.getItem('admin_token')) showDashboard();

        loginBtn.addEventListener('click', async () => {
            loginBtn.innerText = 'Signing in...'; errorMsg.style.display = 'none';
            try {
                const response = await fetch('/api/v1/auth/sign-in', {
                    method: 'POST', headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ email: document.getElementById('email').value, password: document.getElementById('password').value, deviceName: 'DiagnosticUI', platform: 'Web' })
                });
                if (!response.ok) throw new Error('Login failed');
                const data = await response.json();
                localStorage.setItem('admin_token', data.accessToken);
                showDashboard();
            } catch (err) { errorMsg.style.display = 'block'; } finally { loginBtn.innerText = 'Sign In'; }
        });

        logoutBtn.addEventListener('click', () => {
            localStorage.removeItem('admin_token'); dashboardContainer.style.display = 'none'; loginContainer.style.display = 'flex';
        });

        async function showDashboard() {
            loginContainer.style.display = 'none'; dashboardContainer.style.display = 'block';
            const grid = document.getElementById('metrics-grid');
            const loading = document.getElementById('metrics-loading');
            grid.style.display = 'none'; loading.style.display = 'block';
            try {
                const response = await fetch('/api/v1/admin/metrics', { headers: { 'Authorization': `Bearer ${localStorage.getItem('admin_token')}` } });
                if (response.status === 401 || response.status === 403) { logoutBtn.click(); return; }
                const data = await response.json();
                document.getElementById('val-users').innerText = data.totalUsers;
                document.getElementById('val-files').innerText = data.totalFiles;
                document.getElementById('val-storage').innerText = data.totalStorageUsed + ' Bytes';
                document.getElementById('val-docs').innerText = data.totalDocuments;
                loading.style.display = 'none'; grid.style.display = 'grid';
            } catch (err) { loading.innerText = 'Failed to load metrics.'; }
        }
    </script>
</body>
</html>";
    return Results.Content(html, "text/html");
});

// Run migrations and seeder before handling requests.
// AdminSeeder.SeedAsync manages its own scope and calls MigrateAsync internally.
var startupLogger = app.Services.GetRequiredService<ILogger<Program>>();
await AdminSeeder.SeedAsync(app.Services, startupLogger);

// If RESET_ADMIN_PASSWORD=true is set, reset the admin password, log it, then exit.
// This lets you recover a lost admin password without touching the database directly.
// Usage: set the env var, restart the container, read logs for the new password, then remove the env var.
if (string.Equals(Environment.GetEnvironmentVariable("RESET_ADMIN_PASSWORD"), "true", StringComparison.OrdinalIgnoreCase))
{
    startupLogger.LogWarning("RESET_ADMIN_PASSWORD=true detected. Resetting admin password...");
    await AdminSeeder.ResetPasswordAsync(app.Services, startupLogger);
    startupLogger.LogWarning("Password reset complete. Remove RESET_ADMIN_PASSWORD from env vars and restart.");
    return; // Exit — don't start the server, just log the new password.
}

app.Run();

