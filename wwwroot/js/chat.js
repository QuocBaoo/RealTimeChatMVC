"use strict";

var connection = new signalR.HubConnectionBuilder().withUrl("/chatHub").build();

// Tắt nút gửi khi chưa kết nối xong
document.getElementById("sendButton").disabled = true;

// 1. Nhận tin nhắn thường (Text/Sticker)
connection.on("ReceiveMessage", function (msgObj) {
  // msgObj bây giờ là một cục dữ liệu: { sender, content, time, type }
  var li = document.createElement("li");
  document.getElementById("messagesList").appendChild(li);

  // Hiển thị đẹp: [12:30:45] Tên: Nội dung
  li.textContent = `[${msgObj.time}] ${msgObj.sender}: ${msgObj.content}`;
});

// 2. Nhận tin nhắn riêng (Private)
connection.on("ReceivePrivateMessage", function (msgObj) {
  var li = document.createElement("li");
  li.style.color = "red";
  li.style.fontWeight = "bold";
  document.getElementById("messagesList").appendChild(li);
  li.textContent = `[MẬT - ${msgObj.time}] ${msgObj.sender}: ${msgObj.content}`;
});

// 3. Nhận tin nhắn nhóm
connection.on("ReceiveMessage", function (user, message, time) {
  var li = document.createElement("li");
  li.className = "list-group-item";

  // Hiển thị: [10:30] Hùng: Hello anh em
  li.innerHTML = `<strong>[${time}] ${user}:</strong> ${message}`;

  document.getElementById("messagesList").appendChild(li);
});
// Bắt đầu kết nối
connection
  .start()
  .then(function () {
    document.getElementById("sendButton").disabled = false;
  })
  .catch(function (err) {
    return console.error(err.toString());
  });

// Xử lý nút Gửi
document
  .getElementById("sendButton")
  .addEventListener("click", function (event) {
    var user = document.getElementById("userInput").value;
    var message = document.getElementById("messageInput").value;

    // Gọi hàm SendMessage bên Hub
    connection.invoke("SendMessage", user, message).catch(function (err) {
      return console.error(err.toString());
    });
    event.preventDefault();
  });
