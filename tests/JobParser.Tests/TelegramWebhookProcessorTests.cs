using FluentAssertions;
using JobParser.Application.DTOs.Jobs;
using JobParser.Application.DTOs.Telegram;
using JobParser.Application.Interfaces;
using JobParser.Application.Services;
using JobParser.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace JobParser.Tests;

public class TelegramWebhookProcessorTests
{
    [Fact]
    public async Task ProcessAsync_Should_Insert_Mapped_Jobs()
    {
        var parsingService = new Mock<IJobParsingService>();
        var repository = new Mock<IJobDetailsRepository>();

        parsingService
            .Setup(x => x.ParseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ParsedJobDto>
            {
                new()
                {
                    CompanyName = "ABC",
                    JobLocation = "Noida",
                    Qualification = "10th",
                    IsITJob = false,
                    ContactNumber = "9650495004",
                    InterviewTime = "8:00 AM"
                }
            });

        repository
            .Setup(x => x.InsertManyAsync(It.IsAny<IEnumerable<JobDetail>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Processing:DefaultInterviewTime"] = "08:00 AM",
                ["Processing:DefaultInterviewLocation"] = "Please call to given number to know the address"
            })
            .Build();

        var mapper = new JobDetailMapper(config);

        var processor = new TelegramWebhookProcessor(
            parsingService.Object,
            repository.Object,
            mapper,
            NullLogger<TelegramWebhookProcessor>.Instance,
            config);

        var update = new TelegramUpdateDto
        {
            UpdateId = 1,
            Message = new TelegramMessageDto
            {
                MessageId = 10,
                Text = "ABC job post"
            }
        };

        var result = await processor.ProcessAsync(update, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.ParsedJobs.Should().Be(1);
        result.InsertedJobs.Should().Be(1);

        repository.Verify(x => x.InsertManyAsync(It.IsAny<IEnumerable<JobDetail>>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}