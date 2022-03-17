// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Health.SqlServer.Features.Client;

namespace Microsoft.Health.Dicom.SqlServer.Extensions;

internal static class SqlCommandWrapperExtensions
{
    private const string DefaultRedactedValue = "***";

    [SuppressMessage("Usage", "CA2254:Template should be a static expression", Justification = "Template does not contain placeholders.")]
    internal static void LogSqlCommand<T>(this SqlCommandWrapper sqlCommandWrapper, ILogger<T> logger)
    {
        var sb = new StringBuilder();
        foreach (SqlParameter p in sqlCommandWrapper.Parameters)
        {
            sb.Append("DECLARE ")
                .Append(p)
                .Append(' ')
                .Append(p.SqlDbType)
                .Append(p.Value is string ? $"({p.Size})" : p.Value is decimal ? $"({p.Precision},{p.Scale})" : null)
                .Append(" = ")
                .Append(DefaultRedactedValue)
                .Append(';')
                .AppendLine();
        }

        sb.AppendLine();

        sb.AppendLine(sqlCommandWrapper.CommandText);
        logger.LogInformation(sb.ToString());
    }
}
