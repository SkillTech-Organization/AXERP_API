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
            _logsQueryClient = new LogsQueryClient(
                new DefaultAzureCredential()
            );
        }

        public async Task<(int, List<AppInsightsLogEntry>)> QueryLogs(PagedAppInsightsQueryRequest request, string workSpaceId)
        {
            var entries = new List<AppInsightsLogEntry>();

            var orderBy = request.OrderBy;
            var orderByMode = request.OrderDesc ? "desc" : "asc";
            var rowStart = request.RowNumberStart;
            var rowFinish = request.RowNumberFinish;
            var extra_where = string.Empty;

            var count_query = @"
            traces
            | project customDimensions
            | where customDimensions has 'AzureFunctions_FunctionName' and customDimensions['CategoryName'] has 'AXERP.API'
            | summarize Count=count()
            ";

            var query = $@"
            traces
            | project message, timestamp, customDimensions
            | order by {orderBy} {orderByMode}
            | where customDimensions has 'AzureFunctions_FunctionName' and customDimensions['CategoryName'] has 'AXERP.API'
            {extra_where}
            // squishes things down to 1 row where ach column is a huge list of values
            | summarize
                // make up a row number
                rowNum = range(1, 1000000, 1),
                datestamp=make_list(timestamp, 1000000),
                msg=make_list(message, 1000000)
            // expand single rows into real rows
            | mv-expand datestamp, msg, rowNum limit 1000000
            | where rowNum >= {rowStart} and rowNum  <= {rowFinish} and isnotempty(msg)  // for pagination
            ";

            var timeRange = new QueryTimeRange(request.From, request.To);

            Response<IReadOnlyList<int>> countResponse = await _logsQueryClient.QueryWorkspaceAsync<int>(
                workSpaceId,
                count_query,
                timeRange
            );
            var count = countResponse.Value.FirstOrDefault();

            Response<IReadOnlyList<AppInsightsLogEntry>> response = await _logsQueryClient.QueryWorkspaceAsync<AppInsightsLogEntry>(
                workSpaceId,
                query,
                timeRange
            );

            //foreach (var logEntryModel in response.Value)
            //{
            //    Console.WriteLine($"{logEntryModel.RowNumber}, {logEntryModel.Message}, {logEntryModel.TimeStamp}");
            //}

            return (count, entries);
        }
    }
}
