using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using RealTimeChatMVC.Data;
using RealTimeChatMVC.Models;
using System.Linq;

namespace RealTimeChatMVC.Controllers
{
    [Authorize] // Phải đăng nhập mới được vào đây
    public class GroupController : Controller
    {
        private readonly ChatDbContext _context;

        public GroupController(ChatDbContext context)
        {
            _context = context;
        }

        // 1. Hiện danh sách các nhóm đang có
        public IActionResult Index()
        {
            var groups = _context.ChatGroups.OrderByDescending(g => g.CreatedAt).ToList();
            return View(groups);
        }

        // 2. Xử lý tạo nhóm mới
        [HttpPost]
        public IActionResult Create(string groupName)
        {
            if (!string.IsNullOrEmpty(groupName))
            {
                var newGroup = new ChatGroup
                {
                    Name = groupName,
                    CreatedBy = User.Identity.Name // Lấy tên người đang đăng nhập
                };

                _context.ChatGroups.Add(newGroup);
                _context.SaveChanges();
            }
            // Tạo xong thì load lại trang danh sách
            return RedirectToAction("Index");
        }
        
        // 3. Vào phòng chat (Chuyển hướng sang trang Chat với ID phòng)
        public IActionResult Join(int id)
        {
            // Chuyển sang ChatController, hành động Index, kèm theo id phòng
            return RedirectToAction("Index", "Chat", new { roomId = id });
        }
    }
}