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
                .Select(g => new { g.Id, g.Name, g.IsPrivate })
                .ToListAsync();

            return Json(groups);
        }

        // [MỚI] Lấy danh sách tất cả user (Id, Username) để hiển thị ID ở frontend
        [HttpGet]
        public async Task<IActionResult> GetAllUsers()
        {
            var onlineUsers = await _context.OnlineUsers
                .Join(_context.Users, 
                      o => o.UserId, 
                      u => u.Id, 
                      (o, u) => new { u.Id, u.Username }) // Trả về đúng object
                .Distinct() // Tránh trùng lặp nếu 1 user login nhiều nơi
                .ToListAsync();
            return Json(onlineUsers);
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
        public async Task<IActionResult> CreateGroup([FromBody] CreateGroupRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.GroupName)) return BadRequest("Tên nhóm không hợp lệ");

                var username = User.Identity.Name;
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);

                if (user == null) return Unauthorized();

                var newGroup = new ChatGroup
                {
                    Name = request.GroupName,
                    CreatedBy = username,
                    OwnerId = user.Id,
                    GroupCode = Guid.NewGuid().ToString().Substring(0, 6).ToUpper(), // Mã khóa ngẫu nhiên
                    Members = new List<User> { user } // Tự động thêm người tạo vào nhóm
                };

                // Nếu có mời thành viên ngay lúc tạo
                if (request.InvitedUserId > 0)
                {
                    var invitedUser = await _context.Users.FindAsync(request.InvitedUserId);
                    if (invitedUser != null) newGroup.Members.Add(invitedUser);
                }

                _context.ChatGroups.Add(newGroup);
                await _context.SaveChangesAsync();

                return Ok(new { id = newGroup.Id, name = newGroup.Name, code = newGroup.GroupCode });
            }
            catch (Exception ex)
            {
                // Trả về lỗi chi tiết kèm InnerException (nếu có) để dễ debug
                return StatusCode(500, "Lỗi Server: " + ex.Message + (ex.InnerException != null ? " - " + ex.InnerException.Message : ""));
            }
        }

        // [MỚI] Tạo nhóm chat riêng (Private Chat)
        [HttpPost]
        public async Task<IActionResult> CreatePrivateChat([FromBody] PrivateChatRequest request)
        {
            var targetUsername = request.TargetUsername;
            var currentUsername = User.Identity.Name;
            if (currentUsername == targetUsername) return BadRequest("Không thể chat với chính mình");

            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == currentUsername);
            var targetUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == targetUsername);

            if (currentUser == null) return Unauthorized("User không được tìm thấy");
            if (targetUser == null) return NotFound("Người dùng không tồn tại");

            // [FIX] Kiểm tra xem đã có nhóm chat riêng giữa 2 người này chưa
            var existingGroup = await _context.ChatGroups
                .Include(g => g.Members)
                .Where(g => g.IsPrivate && g.Members.Any(m => m.Username == currentUsername) && g.Members.Any(m => m.Username == targetUsername))
                .FirstOrDefaultAsync();

            if (existingGroup != null)
            {
                return Ok(new { id = existingGroup.Id, name = existingGroup.Name, isPrivate = true });
            }

            var group = new ChatGroup
            {
                Name = $"{currentUsername} - {targetUsername}", // Tên nhóm tự động
                IsPrivate = true,
                CreatedBy = currentUsername,
                Members = new List<User> { currentUser, targetUser }
            };

            _context.ChatGroups.Add(group);
            await _context.SaveChangesAsync();

            return Ok(new { id = group.Id, name = group.Name, isPrivate = true });
        }

        // 3. Tham gia nhóm (Logic Database)
        // Lưu ý: Logic này dùng để lưu vào DB. Việc join realtime sẽ do SignalR đảm nhận ở Client.
        [HttpPost]
        public async Task<IActionResult> JoinGroup(string groupCode)
        {
            var username = User.Identity.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            var group = await _context.ChatGroups.Include(g => g.Members).FirstOrDefaultAsync(g => g.GroupCode == groupCode);

            if (user != null && group != null)
            {
                if (!group.Members.Any(m => m.Id == user.Id))
                {
                    group.Members.Add(user);
                    await _context.SaveChangesAsync();
                }
                return Ok(new { message = "Đã tham gia nhóm thành công" });
            }
            return BadRequest("Mã nhóm không đúng hoặc lỗi hệ thống");
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
                    timestamp = m.Timestamp.ToString("HH:mm"),
                    type = m.Type
                })
                .ToListAsync();

            return Json(messages);
        }

        // 5. Mời thành viên ra khỏi nhóm (Kick)
        [HttpPost]
        public async Task<IActionResult> KickMember(int groupId, int userId)
        {
            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == User.Identity.Name);
            var group = await _context.ChatGroups.Include(g => g.Members).FirstOrDefaultAsync(g => g.Id == groupId);

            if (group == null || currentUser == null) return NotFound();

            // Chỉ chủ nhóm mới được kick
            if (group.OwnerId != currentUser.Id) return Forbid("Bạn không phải chủ nhóm");

            var targetUser = group.Members.FirstOrDefault(m => m.Id == userId);
            if (targetUser != null)
            {
                group.Members.Remove(targetUser);
                await _context.SaveChangesAsync();
                return Ok();
            }
            return NotFound("Thành viên không tồn tại");
        }

        // 6. Rời nhóm
        [HttpPost]
        public async Task<IActionResult> LeaveGroup(int groupId)
        {
            var username = User.Identity.Name;
            var group = await _context.ChatGroups.Include(g => g.Members).FirstOrDefaultAsync(g => g.Id == groupId);
            var user = group?.Members.FirstOrDefault(u => u.Username == username);

            if (group != null && user != null)
            {
                group.Members.Remove(user);
                await _context.SaveChangesAsync();
                return Ok();
            }
            return BadRequest();
        }

        // 7. Thêm thành viên bằng ID
        [HttpPost]
        public async Task<IActionResult> AddMemberById(int groupId, int userId)
        {
             var group = await _context.ChatGroups.Include(g => g.Members).FirstOrDefaultAsync(g => g.Id == groupId);
             var user = await _context.Users.FindAsync(userId);
             if(group != null && user != null && !group.Members.Contains(user)) {
                 group.Members.Add(user);
                 await _context.SaveChangesAsync();
                 return Ok();
             }
             return BadRequest("Không tìm thấy user hoặc nhóm");
        }

        public class CreateGroupRequest {
            public string GroupName { get; set; }
            public int InvitedUserId { get; set; }
        }

        public class PrivateChatRequest {
            public string TargetUsername { get; set; }
        }
    }
}