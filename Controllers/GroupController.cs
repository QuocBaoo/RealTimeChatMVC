using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using RealTimeChatMVC.Data;
using RealTimeChatMVC.Models;
using System.Linq;
using System;
using System.Threading.Tasks;

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

        // 1. Lấy danh sách nhóm mà User hiện tại đã tham gia
        [HttpGet]
        public async Task<IActionResult> GetMyGroups()
        {
            var username = User.Identity.Name;
            var groups = await _context.ChatGroups
                .Where(g => g.Members.Any(u => u.Username == username))
                .Select(g => new { g.Id, g.Name })
                .ToListAsync();

            return Json(groups);
        }

        // [MỚI] Lấy tất cả nhóm để hiển thị cho người khác thấy
        [HttpGet]
        public async Task<IActionResult> GetAllGroups()
        {
            var username = User.Identity.Name;
            var groups = await _context.ChatGroups
                .Select(g => new { 
                    g.Id, 
                    g.Name, 
                    IsJoined = g.Members.Any(u => u.Username == username) // Kiểm tra xem user đã trong nhóm chưa
                })
                .ToListAsync();

            return Json(groups);
        }

        // 2. Xử lý tạo nhóm mới
        [HttpPost]
        public async Task<IActionResult> CreateGroup([FromBody] string groupName)
        {
            try
            {
                if (string.IsNullOrEmpty(groupName)) return BadRequest("Tên nhóm không hợp lệ");

                var username = User.Identity.Name;
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);

                if (user == null) return Unauthorized();

                var newGroup = new ChatGroup
                {
                    Name = groupName,
                    CreatedBy = username,
                    Members = new List<User> { user } // Tự động thêm người tạo vào nhóm
                };

                _context.ChatGroups.Add(newGroup);
                await _context.SaveChangesAsync();

                return Ok(new { id = newGroup.Id, name = newGroup.Name });
            }
            catch (Exception ex)
            {
                // Trả về lỗi chi tiết kèm InnerException (nếu có) để dễ debug
                return StatusCode(500, "Lỗi Server: " + ex.Message + (ex.InnerException != null ? " - " + ex.InnerException.Message : ""));
            }
        }

        // 3. Tham gia nhóm (Logic Database)
        // Lưu ý: Logic này dùng để lưu vào DB. Việc join realtime sẽ do SignalR đảm nhận ở Client.
        [HttpPost]
        public async Task<IActionResult> JoinGroup(int groupId)
        {
            var username = User.Identity.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            var group = await _context.ChatGroups.Include(g => g.Members).FirstOrDefaultAsync(g => g.Id == groupId);

            if (user != null && group != null)
            {
                if (!group.Members.Any(m => m.Id == user.Id))
                {
                    group.Members.Add(user);
                    await _context.SaveChangesAsync();
                }
                return Ok(new { message = "Đã tham gia nhóm thành công" });
            }
            return BadRequest("Không tìm thấy nhóm hoặc user");
        }

        // 4. Lấy lịch sử tin nhắn của nhóm
        [HttpGet]
        public async Task<IActionResult> GetGroupHistory(int groupId)
        {
            var messages = await _context.Messages
                .Where(m => m.ChatGroupId == groupId) // Dùng trực tiếp FK ChatGroupId
                .OrderBy(m => m.Timestamp)
                .Select(m => new
                {
                    sender = m.SenderName,
                    content = m.Content,
                    timestamp = m.Timestamp.ToString("HH:mm")
                })
                .ToListAsync();

            return Json(messages);
        }
    }
}