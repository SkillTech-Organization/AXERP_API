using AXERP.API.LogHelper.Enums;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace AXERP.API.LogHelper.Managers
{
    public class AxerpLogger<T>
    {
        private ILogger<T> _logger;

        private string _user;
        private string _system;
        private string _function;

        private long _id;

        private Stopwatch _stopwatch;

        private readonly Dictionary<LogResults, string> ResultToString = new()
        {
            { LogResults.Ok, "Ok" },
            { LogResults.Warning, "Warning" },
            { LogResults.Error, "Error" },
            { LogResults.Debug, "Debug" },
        };

        private const string MESSAGE_TEMPLATE = "{ProcessId}{Function}{System}{When}{Who}{Description}{Result}";

        public AxerpLogger(ILogger<T> logger)
        {
            _logger = logger;
        }

        public void Set(string user, string system)
        {
            _user = user;
            _system = system;
            _id = DateTime.UnixEpoch.Ticks;
            _stopwatch = new Stopwatch();
        }

        public void GenerateNewId()
        {
            _id = DateTime.UnixEpoch.Ticks;
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
            LogInformation(LogResults.Ok, "Begining execution time measurement.");
        }

        public void EndMeasure()
        {
            _stopwatch.Stop();
            LogInformation(LogResults.Ok, "Finished measuring execution time. Result: {0}", _stopwatch.Elapsed);
            _stopwatch.Reset();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void LogInformation(int id, LogResults logResult, string message, params object?[] args)
        {
            string _renderedMessage = RenderMessage(message, args);

            var _f = new StackFrame(1, false).GetMethod().Name;

            _logger.LogInformation(
                MESSAGE_TEMPLATE,
                id, _f, _system, DateTime.UtcNow, _user, _renderedMessage, ResultToString[logResult]
            );
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void LogInformation(LogResults logResult, string message, params object?[] args)
        {
            string _renderedMessage = RenderMessage(message, args);

            var _f = new StackFrame(1, false).GetMethod().Name;

            _logger.LogInformation(
                MESSAGE_TEMPLATE,
                _id, _f, _system, DateTime.UtcNow, _user, _renderedMessage, ResultToString[logResult]
            );
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void LogDebug(int id, string message, params object?[] args)
        {
            string _renderedMessage = RenderMessage(message, args);

            var _f = new StackFrame(1, false).GetMethod().Name;

            _logger.LogDebug(
                MESSAGE_TEMPLATE,
                id, _f, _system, DateTime.UtcNow, _user, _renderedMessage, ResultToString[LogResults.Debug]
            );
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void LogDebug(string message, params object?[] args)
        {
            string _renderedMessage = RenderMessage(message, args);

            var _f = new StackFrame(1, false).GetMethod().Name;

            _logger.LogDebug(
                MESSAGE_TEMPLATE,
                _id, _f, _system, DateTime.UtcNow, _user, _renderedMessage, ResultToString[LogResults.Debug]
            );
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void LogWarning(int id, string message, params object?[] args)
        {
            string _renderedMessage = RenderMessage(message, args);

            var _f = new StackFrame(1, false).GetMethod().Name;

            _logger.LogWarning(
                MESSAGE_TEMPLATE,
                id, _f, _system, DateTime.UtcNow, _user, _renderedMessage, ResultToString[LogResults.Warning]
            );
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void LogWarning(string message, params object?[] args)
        {
            string _renderedMessage = RenderMessage(message, args);

            var _f = new StackFrame(1, false).GetMethod().Name;

            _logger.LogWarning(
                MESSAGE_TEMPLATE,
                _id, _f, _system, DateTime.UtcNow, _user, _renderedMessage, ResultToString[LogResults.Warning]
            );
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void LogError(int id, Exception ex, string message, params object?[] args)
        {
            string _renderedMessage = RenderMessage(message, args);
            _renderedMessage += $" - {ex.Message}";

            var _f = new StackFrame(1, false).GetMethod().Name;

            _logger.LogError(
                MESSAGE_TEMPLATE,
                id, _f, _system, DateTime.UtcNow, _user, _renderedMessage, ResultToString[LogResults.Error]
            );
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void LogError(int id, Exception ex)
        {
            var _renderedMessage = ex.ToString();

            var _f = new StackFrame(1, false).GetMethod().Name;

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

            var _f = new StackFrame(1, false).GetMethod().Name;

            _logger.LogError(
                MESSAGE_TEMPLATE,
                _id, _f, _system, DateTime.UtcNow, _user, _renderedMessage, ResultToString[LogResults.Error]
            );
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void LogError(Exception ex)
        {
            var _renderedMessage = ex.ToString();

            var _f = new StackFrame(1, false).GetMethod().Name;

            _logger.LogError(
                MESSAGE_TEMPLATE,
                _id, _f, _system, DateTime.UtcNow, _user, _renderedMessage, ResultToString[LogResults.Error]
            );
        }
    }
}
