using Microsoft.EntityFrameworkCore;
using RetailX.Api.Infrastructure;
using RetailX.BuildingBlocks.MultiTenancy;
using RetailX.BuildingBlocks.Persistence;
using Scalar.AspNetCore;
using RetailX.Settings.Domain;
using RetailX.Settings.Infrastructure;
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddApplicationPart(typeof(RetailX.Organization.Api.OrganizationController).Assembly)
    .AddApplicationPart(typeof(RetailX.Settings.Api.SettingsController).Assembly);

builder.Services.AddOpenApi();

builder.Services.AddHttpContextAccessor();


builder.Services.AddScoped<ICurrentContext, CurrentContext>();
builder.Services.AddScoped<AuditingInterceptor>();

builder.Services.AddDbContext<RetailXDbContext>((sp, options) =>
    options
        .UseSqlServer(builder.Configuration.GetConnectionString("Default"))
        .AddInterceptors(sp.GetRequiredService<AuditingInterceptor>()));
builder.Services.AddSettingsModule();
builder.Services.AddScoped<DbContext>(sp => sp.GetRequiredService<RetailXDbContext>());
builder.Services.AddScoped<ISettingsService, SettingsService>();
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();