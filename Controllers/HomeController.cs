using Microsoft.AspNetCore.Mvc;

namespace RealTimeChatMVC.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View(); // Chỉ trả về View, không cần logic Database gì cả
        }
    }
}