using Microsoft.AspNetCore.Authentication.Cookies; // [MỚI THÊM] Thư viện cookie
using Microsoft.EntityFrameworkCore;
using RealTimeChatMVC.Data;
using RealTimeChatMVC.Hubs;

var builder = WebApplication.CreateBuilder(args);

// --- PHẦN 1: ĐĂNG KÝ DỊCH VỤ ---

// 1. Đăng ký MVC
builder.Services.AddControllersWithViews();

// 2. Đăng ký SignalR
builder.Services.AddSignalR();

// 3. Đăng ký Kết nối SQL Server
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ChatDbContext>(options =>
    options.UseSqlServer(connectionString));

// 4. Đăng ký chế độ Đăng nhập (Authentication)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login"; // Nếu chưa đăng nhập thì chuyển hướng về đây
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60); // Đăng nhập giữ trong 60 phút
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
        // context.Database.EnsureDeleted(); // <--- Bỏ comment dòng này, chạy 1 lần để Reset DB
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