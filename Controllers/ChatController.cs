using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization; // Để khóa cửa, bắt đăng nhập
using Microsoft.EntityFrameworkCore;
using RealTimeChatMVC.Data;
using RealTimeChatMVC.Models;
using System.Linq;
using System.Threading.Tasks;

namespace RealTimeChatMVC.Controllers
{
    [Authorize] // <--- Quan trọng: Có cái này thì chưa Login không vào được
    public class ChatController : Controller
    {
        private readonly ChatDbContext _context;

        public ChatController(ChatDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            
            var messages = await _context.Messages
                                         .OrderBy(m => m.Timestamp)
                                         .Take(50)
                                         .ToListAsync();

            return View(messages);
        }
    }
}