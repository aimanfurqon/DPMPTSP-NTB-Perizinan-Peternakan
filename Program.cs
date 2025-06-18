using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using PerizinanPeternakan.Data;
using PerizinanPeternakan.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Database Configuration
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Session Configuration
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Session timeout 30 menit
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = "PerizinanPeternakan.Session";
});

// Register services
builder.Services.AddScoped<IPdfGeneratorService, PdfGeneratorService>();

// Add file upload services
builder.Services.Configure<IISServerOptions>(options =>
{
    options.MaxRequestBodySize = 52428800; // 50MB
});

builder.Services.Configure<KestrelServerOptions>(options =>
{
    options.Limits.MaxRequestBodySize = 52428800; // 50MB
});

builder.Services.Configure<FormOptions>(options =>
{
    options.ValueLengthLimit = int.MaxValue;
    options.MultipartBodyLengthLimit = 52428800; // 50MB
    options.MultipartHeadersLengthLimit = 16384;
    options.BufferBody = true;
    options.BufferBodyLengthLimit = 52428800; // 50MB
    options.ValueCountLimit = 1024;
    options.KeyLengthLimit = 2048;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        // Set cache headers untuk file dokumen
        if (ctx.File.Name.StartsWith("documents"))
        {
            ctx.Context.Response.Headers.Append("Cache-Control", "private, max-age=600");
        }
    }
});
app.UseRouting();

// Enable session before authorization
app.UseSession();
app.UseAuthorization();

// Default route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}");

// Simple database creation (no auto seeding)
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        // Hanya buat database dan tabel, tidak seed data
        context.Database.EnsureCreated();
        logger.LogInformation("Database ensured created successfully");

        // Cek apakah ada data users
        var userCount = context.Users.Count();
        logger.LogInformation($"Current users in database: {userCount}");

        if (userCount == 0)
        {
            logger.LogWarning("No users found in database. Please run the SQL insert script manually.");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while ensuring database creation");
    }
}

app.Run();