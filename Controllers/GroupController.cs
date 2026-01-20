using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using RealTimeChatMVC.Data;
using RealTimeChatMVC.Models;
using RealTimeChatMVC.Hubs;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RealTimeChatMVC.Controllers
{
    [Authorize]
    public class GroupController : Controller
    {
        private readonly ChatDbContext _context;
        private readonly IHubContext<ChatHub> _hubContext;

        public GroupController(ChatDbContext context, IHubContext<ChatHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        // 1. Lấy danh sách nhóm của tôi
        [HttpGet]
        public async Task<IActionResult> GetMyGroups()
        {
            var username = User.Identity.Name;

            var groups = await _context.ChatGroups
                .Include(g => g.Members)
                .Where(g => g.Members.Any(u => u.Username == username))
                .Select(g => new
                {
                    g.Id,
                    g.Name,
                    g.IsPrivate,
                    g.GroupCode,
                    Members = g.Members
                })
                .ToListAsync();

            var result = groups.Select(g => new
            {
                g.Id,
                GroupName = g.Name,
                Name = g.IsPrivate
                    ? g.Members.FirstOrDefault(m => m.Username != username)?.Username ?? "Unknown"
                    : g.Name,
                IsPrivate = g.IsPrivate,
                AvatarColor = g.IsPrivate
                    ? g.Members.FirstOrDefault(m => m.Username != username)?.AvatarColor
                    : null,
                g.GroupCode,
                MemberCount = g.Members.Count // [MỚI] Trả về số lượng thành viên
            });

            return Json(result);
        }

        // 2. Lấy danh sách user online
        [HttpGet]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _context.OnlineUsers
                .Join(_context.Users,
                      o => o.UserId,
                      u => u.Id,
                      (o, u) => new { u.Id, u.Username, u.FullName })
                .Distinct()
                .ToListAsync();

            return Json(users);
        }

        // 3. Lấy tất cả nhóm
        [HttpGet]
        public async Task<IActionResult> GetAllGroups()
        {
            var username = User.Identity.Name;

            var groups = await _context.ChatGroups
                .Include(g => g.Members)
                .Select(g => new
                {
                    g.Id,
                    g.Name,
                    IsJoined = g.Members.Any(m => m.Username == username)
                })
                .ToListAsync();

            return Json(groups);
        }

        // 4. Tạo nhóm thường
        [HttpPost]
        public async Task<IActionResult> CreateGroup([FromBody] CreateGroupRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.GroupName))
                return BadRequest("Tên nhóm không hợp lệ");

            var username = User.Identity.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null) return Unauthorized();

            var group = new ChatGroup
            {
                Name = request.GroupName,
                CreatedBy = username,
                OwnerId = user.Id,
                GroupCode = Guid.NewGuid().ToString("N")[..6].ToUpper(),
                Members = new List<User> { user }
            };

            _context.ChatGroups.Add(group);
            await _context.SaveChangesAsync();

            if (request.InvitedUserId > 0)
            {
                await InviteMemberInternal(group.Id, request.InvitedUserId, user.Id);
            }

            return Ok(new { id = group.Id, name = group.Name, code = group.GroupCode });
        }

        // 5. Tạo private chat (ĐÃ FIX HOÀN TOÀN)
        [HttpPost]
        public async Task<IActionResult> CreatePrivateChat([FromBody] PrivateChatRequest request)
        {
            var currentUsername = User.Identity.Name;
            var targetUsername = request.TargetUsername;

            if (currentUsername == targetUsername)
                return BadRequest("Không thể chat với chính mình");

            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == currentUsername);
            var targetUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == targetUsername);

            if (currentUser == null) return Unauthorized();
            if (targetUser == null) return NotFound("Người dùng không tồn tại");

            var existingGroup = await _context.ChatGroups
                .Include(g => g.Members)
                .FirstOrDefaultAsync(g =>
                    g.IsPrivate &&
                    g.Members.Any(m => m.Id == currentUser.Id) &&
                    g.Members.Any(m => m.Id == targetUser.Id));

            if (existingGroup != null)
            {
                return Ok(new
                {
                    id = existingGroup.Id,
                    name = existingGroup.Name,
                    isPrivate = true
                });
            }

            var group = new ChatGroup
            {
                Name = $"{currentUsername} - {targetUsername}",
                IsPrivate = true,
                CreatedBy = currentUsername,
                Members = new List<User> { currentUser, targetUser }
            };

            _context.ChatGroups.Add(group);
            await _context.SaveChangesAsync();

            // REAL-TIME
            await _hubContext.Clients.User(targetUsername)
                .SendAsync("ReceiveNewGroup", group.Id, group.Name, currentUsername);

            await _hubContext.Clients.User(targetUser.Id.ToString())
                .SendAsync("ReceiveNewGroup", group.Id, group.Name, currentUsername);

            return Ok(new
            {
                id = group.Id,
                name = group.Name,
                isPrivate = true
            });
        }

        // 6. Tham gia nhóm
        [HttpPost]
        public async Task<IActionResult> JoinGroup(string groupCode)
        {
            var username = User.Identity.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            var group = await _context.ChatGroups.Include(g => g.Members)
                .FirstOrDefaultAsync(g => g.GroupCode == groupCode);

            if (user == null || group == null)
                return BadRequest("Mã nhóm không hợp lệ");

            if (!group.Members.Any(m => m.Id == user.Id))
            {
                group.Members.Add(user);
                await _context.SaveChangesAsync();
            }

            return Ok("Đã tham gia nhóm");
        }

        // 7. Lịch sử chat nhóm
        [HttpGet]
        public async Task<IActionResult> GetGroupHistory(int groupId)
        {
            var messages = await _context.Messages
                .Where(m => m.ChatGroupId == groupId)
                .OrderBy(m => m.Timestamp)
                .Join(_context.Users,
                      m => m.SenderName,
                      u => u.Username,
                      (m, u) => new
                      {
                          sender = m.SenderName,
                          senderFullName = u.FullName,
                          content = m.Content,
                          timestamp = m.Timestamp.ToString("HH:mm"),
                          type = m.Type
                      })
                .ToListAsync();

            return Json(messages);
        }

        // 8. Kick thành viên
        [HttpPost]
        public async Task<IActionResult> KickMember(int groupId, int userId)
        {
            var me = await _context.Users.FirstOrDefaultAsync(u => u.Username == User.Identity.Name);
            var group = await _context.ChatGroups.Include(g => g.Members)
                .FirstOrDefaultAsync(g => g.Id == groupId);

            if (me == null || group == null) return NotFound();
            if (group.OwnerId != me.Id) return Forbid();

            var target = group.Members.FirstOrDefault(m => m.Id == userId);
            if (target == null) return NotFound();

            group.Members.Remove(target);
            await _context.SaveChangesAsync();
            return Ok();
        }

        // 9. Rời nhóm
        [HttpPost]
        public async Task<IActionResult> LeaveGroup(int groupId)
        {
            var username = User.Identity.Name;
            var group = await _context.ChatGroups.Include(g => g.Members)
                .FirstOrDefaultAsync(g => g.Id == groupId);

            var user = group?.Members.FirstOrDefault(u => u.Username == username);
            if (user == null) return BadRequest();

            group.Members.Remove(user);
            await _context.SaveChangesAsync();
            return Ok();
        }

        // 10. Mời thành viên
        [HttpPost]
        public async Task<IActionResult> InviteMember(int groupId, int userId)
        {
            var me = await _context.Users.FirstOrDefaultAsync(u => u.Username == User.Identity.Name);
            if (me == null) return Unauthorized();

            var ok = await InviteMemberInternal(groupId, userId, me.Id);
            return ok ? Ok() : BadRequest();
        }

        private async Task<bool> InviteMemberInternal(int groupId, int inviteeId, int inviterId)
        {
            var group = await _context.ChatGroups.Include(g => g.Members)
                .FirstOrDefaultAsync(g => g.Id == groupId);
            var invitee = await _context.Users.FindAsync(inviteeId);

            if (group == null || invitee == null) return false;
            if (group.Members.Any(m => m.Id == inviteeId)) return false;

            var exists = await _context.GroupInvitations
                .AnyAsync(i => i.GroupId == groupId && i.InviteeId == inviteeId);
            if (exists) return false;

            _context.GroupInvitations.Add(new GroupInvitation
            {
                GroupId = groupId,
                InviteeId = inviteeId,
                InviterId = inviterId
            });

            await _context.SaveChangesAsync();

            await _hubContext.Clients.User(invitee.Username).SendAsync("ReceiveGroupInvitation");
            await _hubContext.Clients.User(invitee.Id.ToString()).SendAsync("ReceiveGroupInvitation");

            return true;
        }

        // 11. Lấy lời mời của tôi
        [HttpGet]
        public async Task<IActionResult> GetMyGroupInvitations()
        {
            var me = await _context.Users.FirstOrDefaultAsync(u => u.Username == User.Identity.Name);

            var invites = await _context.GroupInvitations
                .Where(i => i.InviteeId == me.Id)
                .Join(_context.ChatGroups, i => i.GroupId, g => g.Id,
                    (i, g) => new
                    {
                        InviteId = i.Id,
                        GroupId = g.Id,
                        GroupName = g.Name,
                        MemberCount = g.Members.Count
                    })
                .ToListAsync();

            return Json(invites);
        }

        // 12. Phản hồi lời mời
        [HttpPost]
        public async Task<IActionResult> RespondGroupInvite(int inviteId, bool accept)
        {
            var invite = await _context.GroupInvitations.FindAsync(inviteId);
            if (invite == null) return NotFound();

            if (accept)
            {
                var group = await _context.ChatGroups.Include(g => g.Members)
                    .FirstOrDefaultAsync(g => g.Id == invite.GroupId);
                var user = await _context.Users.FindAsync(invite.InviteeId);

                if (group != null && user != null)
                    group.Members.Add(user);
            }

            _context.GroupInvitations.Remove(invite);
            await _context.SaveChangesAsync();
            return Ok();
        }

        public class CreateGroupRequest
        {
            public string GroupName { get; set; }
            public int InvitedUserId { get; set; }
        }

        public class PrivateChatRequest
        {
            public string TargetUsername { get; set; }
        }
    }
}
