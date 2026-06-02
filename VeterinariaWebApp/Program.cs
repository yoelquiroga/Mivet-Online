using Microsoft.AspNetCore.Hosting;
using Rotativa.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

var envPath = Path.Combine(builder.Environment.ContentRootPath, "..", "secure.env");
if (File.Exists(envPath))
    DotNetEnv.Env.Load(envPath);
builder.Configuration.AddEnvironmentVariables();

// Add services to the container.
builder.Services.AddControllersWithViews();

//  Servicios de sesión y caché
builder.Services.AddMemoryCache();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
});

//  Registrar HttpClient con la URL base de tu API
builder.Services.AddHttpClient("ClinicaAPI", client =>
{
    client.BaseAddress = new Uri("https://localhost:7054/api/");
    client.DefaultRequestHeaders.Add("X-API-Key",
        builder.Configuration["ApiSettings:ApiKey"]);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

//  Middleware de sesión (DEBE estar antes de MapControllerRoute)
app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);

// Configuración de Rotativa para generar PDFs
RotativaConfiguration.Setup(app.Environment.WebRootPath, "../Rotativa");

app.Run();