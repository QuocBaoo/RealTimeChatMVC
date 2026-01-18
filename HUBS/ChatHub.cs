using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using RealTimeChatMVC.Data;
using RealTimeChatMVC.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RealTimeChatMVC.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly ChatDbContext _context;

        // ConnectionId -> Username
        private static readonly ConcurrentDictionary<string, string> Users = new();

        // UserId -> ConnectionIds (multi-tab / multi-device)
        private static readonly ConcurrentDictionary<int, ConcurrentDictionary<string, byte>> UserConnections = new();

        public ChatHub(ChatDbContext context)
        {
            _context = context;
        }

        // ================= ONLINE USERS =================
        public override async Task OnConnectedAsync()
        {
            string username = Context.User?.Identity?.Name;
            if (string.IsNullOrEmpty(username))
                throw new HubException("Unauthenticated");

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null)
                throw new HubException("User not found");

            Users.TryAdd(Context.ConnectionId, username);

            UserConnections.AddOrUpdate(
                user.Id,
                _ => new ConcurrentDictionary<string, byte>(
                    new[] { new KeyValuePair<string, byte>(Context.ConnectionId, 0) }
                ),
                (_, dict) =>
                {
                    dict.TryAdd(Context.ConnectionId, 0);
                    return dict;
                });

            bool exists = await _context.OnlineUsers
                .AnyAsync(o => o.ConnectionId == Context.ConnectionId);

            if (!exists)
            {
                _context.OnlineUsers.Add(new OnlineUser
                {
                    ConnectionId = Context.ConnectionId,
                    UserId = user.Id,
                    Username = username
                });

                await _context.SaveChangesAsync();
            }

            await Clients.All.SendAsync("UserJoined", username, user.Id);
            await SendOnlineUsersSnapshot();

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            Users.TryRemove(Context.ConnectionId, out _);

            var onlineEntry = await _context.OnlineUsers
                .FirstOrDefaultAsync(o => o.ConnectionId == Context.ConnectionId);

            if (onlineEntry != null)
            {
                bool shouldNotifyOffline = true;

                if (UserConnections.TryGetValue(onlineEntry.UserId, out var conns))
                {
                    conns.TryRemove(Context.ConnectionId, out _);

                    if (!conns.IsEmpty)
                        shouldNotifyOffline = false;
                    else
                        UserConnections.TryRemove(onlineEntry.UserId, out _);
                }

                _context.OnlineUsers.Remove(onlineEntry);
                await _context.SaveChangesAsync();

                if (shouldNotifyOffline)
                {
                    await Clients.All.SendAsync("UserLeft", onlineEntry.Username);
                }

                await SendOnlineUsersSnapshot();
            }

            await base.OnDisconnectedAsync(exception);
        }

        // ================= SNAPSHOT ONLINE USERS =================
        private async Task SendOnlineUsersSnapshot()
        {
            var users = UserConnections.Keys
                .Join(
                    _context.Users,
                    id => id,
                    u => u.Id,
                    (_, u) => new { u.Id, u.Username }
                )
                .ToList();

            await Clients.All.SendAsync("OnlineUsersSnapshot", users);
        }

        // ================= SEARCH USER =================
        public async Task CheckUserById(int userId)
        {
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                await Clients.Caller.SendAsync("UserCheckResult", new { exists = false });
                return;
            }

            bool isOnline = UserConnections.ContainsKey(userId);

            await Clients.Caller.SendAsync("UserCheckResult", new
            {
                exists = true,
                online = isOnline,
                username = user.Username
            });
        }

        // ================= PUBLIC CHAT =================
        // [FIX] Đổi tên thành SendMessage và thêm tham số user (dù không dùng) để khớp với Client gọi lên
        public async Task SendMessage(string user, string message, string type = "Text")
        {
            if (string.IsNullOrEmpty(Context.User?.Identity?.Name))
                throw new HubException("Unauthenticated");

            try
            {
                string sender = Context.User.Identity.Name; // Luôn lấy từ Context để bảo mật
                DateTime time = DateTime.UtcNow.AddHours(7);

                _context.Messages.Add(new Message
                {
                    SenderName = sender,
                    Content = message,
                    Timestamp = time,
                    ChatGroupId = null,
                    Type = type
                });

                await _context.SaveChangesAsync();

                await Clients.All.SendAsync(
                    "ReceiveMessage",
                    sender,
                    message,
                    time.ToString("HH:mm:ss"),
                    type
                );

                await Clients.Caller.SendAsync("SendMessageAck", true);
            }
            catch
            {
                await Clients.Caller.SendAsync("SendMessageAck", false);
                throw;
            }
        }

        // ================= PRIVATE CHAT =================
        public async Task SendPrivateMessage(int toUserId, string message)
        {
            if (string.IsNullOrEmpty(Context.User?.Identity?.Name))
                throw new HubException("Unauthenticated");

            string fromUser = Context.User.Identity.Name;
            DateTime time = DateTime.UtcNow.AddHours(7);

            if (!UserConnections.TryGetValue(toUserId, out var connections))
            {
                await Clients.Caller.SendAsync("PrivateMessageError", "UserOffline");
                return;
            }

            // ✅ Save DB
            _context.Messages.Add(new Message
            {
                SenderName = fromUser,
                Content = message,
                Timestamp = time,
                ToUserId = toUserId,
                Type = "Private"
            });

            await _context.SaveChangesAsync();

            await Clients.Clients(connections.Keys.ToList())
                .SendAsync("ReceivePrivateMessage", fromUser, message, time.ToString("HH:mm:ss"));

            // Self delivery
            await Clients.Caller.SendAsync(
                "ReceivePrivateMessage",
                fromUser,
                message,
                time.ToString("HH:mm:ss")
            );

            await Clients.Caller.SendAsync("SendPrivateMessageAck", true);
        }

        // ================= GROUP CHAT =================
        public async Task JoinRoom(string groupName)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

            var group = await _context.ChatGroups
                .Include(g => g.Members)
                .FirstOrDefaultAsync(g => g.Name == groupName);

            if (group == null)
                return;

            var onlineUsernames = UserConnections.Keys
                .Join(_context.Users,
                    id => id,
                    u => u.Id,
                    (_, u) => u.Username)
                .ToList();

            var onlineMembers = group.Members
                .Where(u => onlineUsernames.Contains(u.Username))
                .Select(u => u.Username)
                .ToList();

            await Clients.Group(groupName)
                .SendAsync("UpdateGroupUsers", onlineMembers);
        }

        public async Task SendGroupMessage(string groupName, string message, string type = "Text")
        {
            if (string.IsNullOrEmpty(Context.User?.Identity?.Name))
                throw new HubException("Unauthenticated");

            string sender = Context.User.Identity.Name;
            DateTime time = DateTime.UtcNow.AddHours(7);

            var group = await _context.ChatGroups
                .FirstOrDefaultAsync(g => g.Name == groupName);

            if (group != null)
            {
                _context.Messages.Add(new Message
                {
                    SenderName = sender,
                    Content = message,
                    Timestamp = time,
                    ChatGroupId = group.Id,
                    Type = type
                });

                await _context.SaveChangesAsync();
            }

            await Clients.Group(groupName)
                .SendAsync(
                    "ReceiveGroupMessage",
                    sender,
                    groupName,
                    message,
                    time.ToString("HH:mm:ss"),
                    type
                );
        }
    }
}
