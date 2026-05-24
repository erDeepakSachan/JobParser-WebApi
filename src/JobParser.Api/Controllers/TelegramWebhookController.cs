using JobParser.Application.DTOs.Telegram;
using JobParser.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace JobParser.Api.Controllers;

[ApiController]
[Route("api/telegram")]
public sealed class TelegramWebhookController : ControllerBase
{
    private readonly ITelegramWebhookProcessor _processor;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TelegramWebhookController> _logger;

    public TelegramWebhookController(
        ITelegramWebhookProcessor processor,
        IConfiguration configuration,
        ILogger<TelegramWebhookController> logger)
    {
        _processor = processor;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpPost("webhook")]
    public IActionResult Receive([FromBody] TelegramUpdateDto update)
    {
        try
        {
            ValidateTelegramSecret(Request);

            if (!IsAllowedChat(update))
                return Unauthorized(new { message = "Chat not allowed." });

            // Hand off processing to a background task
            Task.Run(async () =>
            {
                try
                {
                    var result = await _processor.ProcessAsync(update, CancellationToken.None);
                    _logger.LogInformation("Webhook response for UpdateId={UpdateId}: {@Result}", update.UpdateId, result);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Telegram webhook processing failed for UpdateId={UpdateId}", update?.UpdateId);
                }
            });

            // Immediately return 200 OK so Telegram doesn’t retry
            return Ok();
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized Telegram webhook call.");
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Telegram webhook validation failed for UpdateId={UpdateId}", update?.UpdateId);
            return StatusCode(500, new { message = "Validation failed." });
        }
    }

    private void ValidateTelegramSecret(HttpRequest request)
    {
        var expected = _configuration["Telegram:WebhookSecretToken"];
        if (string.IsNullOrWhiteSpace(expected))
            return;

        //if (!request.Headers.TryGetValue("X-Telegram-Bot-Api-Secret-Token", out var incoming))
        //    throw new UnauthorizedAccessException("Missing Telegram secret token.");

        //if (!string.Equals(incoming.ToString(), expected, StringComparison.Ordinal))
        //    throw new UnauthorizedAccessException("Invalid Telegram secret token.");
    }

    private bool IsAllowedChat(TelegramUpdateDto update)
    {
        var allowedChatId = _configuration["Telegram:AllowedChatId"];
        if (string.IsNullOrWhiteSpace(allowedChatId))
            return true;

        var incomingChatId = update.Message?.Chat?.Id.ToString();
        return string.Equals(allowedChatId, incomingChatId, StringComparison.Ordinal);
    }
}