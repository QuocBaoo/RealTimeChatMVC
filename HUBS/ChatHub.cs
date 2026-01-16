using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using RealTimeChatMVC.Data;
using RealTimeChatMVC.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace RealTimeChatMVC.Hubs
{
    [Authorize] // Bắt buộc đăng nhập mới được vào
    public class ChatHub : Hub
    {
        // 1. Khai báo Database (Để lưu tin nhắn vĩnh viễn)
        private readonly ChatDbContext _context;

        // 2. Khai báo Bộ nhớ tạm (RAM) để quản lý danh sách Online và Nhóm
        private static readonly ConcurrentDictionary<string, string> Users = new();

        // Inject Database vào Hub
        public ChatHub(ChatDbContext context)
        {
            _context = context;
        }

        // -----------------------------------------------------------------------
        // PHẦN 1: QUẢN LÝ KẾT NỐI (ON/OFFLINE)
        // -----------------------------------------------------------------------
        public override async Task OnConnectedAsync()
        {
            // Lấy tên người dùng từ Cookie đăng nhập
            string username = Context.User.Identity.Name;

            // Lưu vào danh sách Online
            Users.TryAdd(Context.ConnectionId, username);

            // Báo cho mọi người biết
            await Clients.All.SendAsync("UserJoined", username);

            // [MỚI] Cập nhật danh sách Online cho các nhóm mà user này tham gia
            await UpdateGroupsForUser(username);

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            // Xóa khỏi danh sách Online
            if (Users.TryRemove(Context.ConnectionId, out var username))
            {
                await Clients.All.SendAsync("UserLeft", username);

                // [MỚI] Cập nhật danh sách Online cho các nhóm mà user này vừa thoát
                await UpdateGroupsForUser(username);
            }
            await base.OnDisconnectedAsync(exception);
        }

        // --- HÀM PHỤ TRỢ: Cập nhật trạng thái Online cho các nhóm ---
        private async Task UpdateGroupsForUser(string username)
        {
            // 1. Tìm các nhóm mà user này là thành viên
            var userGroups = await _context.ChatGroups
                .Where(g => g.Members.Any(u => u.Username == username))
                .Include(g => g.Members)
                .ToListAsync();

            // 2. Lấy danh sách user online hiện tại (Global)
            var allOnlineUsernames = Users.Values.Distinct().ToList();

            // 3. Gửi cập nhật cho từng nhóm
            foreach (var group in userGroups)
            {
                var onlineMembers = group.Members
                    .Where(u => allOnlineUsernames.Contains(u.Username))
                    .Select(u => u.Username)
                    .ToList();

                // Gửi cho những người đang xem nhóm này
                await Clients.Group(group.Name).SendAsync("UpdateGroupUsers", onlineMembers);
            }
        }

        // -----------------------------------------------------------------------
        // PHẦN 2: CHAT CHUNG (CÓ LƯU DATABASE - QUAN TRỌNG)
        // -----------------------------------------------------------------------
        public async Task SendMessage(string user, string message)
        {
            // Để an toàn, ta lấy tên thật từ hệ thống đăng nhập
            string realUser = Context.User.Identity.Name;

            // 1. Lưu vào SQL Server (Phần của Dũng)
            var msgEntity = new Message
            {
                SenderName = realUser,
                Content = message,
                Timestamp = DateTime.Now,
                ChatGroupId = null // Rõ ràng đây là chat chung
            };

            _context.Messages.Add(msgEntity);
            await _context.SaveChangesAsync(); // Lưu vĩnh viễn

            // 2. Gửi ra cho mọi người (Kèm giờ giấc)
            // Format gửi về: User, Message, Time (Khớp với chat.js mới nhất)
            await Clients.All.SendAsync("ReceiveMessage", realUser, message, msgEntity.Timestamp.ToString("HH:mm"));
        }

        // -----------------------------------------------------------------------
        // PHẦN 3: TÍNH NĂNG NÂNG CAO (GROUP, PRIVATE, STICKER)
        // (Tạm thời xử lý trên RAM, chưa lưu SQL để tránh lỗi DB phức tạp)
        // -----------------------------------------------------------------------

        // Gửi Sticker
        public async Task SendSticker(string user, string stickerUrl)
        {
            await Clients.All.SendAsync("ReceiveMessage", user, stickerUrl, DateTime.Now.ToString("HH:mm"));
        }

        // Chat Riêng (Private)
        public async Task SendPrivateMessage(string toUser, string message)
        {
            string fromUser = Context.User.Identity.Name;

            // Tìm ConnectionId của người nhận
            var targetConn = Users.FirstOrDefault(u => u.Value == toUser).Key;

            if (!string.IsNullOrEmpty(targetConn))
            {
                // Gửi cho người nhận
                await Clients.Client(targetConn).SendAsync("ReceivePrivateMessage", fromUser, message, DateTime.Now.ToString("HH:mm"));
                // Gửi lại cho chính mình (để hiện lên màn hình mình)
                await Clients.Caller.SendAsync("ReceivePrivateMessage", fromUser, message, DateTime.Now.ToString("HH:mm"));
            }
        }

        // --- TÍNH NĂNG ROOMS (MỚI) ---
        public async Task JoinRoom(string groupName)
        {
            // Thêm ConnectionId hiện tại vào nhóm SignalR
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

            // --- LOGIC MỚI: Lấy danh sách thành viên Online của nhóm ---
            var group = await _context.ChatGroups
                .Include(g => g.Members)
                .FirstOrDefaultAsync(g => g.Name == groupName);

            if (group != null)
            {
                // Lấy tất cả Username đang online trong hệ thống (từ biến Users static)
                var allOnlineUsernames = Users.Values.Distinct().ToList();

                // Lọc: Chỉ lấy những thành viên của nhóm mà đang có mặt trong danh sách Online
                var onlineMembers = group.Members
                    .Where(u => allOnlineUsernames.Contains(u.Username))
                    .Select(u => u.Username)
                    .ToList();

                // [SỬA] Gửi cho TOÀN BỘ NHÓM để mọi người đều thấy thành viên mới vừa vào
                await Clients.Group(groupName).SendAsync("UpdateGroupUsers", onlineMembers);
            }
        }

        // Chat trong nhóm
        public async Task SendGroupMessage(string groupName, string message)
        {
            string user = Context.User.Identity.Name;

            // 1. Tìm nhóm để lấy ID và Lưu vào DB
            var group = await _context.ChatGroups.FirstOrDefaultAsync(g => g.Name == groupName);
            if (group != null)
            {
                var msgEntity = new Message
                {
                    SenderName = user,
                    Content = message,
                    Timestamp = DateTime.Now,
                    ChatGroupId = group.Id // Lưu trực tiếp ID nhóm (An toàn hơn gán object)
                };
                _context.Messages.Add(msgEntity);
                await _context.SaveChangesAsync();
            }

            // 2. Gửi tin nhắn realtime cho các thành viên trong nhóm
            await Clients.Group(groupName).SendAsync("ReceiveGroupMessage", user, groupName, message, DateTime.Now.ToString("HH:mm"));
        }
    }
}