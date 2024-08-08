using AXERP.API.AppInsightsHelper.Models;
using AXERP.API.Domain.ServiceContracts.Requests;
using Azure;
using Azure.Identity;
using Azure.Monitor.Query;

namespace AXERP.API.AppInsightsHelper.Managers
{
    public class AppInsightsManager
    {
        private readonly LogsQueryClient _logsQueryClient;

        public AppInsightsManager()
        {
            _logsQueryClient = new LogsQueryClient(new DefaultAzureCredential());
        }

        public async Task<List<AppInsightsLogEntry>> QueryLogs(PagedQueryRequest request)
        {
            var entries = new List<AppInsightsLogEntry>();

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

            // Query TOP 10 resource groups by event count
            Response<IReadOnlyList<AppInsightsLogEntry>> response = await _logsQueryClient.QueryWorkspaceAsync<AppInsightsLogEntry>(
                workspaceId,
                "AzureActivity | summarize Count = count() by ResourceGroup | top 10 by Count",
                new QueryTimeRange(TimeSpan.FromDays(1)));

            foreach (var logEntryModel in response.Value)
            {
                Console.WriteLine($"{logEntryModel.RowNumber}, {logEntryModel.Message}, {logEntryModel.TimeStamp}");
            }

            return entries;
        }
    }
}
