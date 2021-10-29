// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;
using Microsoft.Extensions.Logging;
using FellowOakLogLevel = FellowOakDicom.Log.LogLevel;
using IFellowOakLogger = FellowOakDicom.Log.ILogger;

namespace Microsoft.Health.Dicom.Core.Logging
{
    internal class FellowOakLoggerDecorator : IFellowOakLogger
    {
        private readonly ILogger _logger;

        public FellowOakLoggerDecorator(ILogger logger)
            => _logger = EnsureArg.IsNotNull(logger, nameof(logger));

        public void Debug(string msg, params object[] args)
            => _logger.LogDebug(msg, args);

        public void Error(string msg, params object[] args)
            => _logger.LogError(msg, args);

        public void Fatal(string msg, params object[] args)
            => _logger.LogCritical(msg, args);

        public void Info(string msg, params object[] args)
            => _logger.LogInformation(msg, args);

        public void Log(FellowOakLogLevel level, string msg, params object[] args)
            => _logger.Log(MapLogLevel(level), msg, args);

        public void Warn(string msg, params object[] args)
            => _logger.LogWarning(msg, args);

        private static LogLevel MapLogLevel(FellowOakLogLevel level)
            => level switch
            {
                FellowOakLogLevel.Debug => LogLevel.Debug,
                FellowOakLogLevel.Info => LogLevel.Information,
                FellowOakLogLevel.Warning => LogLevel.Warning,
                FellowOakLogLevel.Error => LogLevel.Error,
                FellowOakLogLevel.Fatal => LogLevel.Critical,
                _ => throw new ArgumentOutOfRangeException(nameof(level)),
            };
    }
}
