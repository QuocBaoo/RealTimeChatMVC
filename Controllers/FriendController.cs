using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using RealTimeChatMVC.Data;
using RealTimeChatMVC.Models;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.SignalR;
using RealTimeChatMVC.Hubs;

namespace RealTimeChatMVC.Controllers
{
    [Authorize]
    public class FriendController : Controller
    {
        private readonly ChatDbContext _context;
        private readonly IHubContext<ChatHub> _hubContext;

        public FriendController(ChatDbContext context, IHubContext<ChatHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        [HttpGet]
        public async Task<IActionResult> GetFriends()
        {
            var myName = User.Identity.Name;
            var me = await _context.Users.FirstOrDefaultAsync(u => u.Username == myName);
            
            // Lấy danh sách bạn bè (Status = 1)
            var friendIds = await _context.Friends
                .Where(f => (f.RequesterId == me.Id || f.ReceiverId == me.Id) && f.Status == 1)
                .Select(f => f.RequesterId == me.Id ? f.ReceiverId : f.RequesterId)
                .ToListAsync();

            var friends = await _context.Users
                .Where(u => friendIds.Contains(u.Id))
                .Select(u => new { u.Id, u.Username, u.FullName, u.AvatarColor })
                .ToListAsync();

            return Json(friends);
        }

        [HttpPost]
        public async Task<IActionResult> AddFriend(int targetId)
        {
            var myName = User.Identity.Name;
            var me = await _context.Users.FirstOrDefaultAsync(u => u.Username == myName);
            
            if (me.Id == targetId) return BadRequest("Không thể kết bạn với chính mình");

            var existing = await _context.Friends.FirstOrDefaultAsync(f => 
                (f.RequesterId == me.Id && f.ReceiverId == targetId) || 
                (f.RequesterId == targetId && f.ReceiverId == me.Id));

            if (existing != null) return BadRequest("Đã gửi lời mời hoặc đã là bạn bè");

            // SỬA: Status = 0 (Pending) thay vì 1 (Accepted)
            var friendRequest = new Friend { RequesterId = me.Id, ReceiverId = targetId, Status = 0 };
            _context.Friends.Add(friendRequest);
            await _context.SaveChangesAsync();

            // [REAL-TIME] Gửi thông báo đến người nhận
            var targetUser = await _context.Users.FindAsync(targetId);
            if (targetUser != null) 
            {
                await _hubContext.Clients.User(targetUser.Username).SendAsync("ReceiveFriendRequest");
                await _hubContext.Clients.User(targetUser.Id.ToString()).SendAsync("ReceiveFriendRequest"); // [FIX] Gửi thêm theo ID
            }

            return Ok("Đã gửi lời mời kết bạn");
        }

        // [MỚI] Lấy danh sách lời mời kết bạn
        [HttpGet]
        public async Task<IActionResult> GetPendingRequests()
        {
            var myName = User.Identity.Name;
            var me = await _context.Users.FirstOrDefaultAsync(u => u.Username == myName);

            var requests = await _context.Friends
                .Where(f => f.ReceiverId == me.Id && f.Status == 0)
                .Join(_context.Users, f => f.RequesterId, u => u.Id, (f, u) => new { 
                    RequestId = f.Id,
                    RequesterId = u.Id,
                    RequesterName = u.Username,
                    RequesterAvatar = u.AvatarColor,
                    RequesterFullName = u.FullName // [MỚI] Thêm FullName để hiển thị Avatar đúng
                })
                .ToListAsync();

            return Json(requests);
        }

        // [MỚI] Xử lý lời mời (Chấp nhận / Từ chối)
        [HttpPost]
        public async Task<IActionResult> RespondFriendRequest(int requestId, bool isAccept)
        {
            var request = await _context.Friends.FindAsync(requestId);
            if (request == null) return NotFound();

            var myName = User.Identity.Name;
            var me = await _context.Users.FirstOrDefaultAsync(u => u.Username == myName);

            if (request.ReceiverId != me.Id) return Forbid();

            if (isAccept)
            {
                request.Status = 1; // Chấp nhận
                await _context.SaveChangesAsync();
                return Ok("Đã chấp nhận");
            }
            else
            {
                _context.Friends.Remove(request); // Từ chối -> Xóa
                await _context.SaveChangesAsync();
                return Ok("Đã từ chối");
            }
        }
    }
}