using LMS.Views.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using LMS.Services;
using OpenAI;
using OpenAI.Chat; // needed for ChatClient and messages

var builder = WebApplication.CreateBuilder(args);

// ----------------------
// Services Configuration
// ----------------------

// Add DbContext with MySQL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DefaultConnection"))
    ));

// Add Controllers with Views
builder.Services.AddControllersWithViews();

// ----------------------
// OpenAI ChatClient & AIQuizService
// ----------------------

// Get API key from config or environment
string openAiApiKey = builder.Configuration["OpenAI:ApiKey"] ?? "default_value";

if (string.IsNullOrWhiteSpace(openAiApiKey))
    throw new InvalidOperationException("OpenAI API key not configured. Set OPENAI_API_KEY or OpenAI:ApiKey in appsettings.");

// Register ChatClient (official SDK v2.3.0)
builder.Services.AddSingleton<ChatClient>(sp =>
{
    string model = builder.Configuration["OpenAI:Model"] ?? "gpt-4o-mini";
    return new ChatClient(model, openAiApiKey);
});

// Register AIQuizService for DI
builder.Services.AddScoped<IAIQuizService, AIQuizService>();

// ----------------------
// Authentication, Session, SignalR
// ----------------------

// Add Cookie Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
    });

// Add Session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Add Authorization
builder.Services.AddAuthorization();

// Add SignalR
builder.Services.AddSignalR();

//Add ActivityService
builder.Services.AddScoped<IActivityService, ActivityService>();




var app = builder.Build();

// ----------------------
// Middleware Pipeline
// ----------------------

// Error Handling
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Startup / Program.cs
app.Use(async (context, next) =>
{
    // Call next first so controllers can set response if needed (optionally log after)
    await next();

    // Example: log only if user is authenticated and endpoint is relevant
    var user = context.User?.Identity;
    if (user?.IsAuthenticated == true)
    {
        var path = context.Request.Path.ToString();
        // Decide what to log here or let controllers create detailed logs
        // This is a simple example—more complete logic should live in controllers/services.
    }
});


app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Session must come before authentication if needed in auth
app.UseSession();

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// SignalR
app.MapHub<LMS.Hubs.ChatHub>("/chathub");

// Default route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();
