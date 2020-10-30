using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Data.SqlClient;
using System.Collections.Generic;
using System.Web.Http;

namespace GetTeamDetails
{
    public static class GetTeamDetails
    {
        [FunctionName("GetTeamDetails")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            List<TeamDetail> teamDetailsList = new List<TeamDetail>();
            try
            {
                log.LogInformation("Establishing DB Connection !!");
                var connectionStr = Environment.GetEnvironmentVariable("sqldb_connection");

                using (SqlConnection connection = new SqlConnection(connectionStr))
                {
                    connection.Open();
                    using (SqlCommand command = new SqlCommand(Query.GetUniqueTeamName, connection))
                    {
                        var rows = await command.ExecuteReaderAsync();
                        while (await rows.ReadAsync())
                        {
                            teamDetailsList.Add(new TeamDetail { TeamName = await rows.GetFieldValueAsync<string>(0) });
                        }
                        await rows.CloseAsync();
                        await command.DisposeAsync();
                    }

                    foreach (var team in teamDetailsList)
                    {
                        team.Associates = new List<string>();
                        // fetch all associates of this account
                        var query = $"select FirstName, LastName from Team{team.TeamName}";
                        using (SqlCommand command = new SqlCommand(query, connection))
                        {
                            var rows = await command.ExecuteReaderAsync();
                            while (await rows.ReadAsync())
                            {
                                var firstName = await rows.GetFieldValueAsync<string>(0);
                                var lastName = await rows.GetFieldValueAsync<string>(1);
                                team.Associates.Add($"{firstName} {lastName}");
                            }
                            await rows.CloseAsync();
                            await command.DisposeAsync();
                        }
                    }
                    await connection.DisposeAsync();
                }

                var response = new
                {
                    teamDetailsList,
                };
                return new OkObjectResult(response);
            }
            catch (Exception ex)
            {
                log.LogError(ex.Message);
                return new InternalServerErrorResult();
            }
        }
    }
}
