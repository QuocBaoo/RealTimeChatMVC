using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.IO;
using System.Threading.Tasks;

namespace RealTimeChatMVC.Controllers
{
    [Authorize]
    public class FilesController : Controller
    {
        private const long MAX_FILE_SIZE = 50 * 1024 * 1024; // 50MB (tăng từ 10MB)
        private readonly string _uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");

        public FilesController()
        {
            // Đảm bảo thư mục upload tồn tại
            if (!Directory.Exists(_uploadPath))
                Directory.CreateDirectory(_uploadPath);
        }

        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            try
            {
                // Kiểm tra file có tồn tại không
                if (file == null || file.Length == 0)
                    return BadRequest(new { success = false, message = "Không có file nào được chọn" });

                // Kiểm tra kích thước file
                if (file.Length > MAX_FILE_SIZE)
                    return BadRequest(new { success = false, message = "File quá lớn (tối đa 50MB)" });

                // Tạo tên file an toàn
                string filename = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
                string filepath = Path.Combine(_uploadPath, filename);

                // Lưu file
                using (var stream = new FileStream(filepath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                return Ok(new { success = true, filename = filename, url = $"/uploads/{filename}" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Lỗi upload: {ex.Message}" });
            }
        }
    }
}
