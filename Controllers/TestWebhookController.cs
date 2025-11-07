using CrudCloud.api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CrudCloud.api.Controllers;

[ApiController]
[Route("api/test-webhooks")]
public class TestWebhookController : ControllerBase
{
    private readonly IDiscordWebhookService _discordService;

    public TestWebhookController(IDiscordWebhookService discordService)
    {
        _discordService = discordService;
    }

    [HttpPost("user-created")]
    public async Task<IActionResult> TestUserCreated()
    {
        await _discordService.SendUserCreatedAsync("test@email.com", "usr_12345", DateTime.UtcNow);
        return Ok("✅ User created webhook sent to Discord");
    }

    [HttpPost("database-created")]
    public async Task<IActionResult> TestDatabaseCreated()
    {
        await _discordService.SendDatabaseCreatedAsync("mysql-db-001", "MySQL", "usr_12345", "Test User");
        return Ok("✅ Database created webhook sent to Discord");
    }

    [HttpPost("database-deleted")]
    public async Task<IActionResult> TestDatabaseDeleted()
    {
        await _discordService.SendDatabaseDeletedAsync("postgres-db-002", "PostgreSQL", "usr_12345", "Test User");
        return Ok("✅ Database deleted webhook sent to Discord");
    }

    [HttpPost("error")]
    public async Task<IActionResult> TestError()
    {
        await _discordService.SendErrorAsync("NullReferenceException: Object reference not set", "at UserService.CreateUser()", "/api/users", "usr_12345");
        return Ok("✅ Error webhook sent to Discord");
    }

    [HttpPost("plan-updated")]
    public async Task<IActionResult> TestPlanUpdated()
    {
        await _discordService.SendPlanUpdatedAsync("user@example.com", "usr_12345", "Free", "Pro");
        return Ok("✅ Plan updated webhook sent to Discord");
    }

    [HttpPost("email-sent")]
    public async Task<IActionResult> TestEmailSent()
    {
        await _discordService.SendEmailSentAsync("user@example.com", "Welcome Email", true);
        return Ok("✅ Email sent webhook sent to Discord");
    }

    [HttpPost("email-failed")]
    public async Task<IActionResult> TestEmailFailed()
    {
        await _discordService.SendEmailSentAsync("user@example.com", "Password Reset", false, "SMTP connection timeout");
        return Ok("✅ Email failed webhook sent to Discord");
    }
}