using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace VSMSWebServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class LogsController : ControllerBase
    {
        private readonly string _logsPath;

        public LogsController(IWebHostEnvironment env)
        {
            // Папка logs в корне проекта (ContentRootPath)
            _logsPath = Path.Combine(env.ContentRootPath, "logs");

            // Если папки нет – создаём
            if (!Directory.Exists(_logsPath))
                Directory.CreateDirectory(_logsPath);
        }

        // GET: api/logs
        // Возвращает список имён файлов в папке logs
        [HttpGet]
        public IActionResult GetFiles()
        {
            try
            {
                var files = Directory.GetFiles(_logsPath)
                    .Select(Path.GetFileName)
                    .ToList();
                return Ok(files);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ошибка чтения папки: {ex.Message}");
            }
        }

        // GET: api/logs/{filename}
        // Возвращает содержимое указанного текстового файла
        [HttpGet("{filename}")]
        public async Task<IActionResult> GetFile(string filename)
        {
            if (string.IsNullOrWhiteSpace(filename))
                return BadRequest("Имя файла не может быть пустым.");

            // Защита от path traversal – строим полный путь и проверяем, что он внутри _logsPath
            var basePath = Path.GetFullPath(_logsPath) + Path.DirectorySeparatorChar;
            var fullPath = Path.GetFullPath(Path.Combine(_logsPath, filename));

            if (!fullPath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
                return BadRequest("Недопустимое имя файла.");

            if (!System.IO.File.Exists(fullPath))
                return NotFound($"Файл '{filename}' не найден.");

            try
            {
                using var stream = new FileStream(
                    fullPath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite // 
                );

                using var reader = new StreamReader(stream);
                var content = await reader.ReadToEndAsync();

                return Content(content, "text/plain", System.Text.Encoding.UTF8);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ошибка чтения файла: {ex.Message}");
            }
        }
    }
}
