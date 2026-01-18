using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RealTimeChatMVC.Data;

namespace RealTimeChatMVC.Controllers
{
    [Authorize] // Bắt buộc đăng nhập mới vào được
    public class ChatController : Controller
    {
        private readonly ChatDbContext _context;

        public ChatController(ChatDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetHistory()
        {
            try
            {
                // Lấy tin nhắn chat chung (ChatGroupId = null)
                var messages = await _context.Messages
                    .Where(m => m.ChatGroupId == null)
                    .OrderByDescending(m => m.Timestamp)
                    .Take(100)  // Lấy 100 tin nhắn gần nhất
                    .OrderBy(m => m.Timestamp)  // Sắp xếp lại từ cũ đến mới
                    .Select(m => new
                    {
                        user = m.SenderName,
                        message = m.Content,
                        time = m.Timestamp.ToString("HH:mm:ss"),
                        type = m.Type ?? "Text"
                    })
                    .ToListAsync();

                return Json(messages);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Lỗi tải lịch sử: " + ex.Message });
            }
        }
    }
}