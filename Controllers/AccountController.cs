using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using RealTimeChatMVC.Data;
using RealTimeChatMVC.Models;

namespace RealTimeChatMVC.Controllers
{
    public class AccountController : Controller
    {
        private readonly ChatDbContext _context;

        public AccountController(ChatDbContext context)
        {
            _context = context;
        }

        // --- ĐĂNG KÝ ---
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken] // [FIX] Chống tấn công CSRF
        // SỬA LỖI: Dùng tên đầy đủ "RealTimeChatMVC.Models.User" để tránh nhầm lẫn
        public async Task<IActionResult> Register(RealTimeChatMVC.Models.User user)
        {
            // 1. Kiểm tra dữ liệu nhập vào
            if (!ModelState.IsValid)
            {
                // Gom các lỗi lại thành 1 dòng để dễ đọc
                var errors = string.Join("; ", ModelState.Values
                                                .SelectMany(v => v.Errors)
                                                .Select(e => e.ErrorMessage));
                ViewBag.Error = "Dữ liệu chưa đúng: " + errors;
                return View(user);
            }

            try
            {
                // 2. Kiểm tra trùng tên
                var existingUser = _context.Users.FirstOrDefault(u => u.Username == user.Username);
                if (existingUser != null)
                {
                    ViewBag.Error = $"Tài khoản '{user.Username}' đã có người dùng!";
                    return View(user);
                }

                // Generate Random Fixed Color
                var random = new Random();
                user.AvatarColor = String.Format("#{0:X6}", random.Next(0x1000000));

                // 3. Lưu vào Database
                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // 4. Thành công
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Lỗi Database: " + ex.Message;
                if (ex.InnerException != null)
                {
                    ViewBag.Error += " (" + ex.InnerException.Message + ")";
                }
                return View(user);
            }
        }

        // --- ĐĂNG NHẬP ---
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken] // [FIX] Chống tấn công CSRF
        public async Task<IActionResult> Login(string username, string password)
        {
            // Tìm user trong DB
            var user = _context.Users.FirstOrDefault(u => u.Username == username && u.Password == password);

            if (user != null)
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim("FullName", user.FullName ?? user.Username),
                    new Claim("UserId", user.Id.ToString()),
                    new Claim("AvatarColor", user.AvatarColor ?? "#000")
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

                return RedirectToAction("Index", "Home");
            }

            ViewBag.Error = "Sai tài khoản hoặc mật khẩu!";
            ViewBag.Error = "Tên đăng nhập hoặc mật khẩu không đúng";
            return View();
        }

        // --- ĐĂNG XUẤT ---
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }
    }
}