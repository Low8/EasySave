using Microsoft.AspNetCore.Mvc;
using EasyLog;
using EasySave.RemoteLogging;

namespace EasySave.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LogController : ControllerBase
{
    private readonly CentralizedLogStorageService _logService;

    public LogController(CentralizedLogStorageService logService)
    {
        _logService = logService;
    }

    [HttpPost]
    public async Task<IActionResult> PostLog([FromBody] LogEntry logEntry)
    {
        if (logEntry == null)
        {
            return BadRequest("Invalid log entry");
        }

        var result = await _logService.StoreLogAsync(logEntry);
        return result ? Ok() : StatusCode(500, "Failed to store log");
    }

    [HttpPost("batch")]
    public async Task<IActionResult> PostLogs([FromBody] List<LogEntry> logEntries)
    {
        if (logEntries == null || !logEntries.Any())
        {
            return BadRequest("Invalid log entries");
        }

        var result = await _logService.StoreLogsAsync(logEntries);
        return result ? Ok() : StatusCode(500, "Failed to store logs");
    }

    [HttpGet]
    public IActionResult GetLogs([FromQuery] string? date)
    {
        if (string.IsNullOrEmpty(date))
        {
            return BadRequest("Date parameter is required (format: yyyy-MM-dd)");
        }

        if (DateTime.TryParse(date, out var targetDate))
        {
            var logs = _logService.GetLogsForDate(targetDate);
            return Ok(logs);
        }

        return BadRequest("Invalid date format. Use yyyy-MM-dd");
    }

    [HttpGet("range")]
    public IActionResult GetLogsRange([FromQuery] string startDate, [FromQuery] string endDate)
    {
        if (!DateTime.TryParse(startDate, out var start) || 
            !DateTime.TryParse(endDate, out var end))
        {
            return BadRequest("Invalid date format. Use yyyy-MM-dd");
        }

        var logs = _logService.GetLogsForDateRange(start, end);
        return Ok(logs);
    }

    [HttpDelete("cleanup")]
    public IActionResult CleanupOldLogs([FromQuery] int daysToKeep = 30)
    {
        var deletedCount = _logService.DeleteOldLogs(daysToKeep);
        return Ok(new { DeletedCount = deletedCount });
    }
}
