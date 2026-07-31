using Microsoft.AspNetCore.Mvc;

namespace Lanflix.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NotificationsController : ControllerBase
{
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(ILogger<NotificationsController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Get notifications for a profile (stub - not implemented yet)
    /// </summary>
    [HttpGet("{profileId}")]
    public IActionResult GetNotifications([FromRoute] int profileId)
    {
        _logger.LogInformation("Getting notifications for profile {ProfileId}", profileId);
        
        // Return empty notifications for now
        return Ok(new
        {
            notifications = new List<object>()
        });
    }

    /// <summary>
    /// Respond to a notification (stub - not implemented yet)
    /// </summary>
    [HttpPost("{notificationId}/respond")]
    public IActionResult RespondToNotification(
        [FromRoute] int notificationId,
        [FromBody] NotificationResponse response)
    {
        _logger.LogInformation("Responding to notification {NotificationId}: {Response}", 
            notificationId, response.Response);
        
        return Ok(new { message = "Response recorded" });
    }

    /// <summary>
    /// Register device for notifications
    /// </summary>
    [HttpPost("register")]
    public IActionResult RegisterNotificationDevice([FromBody] object body)
    {
        return Ok(new { message = "Notification device registered" });
    }
}

public class NotificationResponse
{
    public string Response { get; set; } = string.Empty;
}
