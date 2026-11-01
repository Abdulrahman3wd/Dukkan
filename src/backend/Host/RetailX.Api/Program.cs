using Microsoft.EntityFrameworkCore;
using RetailX.Api.Infrastructure;
using RetailX.BuildingBlocks.MultiTenancy;
using RetailX.BuildingBlocks.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentContext, CurrentContext>();
builder.Services.AddScoped<AuditingInterceptor>();

builder.Services.AddDbContext<RetailXDbContext>((sp, options) =>
    options
        .UseSqlServer(builder.Configuration.GetConnectionString("Default"))
        .AddInterceptors(sp.GetRequiredService<AuditingInterceptor>()));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();