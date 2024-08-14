﻿using AXERP.API.LogHelper.Enums;
using Microsoft.Extensions.Logging;
using Serilog;
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
            //"{ProcessId}{Function}{System}{When}{Who}{Description}{Result}";
            "ProcessId: {ProcessId} | Function: {Function} | System: {System} | When: {When} | Who: {Who} | Description: {Description} | Result: {Result}";

        public AxerpLogger(ILogger<T> logger)
        {
            _logger = logger;
        }

        public void Set(string user, string system, long? id = null)
        {
            _user = user;
            _system = system;
            SetNewId(id);
            _stopwatch = new Stopwatch();
        }

        public void SetNewId(long? id)
        {
            ProcessId = id ?? DateTime.UnixEpoch.Ticks;
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

            var _f = new StackFrame(1, false).GetMethod().Name;

            _logger.LogInformation(
                MESSAGE_TEMPLATE,
                id, _f, _system, DateTime.UtcNow, _user, _renderedMessage, ResultToString[LogResults.Ok]
            );
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void LogInformation(string message, params object?[] args)
        {
            string _renderedMessage = RenderMessage(message, args);

            var _f = new StackFrame(1, false).GetMethod().Name;

            _logger.LogInformation(
                MESSAGE_TEMPLATE,
                ProcessId, _f, _system, DateTime.UtcNow, _user, _renderedMessage, ResultToString[LogResults.Ok]
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
                ProcessId, _f, _system, DateTime.UtcNow, _user, _renderedMessage, ResultToString[LogResults.Debug]
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
                ProcessId, _f, _system, DateTime.UtcNow, _user, _renderedMessage, ResultToString[LogResults.Warning]
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
        public void LogError(int id, string message, params object?[] args)
        {
            string _renderedMessage = RenderMessage(message, args);

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
                ProcessId, _f, _system, DateTime.UtcNow, _user, _renderedMessage, ResultToString[LogResults.Error]
            );
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void LogError(string message, params object?[] args)
        {
            string _renderedMessage = RenderMessage(message, args);

            var _f = new StackFrame(1, false).GetMethod().Name;

            _logger.LogError(
                MESSAGE_TEMPLATE,
                ProcessId, _f, _system, DateTime.UtcNow, _user, _renderedMessage, ResultToString[LogResults.Error]
            );
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void LogError(Exception ex)
        {
            var _renderedMessage = ex.ToString();

            var _f = new StackFrame(1, false).GetMethod().Name;

            _logger.LogError(
                MESSAGE_TEMPLATE,
                ProcessId, _f, _system, DateTime.UtcNow, _user, _renderedMessage, ResultToString[LogResults.Error]
            );
        }
    }
}
