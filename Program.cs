using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using PerizinanPeternakan.Data;
using PerizinanPeternakan.Service;
using PerizinanPeternakan.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient();

// Database Configuration
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrEmpty(connectionString))
{
    connectionString = builder.Configuration.GetConnectionString("AzureConnection");
}
if (string.IsNullOrEmpty(connectionString))
{
    connectionString = builder.Configuration.GetConnectionString("AzurePasswordless");
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

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
builder.Services.AddTransient<IEmailSender, MailKitEmailSender>();
builder.Services.AddScoped<IApplicationNumberService, ApplicationNumberService>();
builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<IApprovalService, ApprovalService>();
builder.Services.AddScoped<INotificationService, NotificationService>();

// Configure SMTP with environment variable support
var smtpSection = builder.Configuration.GetSection("Smtp");
var smtpOptions = new SmtpOptions
{
    Host = smtpSection["Host"],
    Port = int.Parse(smtpSection["Port"] ?? "587"),
    EnableSsl = bool.Parse(smtpSection["EnableSsl"] ?? "true"),
    Username = smtpSection["UserName"],
    Password = Environment.GetEnvironmentVariable("SMTP_PASSWORD") ?? smtpSection["Password"]
};

builder.Services.Configure<SmtpOptions>(options =>
{
    options.Host = smtpOptions.Host;
    options.Port = smtpOptions.Port;
    options.EnableSsl = smtpOptions.EnableSsl;
    options.Username = smtpOptions.Username;
    options.Password = smtpOptions.Password;
});

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
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
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
        // Test database connection first
        logger.LogInformation("Testing database connection...");
        var canConnect = context.Database.CanConnect();
        logger.LogInformation($"Database connection test result: {canConnect}");

        if (canConnect)
        {
            try
            {
                // Apply migrations instead of EnsureCreated for better control
                logger.LogInformation("Applying database migrations...");
                context.Database.Migrate();
                logger.LogInformation("Database migrations applied successfully");

                // Cek apakah ada data users
                var userCount = context.Users.Count();
                logger.LogInformation($"Current users in database: {userCount}");

                if (userCount == 0)
                {
                    logger.LogWarning("No users found in database. Please run the SQL insert script manually.");
                }
            }
            catch (Exception migrationEx)
            {
                logger.LogError(migrationEx, "Error applying migrations. Trying EnsureCreated as fallback...");
                
                // Fallback to EnsureCreated if migrations fail
                context.Database.EnsureCreated();
                logger.LogInformation("Database ensured created successfully (fallback method)");
            }
        }
        else
        {
            logger.LogError("Cannot connect to database. Please check connection string and network connectivity.");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while ensuring database creation");
        // Don't throw the exception in production to prevent app from crashing
        if (app.Environment.IsDevelopment())
        {
            throw;
        }
    }
}

app.Run();