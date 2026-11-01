using Microsoft.EntityFrameworkCore;
using RetailX.Api.Infrastructure;
using RetailX.BuildingBlocks.MultiTenancy;
using RetailX.BuildingBlocks.Persistence;
using Scalar.AspNetCore;
using RetailX.Settings.Domain;
using RetailX.Settings.Infrastructure;
using RetailX.Identity.Infrastructure;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddApplicationPart(typeof(RetailX.Organization.Api.OrganizationController).Assembly)
    .AddApplicationPart(typeof(RetailX.Settings.Api.SettingsController).Assembly)
    .AddApplicationPart(typeof(RetailX.Identity.Api.AuthController).Assembly);
builder.Services.AddOpenApi();

builder.Services.AddHttpContextAccessor();


builder.Services.AddScoped<ICurrentContext, CurrentContext>();
builder.Services.AddScoped<AuditingInterceptor>();

builder.Services.AddDbContext<RetailXDbContext>((sp, options) =>
    options
        .UseSqlServer(builder.Configuration.GetConnectionString("Default"))
        .AddInterceptors(sp.GetRequiredService<AuditingInterceptor>()));

builder.Services.AddSettingsModule();

builder.Services.AddIdentityModule(builder.Configuration);
builder.Services.AddScoped<DbContext>(sp => sp.GetRequiredService<RetailXDbContext>());
builder.Services.AddScoped<ISettingsService, SettingsService>();

var jwt = builder.Configuration.GetSection("Jwt");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.MapInboundClaims = false;   // keep claim names as written (sub, tenant_id, ...)
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwt["Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["SigningKey"]!)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization();
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();