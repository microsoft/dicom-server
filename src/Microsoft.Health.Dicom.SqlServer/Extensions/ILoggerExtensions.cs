// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Microsoft.Health.Dicom.SqlServer.Extensions
{
    internal static class ILoggerExtensions
    {
        public static void LogStoredProcedureSuccess(this ILogger logger, string name, Stopwatch sw)
            => logger.LogInformation(
                "SQL stored procedure '{Name}' successfully executed after {ElapsedMs} ms",
                name,
                sw.ElapsedMilliseconds);
    }
}
