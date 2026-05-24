using System.Data;
using JobParser.Application.Interfaces;
using JobParser.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MySqlConnector;

namespace JobParser.Infrastructure.Persistence;

public sealed class JobDetailsRepository : IJobDetailsRepository
{
    private readonly string _connectionString;
    private readonly ILogger<JobDetailsRepository> _logger;

    public JobDetailsRepository(IConfiguration configuration, ILogger<JobDetailsRepository> logger)
    {
        _connectionString = configuration.GetConnectionString("MySql")
            ?? throw new InvalidOperationException("MySql connection string is missing.");
        _logger = logger;
    }

    public async Task<int> InsertManyAsync(IEnumerable<JobDetail> jobs, CancellationToken cancellationToken)
    {
        var list = jobs.ToList();
        if (list.Count == 0) return 0;

        try
        {
            var inserted = 0;

            foreach (var job in list)
            {
                var insertedId = InsertAsync(job, cancellationToken);
            }
            return inserted;
        }
        catch (Exception ex)
        {
            //await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Failed to insert jobs into MySQL.");
            return 0;
        }
    }

    private async Task<bool> InsertAsync(JobDetail job, CancellationToken cancellationToken)
    {
        // First, resolve JobLocationID
        const string lookupSql = "SELECT JobLocationID FROM joblocation WHERE Location = @JobLocationName LIMIT 1";
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var lookupCmd = new MySqlCommand(lookupSql, connection);
        lookupCmd.Parameters.AddWithValue("@JobLocationName", job.JobLocation);

        var locationIdObj = await lookupCmd.ExecuteScalarAsync(cancellationToken);
        int? locationId = locationIdObj != null ? Convert.ToInt32(locationIdObj) : (int?)null;

        const string insertSql = """
                                        INSERT INTO jobdetails 
                                        (CompanyName, JobLocationID, Qualification, Department, IsITJob, 
                                         InterviewDate, InterviewTime, InterviewLocation, ContactNumber, 
                                         Email, OtherDetail)
                                        VALUES 
                                        (@CompanyName, @JobLocationID, @Qualification, @Department, @IsITJob,
                                         @InterviewDate, @InterviewTime, @InterviewLocation, @ContactNumber,
                                         @Email, @OtherDetail);
                                        SELECT LAST_INSERT_ID();
                                        """;

        try
        {



            await using var command = new MySqlCommand(insertSql, connection);

            command.Parameters.AddWithValue("@CompanyName", (object?)job.CompanyName ?? DBNull.Value);
            command.Parameters.AddWithValue("@JobLocationID", (object?)locationId ?? DBNull.Value);
            //command.Parameters.AddWithValue("@JobLocation", (object?)jobDetails.JobLocation ?? DBNull.Value);
            command.Parameters.AddWithValue("@Qualification", (object?)job.Qualification ?? DBNull.Value);
            command.Parameters.AddWithValue("@Department", (object?)job.Department ?? DBNull.Value);
            command.Parameters.AddWithValue("@IsITJob", job.IsITJob);
            command.Parameters.AddWithValue("@InterviewDate", (object?)job.InterviewDate ?? DBNull.Value);
            command.Parameters.AddWithValue("@InterviewTime", (object?)job.InterviewTime ?? DBNull.Value);
            command.Parameters.AddWithValue("@InterviewLocation", (object?)job.InterviewLocation ?? DBNull.Value);
            command.Parameters.AddWithValue("@ContactNumber", (object?)job.ContactNumber ?? DBNull.Value);
            command.Parameters.AddWithValue("@Email", (object?)job.Email ?? DBNull.Value);
            command.Parameters.AddWithValue("@OtherDetail", (object?)job.OtherDetail ?? DBNull.Value);

            var result = await command.ExecuteScalarAsync(cancellationToken);
            var insertedId = Convert.ToInt32(result);
            return true;
        }
        catch (Exception ex)
        {
            //_logger.LogInformation("Duplicate record skipped because ContentHash already exists: {ContentHash}", job.ContentHash);
            return false;
        }
    }
}