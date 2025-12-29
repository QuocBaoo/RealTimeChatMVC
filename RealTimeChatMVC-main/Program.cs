using Microsoft.EntityFrameworkCore;
using RealTimeChatMVC.Data;  
using RealTimeChatMVC.Hubs;  

var builder = WebApplication.CreateBuilder(args);

// --- PHẦN 1: ĐĂNG KÝ DỊCH VỤ (Service Registration) ---

// 1. Đăng ký MVC (để chạy web)
builder.Services.AddControllersWithViews();

// 2. Đăng ký SignalR (Quan trọng cho Chat)
builder.Services.AddSignalR();

// 3. Đăng ký Kết nối SQL Server
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ChatDbContext>(options =>
    options.UseSqlServer(connectionString));

// --- KẾT THÚC PHẦN ĐĂNG KÝ ---

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles(); // Cho phép truy cập wwwroot (css, js, ảnh)

app.UseRouting();

app.UseAuthorization();

// Định tuyến cho MVC
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Định tuyến cho SignalR Hub (Của bạn đây)
app.MapHub<ChatHub>("/chatHub");

app.Run();