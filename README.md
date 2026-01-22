# 💬 RealTimeChatMVC

>
>
> Ứng dụng chat thời gian thực đa nền tảng, tốc độ cao, sử dụng công nghệ SignalR.

---

## 🚀 Tính Năng Chính
*   **Chat 1-1 & Chat Nhóm:** Gửi tin nhắn tức thì, độ trễ cực thấp.
*   **Trạng Thái Online:** Cập nhật real-time ai đang online/offline.
*   **Chia Sẻ File:** Hỗ trợ gửi ảnh, video, tài liệu lên đến 100MB.
*   **Thông Báo:** Nhận thông báo tin nhắn ngay lập tức.
*   **Bảo Mật:** Xác thực người dùng an toàn.

---

## 💻 Công Nghệ & Môi Trường

| Thành Phần | Công Nghệ / Công Cụ |
| :--- | :--- |
| **Backend** | ASP.NET Core 8.0, C# |
| **Real-time** | SignalR (WebSocket) |
| **Frontend** | Razor Views, JavaScript, Bootstrap 5 |
| **Database** | SQL Server 2022, Entity Framework Core |
| **Hạ Tầng** | Docker, Docker Compose |
| **IDE** | VS Code (Visual Studio Code) |

> **Kiến thức áp dụng:** WebSocket, Xử lý đồng thời (Concurrency), Lập trình bất đồng bộ (Async/Await), Quản lý State.

---

## ⚙️ Hướng Dẫn Cài Đặt & Chạy (VS Code)

Bạn có thể chạy dự án dễ dàng theo 2 cách dưới đây:

### 🌟 Cách 1: Chạy Bằng Docker (Khuyên Dùng)
Cách này nhanh nhất, không cần cài đặt SQL Server hay môi trường phức tạp.

1.  **Mở Terminal** trong VS Code (`Ctrl + `).
2.  **Khởi chạy** bằng lệnh sau:
    ```bash
    docker-compose up -d --build
    ```
3.  **Truy cập**: Mở trình duyệt vào `http://localhost:5000`

### 🛠️ Cách 2: Chạy Thủ Công (Dotnet CLI)
Dành cho việc phát triển (Dev) và Debug trực tiếp.

1.  **Cấu hình Database**:
    *   Mở file `appsettings.json`.
    *   Sửa chuỗi kết nối `DefaultConnection` cho đúng với SQL Server của bạn.
2.  **Mở Terminal** trong VS Code.
3.  **Khởi tạo Database**:
    ```bash
    dotnet ef database update
    ```
4.  **Chạy dự án**:
    ```bash
    dotnet run
    ```
5.  **Truy cập**: Vào địa chỉ `https://localhost:7123` (hoặc port hiển thị trên màn hình console).

---

## 📂 Cấu Trúc Dự Án
*   `Hubs/ChatHub.cs`: Trái tim xử lý tín hiệu Real-time.
*   `Controllers/`: Xử lý Logic API và điều hướng.
*   `Views/`: Giao diện người dùng.
*   `wwwroot/`: File tĩnh (CSS, JS, Ảnh).


*
