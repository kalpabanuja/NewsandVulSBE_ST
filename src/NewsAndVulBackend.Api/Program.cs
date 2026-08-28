using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NewsAndVulBackend.Api.Services;
using NewsAndVulBackend.Core.Interfaces;
using NewsAndVulBackend.Infrastructure.Data;
using NewsAndVulBackend.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Register DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register Services
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IStorageService, S3StorageService>();

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
            ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
            ValidAudience = builder.Configuration["JwtSettings:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:Secret"]!))
        };
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Use authentication and authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
