// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Microsoft.Health.Dicom.SqlServer.Extensions
{
    internal static class ILoggerExtensions
    {
        private static readonly Action<ILogger, string, long, Exception> LogSuccess =
            LoggerMessage.Define<string, long>(
                LogLevel.Information,
                default,
                "SQL stored procedure '{Name}' successfully executed after {ElapsedMs} ms");

        public static void StoredProcedureSucceeded(this ILogger logger, string name, Stopwatch sw)
            => LogSuccess(logger, name, sw.ElapsedMilliseconds, null);
    }
}
