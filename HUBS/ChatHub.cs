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
        private static readonly ConcurrentDictionary<string, string> Users = new();

        public ChatHub(ChatDbContext context)
        {
            _context = context;
        }

        // --- QUẢN LÝ KẾT NỐI ---
        public override async Task OnConnectedAsync()
        {
            string username = Context.User.Identity.Name;
            Users.TryAdd(Context.ConnectionId, username);
            await Clients.All.SendAsync("UserJoined", username);
            await UpdateGroupsForUser(username);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (Users.TryRemove(Context.ConnectionId, out var username))
            {
                await Clients.All.SendAsync("UserLeft", username);
                await UpdateGroupsForUser(username);
            }
            await base.OnDisconnectedAsync(exception);
        }

        private async Task UpdateGroupsForUser(string username)
        {
            var userGroups = await _context.ChatGroups
                .Where(g => g.Members.Any(u => u.Username == username))
                .Include(g => g.Members)
                .ToListAsync();

            var allOnlineUsernames = Users.Values.Distinct().ToList();

            foreach (var group in userGroups)
            {
                var onlineMembers = group.Members
                    .Where(u => allOnlineUsernames.Contains(u.Username))
                    .Select(u => u.Username)
                    .ToList();
                await Clients.Group(group.Name).SendAsync("UpdateGroupUsers", onlineMembers);
            }
        }

        // --- CHAT CHUNG ---
        public async Task SendMessage(string user, string message)
        {
            string realUser = Context.User.Identity.Name;

            var msgEntity = new Message
            {
                SenderName = realUser,
                Content = message,
                Timestamp = DateTime.UtcNow.AddHours(7), // Giờ Việt Nam
                ChatGroupId = null
            };

            _context.Messages.Add(msgEntity);
            await _context.SaveChangesAsync();

            await Clients.All.SendAsync("ReceiveMessage", realUser, message, msgEntity.Timestamp.ToString("HH:mm:ss"));
        }

        // --- STICKER & PRIVATE ---
        public async Task SendSticker(string user, string stickerUrl)
        {
            await Clients.All.SendAsync("ReceiveMessage", user, stickerUrl, DateTime.UtcNow.AddHours(7).ToString("HH:mm:ss"));
        }

        public async Task SendPrivateMessage(string toUser, string message)
        {
            string fromUser = Context.User.Identity.Name;
            var targetConn = Users.FirstOrDefault(u => u.Value == toUser).Key;

            if (!string.IsNullOrEmpty(targetConn))
            {
                string time = DateTime.UtcNow.AddHours(7).ToString("HH:mm:ss");
                await Clients.Client(targetConn).SendAsync("ReceivePrivateMessage", fromUser, message, time);
                await Clients.Caller.SendAsync("ReceivePrivateMessage", fromUser, message, time);
            }
        }

        // --- ROOMS & GROUP CHAT ---
        public async Task JoinRoom(string groupName)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

            var group = await _context.ChatGroups
                .Include(g => g.Members)
                .FirstOrDefaultAsync(g => g.Name == groupName);

            if (group != null)
            {
                var allOnlineUsernames = Users.Values.Distinct().ToList();
                var onlineMembers = group.Members
                    .Where(u => allOnlineUsernames.Contains(u.Username))
                    .Select(u => u.Username)
                    .ToList();
                await Clients.Group(groupName).SendAsync("UpdateGroupUsers", onlineMembers);
            }
        }

        public async Task SendGroupMessage(string groupName, string message)
        {
            string user = Context.User.Identity.Name;

            // 1. Lưu vào Database
            var group = await _context.ChatGroups.FirstOrDefaultAsync(g => g.Name == groupName);
            DateTime timeNow = DateTime.UtcNow.AddHours(7);

            if (group != null)
            {
                var msgEntity = new Message
                {
                    SenderName = user,
                    Content = message,
                    Timestamp = timeNow,
                    ChatGroupId = group.Id
                };
                _context.Messages.Add(msgEntity);
                await _context.SaveChangesAsync();
            }

            // 2. Gửi Realtime
            await Clients.Group(groupName).SendAsync("ReceiveGroupMessage", user, groupName, message, timeNow.ToString("HH:mm:ss"));
        }
    }
}