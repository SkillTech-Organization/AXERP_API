using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using System.Net;

namespace AXERP.API.Functions.Diagnostics
{
    public class DiagnosticFunctions
    {
        private readonly ILogger<DiagnosticFunctions> _logger;

        public DiagnosticFunctions(ILogger<DiagnosticFunctions> logger)
        {
            _logger = logger;
        }

        [Function(nameof(GetVersionInfo))]
        [OpenApiOperation(operationId: nameof(GetVersionInfo), tags: new[] { "diagnostics" })]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/json", bodyType: typeof(string), Description = "The OK response")]
        public IActionResult GetVersionInfo([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req)
        {
            _logger.LogInformation("Calling {name}", nameof(GetVersionInfo));

            return new OkObjectResult(new { NETRunTimeVersion = Environment.Version, AppVersion = GetType().Assembly.GetName().Version });
        }
    }
}
