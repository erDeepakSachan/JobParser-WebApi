using JobParser.Domain.Entities;

namespace JobParser.Application.Interfaces;

public interface IJobDetailsRepository
{
    Task<int> InsertManyAsync(IEnumerable<JobDetail> jobs, CancellationToken cancellationToken);
}