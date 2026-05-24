using JobParser.Application.DTOs.Jobs;

namespace JobParser.Application.Interfaces;

public interface IJobParsingService
{
    Task<IReadOnlyList<ParsedJobDto>> ParseAsync(string rawMessage, CancellationToken cancellationToken);
}