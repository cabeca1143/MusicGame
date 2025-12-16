using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MusicGame;

[Route("api/[controller]")]
[ApiController]
public class DownloadController(IWebHostEnvironment env) : ControllerBase
{
    private IWebHostEnvironment _env = env;

    [HttpGet("game-data")]
    [Authorize(Roles = "Admin")]
    public IActionResult DownloadJson()
    {
        string filePath = Path.Combine(_env.WebRootPath, "Current.json");

        if(!System.IO.File.Exists(filePath))
        {
            return NotFound("File not found.");
        }

        byte[] bytes = System.IO.File.ReadAllBytes(filePath);
        return File(bytes, "application/json", "Current.json");
    }
}
