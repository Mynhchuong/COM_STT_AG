using COM_STT_WEB.API.Service;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddHttpClient<AuthApiService>(client =>
{
    var baseUrl = builder.Configuration["ApiSettings:BaseUrl"]
                  ?? throw new InvalidOperationException("ApiSettings:BaseUrl not configured.");
    client.BaseAddress = new Uri(baseUrl);
});

builder.Services.AddHttpClient<ProductionApiService>(client =>
{
    var baseUrl = builder.Configuration["ApiSettings:BaseUrl"]
                  ?? throw new InvalidOperationException("ApiSettings:BaseUrl not configured.");
    client.BaseAddress = new Uri(baseUrl);
});

builder.Services.AddHttpClient<ReportApiService>(client =>
{
    var baseUrl = builder.Configuration["ApiSettings:BaseUrl"]
                  ?? throw new InvalidOperationException("ApiSettings:BaseUrl not configured.");
    client.BaseAddress = new Uri(baseUrl);
});

builder.Services.AddAntiforgery(options =>
{
    // Cho phép JS gửi token qua header khi gọi fetch() từ các trang JSON (VD: Production/Scan)
    options.HeaderName = "RequestVerificationToken";
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/Login";
        // false: phiên không "ghi nhớ" phải hết hạn đúng giờ đã định (nửa đêm) bất kể hoạt động,
        // để ép đăng nhập lại mỗi ngày — xem comment trong AccountController.Login.
        options.SlidingExpiration = false;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.Name = ".AspNetCore.Cookies.ComStt";
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
