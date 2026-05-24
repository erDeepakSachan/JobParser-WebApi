using JobParser.Application.DTOs.Jobs;
using JobParser.Application.DTOs.Telegram;
using JobParser.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace JobParser.Application.Services;

public sealed class TelegramWebhookProcessor : ITelegramWebhookProcessor
{
    private readonly IJobParsingService _jobParsingService;
    private readonly IJobDetailsRepository _repository;
    private readonly JobDetailMapper _mapper;
    private readonly ILogger<TelegramWebhookProcessor> _logger;
    private readonly IConfiguration _configuration;

    public TelegramWebhookProcessor(
        IJobParsingService jobParsingService,
        IJobDetailsRepository repository,
        JobDetailMapper mapper,
        ILogger<TelegramWebhookProcessor> logger,
        IConfiguration configuration)
    {
        _jobParsingService = jobParsingService;
        _repository = repository;
        _mapper = mapper;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<WebhookProcessingResultDto> ProcessAsync(TelegramUpdateDto update, CancellationToken cancellationToken)
    {
        var rawMessage = update.Message?.Text ?? update.Message?.Caption;

        if (string.IsNullOrWhiteSpace(rawMessage))
        {
            _logger.LogInformation("Telegram update {UpdateId} ignored because message is empty.", update.UpdateId);

            return new WebhookProcessingResultDto
            {
                Success = true,
                UpdateId = update.UpdateId,
                ParsedJobs = 0,
                InsertedJobs = 0,
                Message = "No text/caption found."
            };
        }

        var maxLength = int.TryParse(_configuration["Processing:MaxMessageLength"], out var len) ? len : 20000;
        if (rawMessage.Length > maxLength)
            rawMessage = rawMessage[..maxLength];

        _logger.LogInformation("Processing Telegram update {UpdateId}. Length={Length}", update.UpdateId, rawMessage.Length);

        var parsedJobs = await _jobParsingService.ParseAsync(rawMessage, cancellationToken);
        var mappedJobs = parsedJobs.Select(dto => _mapper.Map(dto, rawMessage)).ToList();

        var inserted = await _repository.InsertManyAsync(mappedJobs, cancellationToken);

        _logger.LogInformation("Update {UpdateId} processed. Parsed={ParsedCount}, Inserted={InsertedCount}",
            update.UpdateId, mappedJobs.Count, inserted);

        return new WebhookProcessingResultDto
        {
            Success = true,
            UpdateId = update.UpdateId,
            ParsedJobs = mappedJobs.Count,
            InsertedJobs = inserted,
            Message = "Processed successfully.",
            Jobs = parsedJobs
        };
    }
}