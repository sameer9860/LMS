using LMS.Views.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Builder;

var builder = WebApplication.CreateBuilder(args);

// Add DbContext with MySQL using connection string from appsettings.json
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DefaultConnection"))));

// Add Controllers with Views
builder.Services.AddControllersWithViews();

// Add Authentication with Cookies
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login"; // Redirect to login page if not authenticated
        options.AccessDeniedPath = "/Account/AccessDenied"; // Optional: access denied page
    });
// Add this in Program.cs before builder.Build()
builder.Services.AddSession();
// Add Authorization
builder.Services.AddAuthorization();

var app = builder.Build();

// Configure Middleware pipeline

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
app.UseSession();
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Add Authentication & Authorization middleware in this order
app.UseAuthentication();
app.UseAuthorization();

// // Add SignalR (required for Hub)
// builder.Services.AddSignalR();

// // Register NotificationService (required for DI)
// builder.Services.AddScoped<NotificationService>();

// // Map Hub endpoint (typically near app.MapControllerRoute())
// app.MapHub<NotificationHub>("/notificationHub");
// Default route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();
