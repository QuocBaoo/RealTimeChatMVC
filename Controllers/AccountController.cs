using Microsoft.AspNetCore.Mvc;
namespace RealTimeChatMVC.Controllers
{
    public class AccountController : Controller
    {
        public IActionResult Login() => View();
        public IActionResult Register() => View();
    }
}
