using Azure;
using Azure.Identity;
using Azure.Monitor.Query;
using Azure.Monitor.Query.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AXERP.API.AppInsightsHelper.Managers
{
    public class AppInsightsManager
    {
        private readonly LogsQueryClient _logsQueryClient;

        public AppInsightsManager()
        {
            _logsQueryClient = new LogsQueryClient(new DefaultAzureCredential());
        }

        public async Task QueryLogs()
        {
            /*
            LogsQueryResult
            |---Error
            |---Status
            |---Table
                |---Name
                |---Columns (list of `LogsTableColumn` objects)
                    |---Name
                    |---Type
                |---Rows (list of `LogsTableRows` objects)
                    |---Count
            |---AllTables (list of `LogsTable` objects)
            */
            string workspaceId = "<workspace_id>";

            Response<LogsQueryResult> result = await _logsQueryClient.QueryWorkspaceAsync(
                workspaceId,
                "AzureActivity | top 10 by TimeGenerated",
                new QueryTimeRange(TimeSpan.FromDays(1))
            );

            LogsTable table = result.Value.Table;

            foreach (var row in table.Rows)
            {
                Console.WriteLine($"{row["OperationName"]} {row["ResourceGroup"]}");
            }
        }
    }
}
