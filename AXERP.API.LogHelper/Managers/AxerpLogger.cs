using AXERP.API.LogHelper.Attributes;
using AXERP.API.LogHelper.Enums;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace AXERP.API.LogHelper.Managers
{
    public class AxerpLogger<T> : IAxerpLogger where T : class
    {
        private ILogger<T> _logger;

        private string _user;
        private string _system;
        private string _function;

        public string ForSystem => typeof(T).GetCustomAttribute<ForSystemAttribute>()?.SystemName ?? typeof(T).Name;
        public string ForFunction => typeof(T).GetCustomAttribute<ForSystemAttribute>()?.DefaultFunctionName ?? "Unknown Function";

        public long ProcessId { get; private set; }

        private Stopwatch _stopwatch;

        private readonly Dictionary<LogResults, string> ResultToString = new()
        {
            { LogResults.Ok, "Ok" },
            { LogResults.Warning, "Warning" },
            { LogResults.Error, "Error" },
            { LogResults.Debug, "Debug" },
        };

        private const string MESSAGE_TEMPLATE =
            "ProcessId: {ProcessId} | Function: {Function} | System: {System} | When: {When} | Who: {Who} | Description: {Description} | Result: {Result}";

        public AxerpLogger(ILogger<T> logger)
        {
            _logger = logger;
        }

        public void SetLoggerProcessData(string? user = null, string? system = null, string? function = null, long? id = null)
        {
            _user = user ?? "Unknown";
            _system = system ?? ForSystem;
            _function = function ?? ForFunction;
            SetNewId(id);
            _stopwatch = new Stopwatch();
        }

        public void SetNewId(long? id)
        {
            ProcessId = id ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        public long GetNewId()
        {
            return DateTime.UnixEpoch.Ticks;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string RenderMessage(string message, object?[] args)
        {
            var _renderedMessage = string.Empty;

            if (args != null && args.Length > 0)
            {
                _renderedMessage = string.Format(
                    message,
                    args
                );
            }
            else
            {
                _renderedMessage = message;
            }

            return _renderedMessage;
        }

        public void BeginMeasure()
        {
            _stopwatch.Start();
            LogInformation("Begining execution time measurement.");
        }

        public void EndMeasure()
        {
            _stopwatch.Stop();
            LogInformation("Finished measuring execution time. Result: {0}", _stopwatch.Elapsed);
            _stopwatch.Reset();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void LogInformation(int id, string message, params object?[] args)
        {
            string _renderedMessage = RenderMessage(message, args);

            var callerMethod = new StackFrame(1, false).GetMethod()!;
            var _f = callerMethod.GetCustomAttribute<ForFunctionAttribute>()?.FunctionName ?? _function ?? callerMethod.Name;

            _logger.LogInformation(
                MESSAGE_TEMPLATE,
                id, _f, _system, DateTime.UtcNow, _user, _renderedMessage, ResultToString[LogResults.Ok]
            );
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void LogInformation(string message, params object?[] args)
        {
            string _renderedMessage = RenderMessage(message, args);

            var callerMethod = new StackFrame(1, false).GetMethod()!;
            var _f = callerMethod.GetCustomAttribute<ForFunctionAttribute>()?.FunctionName ?? _function ?? callerMethod.Name;

            _logger.LogInformation(
                MESSAGE_TEMPLATE,
                ProcessId, _f, _system, DateTime.UtcNow, _user, _renderedMessage, ResultToString[LogResults.Ok]
            );
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void LogDebug(int id, string message, params object?[] args)
        {
            string _renderedMessage = RenderMessage(message, args);

            var callerMethod = new StackFrame(1, false).GetMethod()!;
            var _f = callerMethod.GetCustomAttribute<ForFunctionAttribute>()?.FunctionName ?? _function ?? callerMethod.Name;

            _logger.LogDebug(
                MESSAGE_TEMPLATE,
                id, _f, _system, DateTime.UtcNow, _user, _renderedMessage, ResultToString[LogResults.Debug]
            );
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void LogDebug(string message, params object?[] args)
        {
            string _renderedMessage = RenderMessage(message, args);

            var callerMethod = new StackFrame(1, false).GetMethod()!;
            var _f = callerMethod.GetCustomAttribute<ForFunctionAttribute>()?.FunctionName ?? _function ?? callerMethod.Name;

            _logger.LogDebug(
                MESSAGE_TEMPLATE,
                ProcessId, _f, _system, DateTime.UtcNow, _user, _renderedMessage, ResultToString[LogResults.Debug]
            );
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void LogWarning(int id, string message, params object?[] args)
        {
            string _renderedMessage = RenderMessage(message, args);

            var callerMethod = new StackFrame(1, false).GetMethod()!;
            var _f = callerMethod.GetCustomAttribute<ForFunctionAttribute>()?.FunctionName ?? _function ?? callerMethod.Name;

            _logger.LogWarning(
                MESSAGE_TEMPLATE,
                id, _f, _system, DateTime.UtcNow, _user, _renderedMessage, ResultToString[LogResults.Warning]
            );
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void LogWarning(string message, params object?[] args)
        {
            string _renderedMessage = RenderMessage(message, args);

            var callerMethod = new StackFrame(1, false).GetMethod()!;
            var _f = callerMethod.GetCustomAttribute<ForFunctionAttribute>()?.FunctionName ?? _function ?? callerMethod.Name;

            _logger.LogWarning(
                MESSAGE_TEMPLATE,
                ProcessId, _f, _system, DateTime.UtcNow, _user, _renderedMessage, ResultToString[LogResults.Warning]
            );
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void LogError(int id, Exception ex, string message, params object?[] args)
        {
            string _renderedMessage = RenderMessage(message, args);
            _renderedMessage += $" - {ex.Message}";

            var callerMethod = new StackFrame(1, false).GetMethod()!;
            var _f = callerMethod.GetCustomAttribute<ForFunctionAttribute>()?.FunctionName ?? _function ?? callerMethod.Name;

            _logger.LogError(
                MESSAGE_TEMPLATE,
                id, _f, _system, DateTime.UtcNow, _user, _renderedMessage, ResultToString[LogResults.Error]
            );
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void LogError(int id, string message, params object?[] args)
        {
            string _renderedMessage = RenderMessage(message, args);

            var callerMethod = new StackFrame(1, false).GetMethod()!;
            var _f = callerMethod.GetCustomAttribute<ForFunctionAttribute>()?.FunctionName ?? _function ?? callerMethod.Name;

            _logger.LogError(
                MESSAGE_TEMPLATE,
                id, _f, _system, DateTime.UtcNow, _user, _renderedMessage, ResultToString[LogResults.Error]
            );
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void LogError(int id, Exception ex)
        {
            var _renderedMessage = ex.ToString();

            var callerMethod = new StackFrame(1, false).GetMethod()!;
            var _f = callerMethod.GetCustomAttribute<ForFunctionAttribute>()?.FunctionName ?? _function ?? callerMethod.Name;

            _logger.LogError(
                MESSAGE_TEMPLATE,
                id, _f, _system, DateTime.UtcNow, _user, _renderedMessage, ResultToString[LogResults.Error]
            );
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void LogError(Exception ex, string message, params object?[] args)
        {
            string _renderedMessage = RenderMessage(message, args);
            _renderedMessage += $" - {ex.Message}";

            var callerMethod = new StackFrame(1, false).GetMethod()!;
            var _f = callerMethod.GetCustomAttribute<ForFunctionAttribute>()?.FunctionName ?? _function ?? callerMethod.Name;

            _logger.LogError(
                MESSAGE_TEMPLATE,
                ProcessId, _f, _system, DateTime.UtcNow, _user, _renderedMessage, ResultToString[LogResults.Error]
            );
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void LogError(string message, params object?[] args)
        {
            string _renderedMessage = RenderMessage(message, args);

            var callerMethod = new StackFrame(1, false).GetMethod()!;
            var _f = callerMethod.GetCustomAttribute<ForFunctionAttribute>()?.FunctionName ?? _function ?? callerMethod.Name;

            _logger.LogError(
                MESSAGE_TEMPLATE,
                ProcessId, _f, _system, DateTime.UtcNow, _user, _renderedMessage, ResultToString[LogResults.Error]
            );
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void LogError(Exception ex)
        {
            var _renderedMessage = ex.ToString();

            var callerMethod = new StackFrame(1, false).GetMethod()!;
            var _f = callerMethod.GetCustomAttribute<ForFunctionAttribute>()?.FunctionName ?? _function ?? callerMethod.Name;

            _logger.LogError(
                MESSAGE_TEMPLATE,
                ProcessId, _f, _system, DateTime.UtcNow, _user, _renderedMessage, ResultToString[LogResults.Error]
            );
        }
    }
}
