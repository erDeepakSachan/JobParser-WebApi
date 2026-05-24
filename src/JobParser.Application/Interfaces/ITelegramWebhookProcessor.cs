using JobParser.Application.DTOs.Jobs;
using JobParser.Application.DTOs.Telegram;

namespace JobParser.Application.Interfaces;

public interface ITelegramWebhookProcessor
{
    Task<WebhookProcessingResultDto> ProcessAsync(TelegramUpdateDto update, CancellationToken cancellationToken);
}