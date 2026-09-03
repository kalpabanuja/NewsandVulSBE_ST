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
    <title>Premium Admin Dashboard | Notes & File Backend</title>
    <link href=""https://fonts.googleapis.com/css2?family=Outfit:wght@300;400;500;600;700&display=swap"" rel=""stylesheet"">
    <style>
        :root {
            --bg-color: #0B0F19;
            --glass-bg: rgba(17, 24, 39, 0.6);
            --glass-border: rgba(255, 255, 255, 0.08);
            --primary: #6366F1;
            --primary-hover: #4F46E5;
            --accent: #10B981;
            --text-main: #F9FAFB;
            --text-muted: #9CA3AF;
            --danger: #EF4444;
        }
        * { box-sizing: border-box; margin: 0; padding: 0; font-family: 'Outfit', sans-serif; }
        body {
            background-color: var(--bg-color);
            background-image: 
                radial-gradient(at 0% 0%, rgba(99, 102, 241, 0.15) 0px, transparent 50%),
                radial-gradient(at 100% 100%, rgba(16, 185, 129, 0.1) 0px, transparent 50%);
            background-attachment: fixed;
            color: var(--text-main);
            min-height: 100vh;
            display: flex;
            flex-direction: column;
        }
        #login-container { display: flex; justify-content: center; align-items: center; flex-grow: 1; height: 100vh; }
        .glass-card {
            background: var(--glass-bg);
            backdrop-filter: blur(16px);
            -webkit-backdrop-filter: blur(16px);
            border: 1px solid var(--glass-border);
            border-radius: 20px;
            padding: 2.5rem;
            box-shadow: 0 25px 50px -12px rgba(0, 0, 0, 0.5);
            transition: transform 0.3s ease;
        }
        .login-card { width: 100%; max-width: 420px; }
        h2 { margin-bottom: 1.5rem; font-weight: 600; text-align: center; font-size: 1.8rem; letter-spacing: -0.02em; }
        .input-group { margin-bottom: 1.5rem; }
        .input-group label { display: block; margin-bottom: 0.5rem; font-size: 0.9rem; color: var(--text-muted); font-weight: 500; }
        .input-group input {
            width: 100%; padding: 0.85rem 1.2rem; background: rgba(0, 0, 0, 0.3);
            border: 1px solid var(--glass-border); border-radius: 10px; color: var(--text-main); font-size: 1rem;
            transition: all 0.3s;
        }
        .input-group input:focus { outline: none; border-color: var(--primary); box-shadow: 0 0 0 3px rgba(99, 102, 241, 0.2); }
        button {
            width: 100%; padding: 0.85rem; background: linear-gradient(135deg, var(--primary), var(--primary-hover));
            color: white; border: none; border-radius: 10px; font-size: 1.05rem; font-weight: 600; cursor: pointer;
            transition: transform 0.2s, box-shadow 0.2s; box-shadow: 0 4px 14px 0 rgba(99, 102, 241, 0.39);
        }
        button:hover { transform: translateY(-1px); box-shadow: 0 6px 20px rgba(99, 102, 241, 0.4); }
        .error-msg { color: var(--danger); font-size: 0.9rem; margin-top: 1rem; text-align: center; display: none; font-weight: 500; }
        
        #dashboard-container { display: none; padding: 3rem 2rem; max-width: 1300px; margin: 0 auto; width: 100%; animation: fadeIn 0.5s ease-out; }
        @keyframes fadeIn { from { opacity: 0; transform: translateY(10px); } to { opacity: 1; transform: translateY(0); } }
        header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 2.5rem; }
        header h2 { margin: 0; text-align: left; }
        .logout-btn { width: auto; padding: 0.6rem 1.5rem; background: rgba(255,255,255,0.05); box-shadow: none; border: 1px solid var(--glass-border); }
        .logout-btn:hover { background: rgba(255,255,255,0.1); box-shadow: none; }
        
        .tabs { display: flex; gap: 1rem; margin-bottom: 2rem; }
        .tab-btn { width: auto; background: rgba(255,255,255,0.05); box-shadow: none; border: 1px solid var(--glass-border); padding: 0.75rem 1.5rem; border-radius: 12px; font-weight: 500; }
        .tab-btn.active { background: var(--primary); border-color: var(--primary); box-shadow: 0 4px 14px 0 rgba(99, 102, 241, 0.39); }
        
        .tab-content { display: none; }
        .tab-content.active { display: block; animation: fadeIn 0.4s ease-out; }
        
        .grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(280px, 1fr)); gap: 1.5rem; margin-bottom: 2rem; }
        .metric-card { display: flex; flex-direction: column; gap: 0.5rem; }
        .metric-title { color: var(--text-muted); font-size: 0.85rem; text-transform: uppercase; letter-spacing: 0.05em; font-weight: 600; display: flex; justify-content: space-between; align-items: center; }
        .metric-value { font-size: 2.8rem; font-weight: 700; color: var(--text-main); line-height: 1.1; }
        .metric-value span { font-size: 1rem; color: var(--text-muted); font-weight: 400; }
        
        .controls { display: flex; justify-content: space-between; align-items: center; margin-bottom: 1.5rem; }
        select { background: rgba(0, 0, 0, 0.3); border: 1px solid var(--glass-border); color: var(--text-main); padding: 0.6rem 1rem; border-radius: 8px; font-family: 'Outfit'; outline: none; cursor: pointer; }
        select:focus { border-color: var(--primary); }
        
        table { width: 100%; border-collapse: separate; border-spacing: 0; }
        th, td { text-align: left; padding: 1.2rem 1rem; border-bottom: 1px solid var(--glass-border); }
        th { color: var(--text-muted); font-weight: 500; font-size: 0.9rem; text-transform: uppercase; letter-spacing: 0.02em; }
        tbody tr { transition: background 0.2s; }
        tbody tr:hover { background: rgba(255,255,255,0.03); }
        td { font-size: 0.95rem; }
        
        .badge { background: rgba(16, 185, 129, 0.2); color: var(--accent); padding: 0.2rem 0.6rem; border-radius: 20px; font-size: 0.8rem; font-weight: 600; }
        .loading { text-align: center; padding: 3rem; color: var(--text-muted); font-style: italic; }
        
        /* Format Bytes Function */
        .progress-bar { width: 100%; height: 6px; background: rgba(255,255,255,0.1); border-radius: 4px; margin-top: 1rem; overflow: hidden; }
        .progress-fill { height: 100%; background: linear-gradient(90deg, var(--primary), var(--accent)); border-radius: 4px; transition: width 1s ease-in-out; }
    </style>
