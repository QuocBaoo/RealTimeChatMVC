using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RealTimeChatMVC.Data;
using System.Linq;
using System.Threading.Tasks;

namespace RealTimeChatMVC.Controllers
{
    [Authorize]
    public class ChatController : Controller
    {
        private readonly ChatDbContext _context;

        public ChatController(ChatDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            
            var messages = await _context.Messages
                                         .Where(m => m.ChatGroupId == null) // CHỈ LẤY TIN NHẮN CHUNG
                                         .OrderBy(m => m.Timestamp)
                                         .Take(50)
                                         .ToListAsync();
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetHistory()
        {
            var messages = await _context.Messages
                .OrderByDescending(m => m.Timestamp) // Lấy tin mới nhất trước
                .Take(50)                            // Giới hạn 50 tin
                .OrderBy(m => m.Timestamp)           // Đảo lại để hiển thị từ cũ -> mới
                .Select(m => new {
                    user = m.SenderName,
                    message = m.Content,
                    time = m.Timestamp.ToString("HH:mm:ss")
                })
                .ToListAsync();

            return Json(messages);
        }
    }
}