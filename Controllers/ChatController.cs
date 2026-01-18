using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace RealTimeChatMVC.Controllers
{
    [Authorize] // Bắt buộc đăng nhập mới vào được
    public class ChatController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}