</head>
<body>
    <div id=""login-container"">
        <div class=""glass-card login-card"">
            <h2>Admin Portal</h2>
            <div class=""input-group"">
                <label for=""email"">Email Address</label>
                <input type=""email"" id=""email"" placeholder=""admin@notesandfile.local"" value=""admin@notesandfile.local"">
            </div>
            <div class=""input-group"">
                <label for=""password"">Password</label>
                <input type=""password"" id=""password"" placeholder=""Enter admin password"">
            </div>
            <button id=""login-btn"">Sign In</button>
            <div class=""error-msg"" id=""login-error"">Invalid credentials</div>
        </div>
    </div>
    
    <div id=""dashboard-container"">
        <header>
            <h2>Admin Dashboard</h2>
            <button class=""logout-btn"" id=""logout-btn"">Sign Out</button>
        </header>
        
        <div class=""tabs"">
            <button class=""tab-btn active"" data-target=""storage-tab"">Storage Analytics</button>
            <button class=""tab-btn"" data-target=""users-tab"">User Directory</button>
        </div>

        <div id=""storage-tab"" class=""tab-content active"">
            <div class=""controls"">
                <h3>System Overview</h3>
                <select id=""time-filter"">
                    <option value=""all"">All Time</option>
                    <option value=""monthly"">Last 30 Days</option>
                    <option value=""weekly"">Last 7 Days</option>
                    <option value=""daily"">Last 24 Hours</option>
                </select>
            </div>
            <div id=""storage-loading"" class=""loading"">Loading analytics...</div>
            <div id=""storage-content"" style=""display:none;"">
                <div class=""grid"">
                    <div class=""glass-card metric-card"">
                        <div class=""metric-title"">Storage Used</div>
                        <div class=""metric-value"" id=""val-used"">0</div>
                        <div class=""progress-bar""><div class=""progress-fill"" id=""storage-progress"" style=""width: 0%""></div></div>
                    </div>
                    <div class=""glass-card metric-card"">
                        <div class=""metric-title"">Storage Left</div>
                        <div class=""metric-value"" id=""val-left"">0</div>
                    </div>
                    <div class=""glass-card metric-card"">
                        <div class=""metric-title"">Files Uploaded <span class=""badge"" id=""files-badge"">All</span></div>
                        <div class=""metric-value"" id=""val-files"">0</div>
                    </div>
                    <div class=""glass-card metric-card"">
                        <div class=""metric-title"">Notes Created <span class=""badge"" id=""notes-badge"">All</span></div>
                        <div class=""metric-value"" id=""val-notes"">0</div>
                    </div>
                </div>
            </div>
        </div>

        <div id=""users-tab"" class=""tab-content"">
            <div class=""controls"">
                <h3>Registered Users</h3>
                <span id=""users-total"" style=""color: var(--text-muted);"">Total: 0</span>
            </div>
            <div class=""glass-card"">
                <div id=""users-loading"" class=""loading"">Loading users...</div>
                <div style=""overflow-x: auto;"">
                    <table id=""users-table"" style=""display:none;"">
                        <thead>
                            <tr>
                                <th>Email</th>
                                <th>Storage Used</th>
                                <th>Current Files</th>
                                <th>Lifetime Files</th>
                                <th>Active Notes</th>
                            </tr>
                        </thead>
                        <tbody id=""users-tbody""></tbody>
                    </table>
                </div>
            </div>
        </div>
    </div>
    
    <script>
        const loginContainer = document.getElementById('login-container');
        const dashboardContainer = document.getElementById('dashboard-container');
        const loginBtn = document.getElementById('login-btn');
        const logoutBtn = document.getElementById('logout-btn');
        const errorMsg = document.getElementById('login-error');
        const timeFilter = document.getElementById('time-filter');

        function formatBytes(bytes, decimals = 2) {
            if (!+bytes) return '0 Bytes';
            const k = 1024, dm = decimals < 0 ? 0 : decimals;
            const sizes = ['Bytes', 'KB', 'MB', 'GB', 'TB', 'PB'];
            const i = Math.floor(Math.log(bytes) / Math.log(k));
            return `${parseFloat((bytes / Math.pow(k, i)).toFixed(dm))} <span>${sizes[i]}</span>`;
        }
        
        function formatBytesRaw(bytes) {
            if (!+bytes) return '0 Bytes';
            const k = 1024, i = Math.floor(Math.log(bytes) / Math.log(k));
            return `${parseFloat((bytes / Math.pow(k, i)).toFixed(2))} ${['Bytes', 'KB', 'MB', 'GB', 'TB'][i]}`;
        }

        // Tab Switching Logic
        document.querySelectorAll('.tab-btn').forEach(btn => {
            btn.addEventListener('click', () => {
                document.querySelectorAll('.tab-btn').forEach(b => b.classList.remove('active'));
                document.querySelectorAll('.tab-content').forEach(c => c.classList.remove('active'));
                btn.classList.add('active');
                document.getElementById(btn.dataset.target).classList.add('active');
                
                if(btn.dataset.target === 'users-tab' && document.getElementById('users-tbody').children.length === 0) {
                    loadUsers();
                }
            });
        });

        if (localStorage.getItem('admin_token')) showDashboard();

        loginBtn.addEventListener('click', async () => {
            loginBtn.innerText = 'Signing in...'; errorMsg.style.display = 'none';
            try {
                const response = await fetch('/api/v1/auth/sign-in', {
                    method: 'POST', headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ email: document.getElementById('email').value, password: document.getElementById('password').value, deviceName: 'AdminPortal', platform: 'Web' })
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

        timeFilter.addEventListener('change', loadStorageStats);

        function showDashboard() {
            loginContainer.style.display = 'none'; dashboardContainer.style.display = 'block';
            loadStorageStats();
        }

        async function loadStorageStats() {
            const loading = document.getElementById('storage-loading');
            const content = document.getElementById('storage-content');
            loading.style.display = 'block'; content.style.display = 'none';
            
            try {
                const filter = timeFilter.value;
                const response = await fetch(`/api/v1/admin/storage/stats?filter=${filter}`, { headers: { 'Authorization': `Bearer ${localStorage.getItem('admin_token')}` } });
                if (response.status === 401 || response.status === 403) { logoutBtn.click(); return; }
                const data = await response.json();
                
                document.getElementById('val-used').innerHTML = formatBytes(data.totalStorageUsed);
                document.getElementById('val-left').innerHTML = formatBytes(data.totalStorageLeft);
                document.getElementById('val-files').innerText = data.filesUploaded;
                document.getElementById('val-notes').innerText = data.notesUploaded;
                
                document.getElementById('files-badge').innerText = filter;
                document.getElementById('notes-badge').innerText = filter;

                const percent = Math.min(100, (data.totalStorageUsed / data.maxStorageBytes) * 100);
                document.getElementById('storage-progress').style.width = `${percent}%`;

                loading.style.display = 'none'; content.style.display = 'block';
            } catch (err) { loading.innerText = 'Failed to load storage stats.'; }
        }

        async function loadUsers() {
            const loading = document.getElementById('users-loading');
            const table = document.getElementById('users-table');
            const tbody = document.getElementById('users-tbody');
            loading.style.display = 'block'; table.style.display = 'none';
            
            try {
                const response = await fetch(`/api/v1/admin/users?page=1&pageSize=100`, { headers: { 'Authorization': `Bearer ${localStorage.getItem('admin_token')}` } });
                if (response.status === 401 || response.status === 403) { logoutBtn.click(); return; }
                const data = await response.json();
                
                document.getElementById('users-total').innerText = `Total: ${data.totalCount}`;
                tbody.innerHTML = '';
                
                data.data.forEach(user => {
                    const tr = document.createElement('tr');
                    tr.innerHTML = `
                        <td>${user.email}</td>
                        <td>${formatBytesRaw(user.storageUsed)}</td>
                        <td>${user.currentFiles}</td>
                        <td>${user.lifetimeFiles}</td>
                        <td>${user.totalNotes}</td>
                    `;
                    tbody.appendChild(tr);
                });

                loading.style.display = 'none'; table.style.display = 'table';
            } catch (err) { loading.innerText = 'Failed to load users.'; }
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

