using Microsoft.AspNetCore.Authentication.Cookies; // [MỚI THÊM] Thư viện cookie
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using RealTimeChatMVC.Data;
using RealTimeChatMVC.Hubs;

var builder = WebApplication.CreateBuilder(args);

// --- PHẦN 1: ĐĂNG KÝ DỊCH VỤ ---

// 1. Đăng ký MVC Controllers
builder.Services.AddControllersWithViews();

// [THÊM] Cấu hình kích thước upload file
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 50 * 1024 * 1024; // 50MB (tăng từ 10MB)
});

// 2. Đăng ký SignalR
builder.Services.AddSignalR(hubOptions =>
{
    // Tăng giới hạn kích thước message (mặc định 32KB, tăng lên 1MB)
    hubOptions.MaximumReceiveMessageSize = 1024 * 1024;
    
    // Timeout cho long-running operations
    hubOptions.HandshakeTimeout = TimeSpan.FromSeconds(15);
});

// 3. Đăng ký Kết nối SQL Server
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ChatDbContext>(options =>
    options.UseSqlServer(connectionString));

// 4. Đăng ký chế độ Đăng nhập (Authentication)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60);

        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax; // [FIX] Để Lax an toàn hơn cho Dev/Prod cơ bản
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; // [FIX] Tự động theo HTTP/HTTPS
    });

// --- KẾT THÚC PHẦN ĐĂNG KÝ ---

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// 5. Kích hoạt chế độ Xác thực (Bắt buộc đặt trước Authorization)
app.UseAuthentication();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"); // Mặc định vào trang chủ, sau này mình sẽ sửa để vào Login trước

app.MapHub<ChatHub>("/chatHub");

// --- TỰ ĐỘNG TẠO DATABASE (MIGRATION) ---
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ChatDbContext>();
        context.Database.EnsureDeleted(); // <--- Bỏ comment dòng này, chạy 1 lần để Reset DB
        context.Database.EnsureCreated(); // Tự động tạo bảng dựa trên code (không cần file Migration)
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Lỗi khi khởi tạo Database (Migration).");
    }
}
// ----------------------------------------

app.Run();