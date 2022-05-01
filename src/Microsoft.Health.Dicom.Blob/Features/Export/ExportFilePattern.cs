// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Globalization;
using System.Text;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Operations;

namespace Microsoft.Health.Dicom.Blob.Features.Export;

internal static class ExportFilePattern
{
    public static string Parse(string pattern, ExportPatternPlaceholders placeholders = ExportPatternPlaceholders.All)
    {
        var builder = new StringBuilder();
        for (int i = 0; i < pattern.Length; i++)
        {
            if (pattern[i] == '%')
            {
                builder.Append('{');

                int j = pattern.IndexOf('%', i + 1);
                if (j == -1)
                    throw new FormatException("Missing end character");

                string p = pattern.Substring(i, j - i);
                if (placeholders.HasFlag(ExportPatternPlaceholders.Operation) && p.Equals(nameof(ExportPatternPlaceholders.Operation), StringComparison.OrdinalIgnoreCase))
                    builder.Append($"0:{OperationId.FormatSpecifier}");
                else if (placeholders.HasFlag(ExportPatternPlaceholders.Study) && p.Equals(nameof(ExportPatternPlaceholders.Study), StringComparison.OrdinalIgnoreCase))
                    builder.Append('1');
                else if (placeholders.HasFlag(ExportPatternPlaceholders.Series) && p.Equals(nameof(ExportPatternPlaceholders.Series), StringComparison.OrdinalIgnoreCase))
                    builder.Append('2');
                else if (placeholders.HasFlag(ExportPatternPlaceholders.SopInstance) && p.Equals(nameof(ExportPatternPlaceholders.SopInstance), StringComparison.OrdinalIgnoreCase))
                    builder.Append('3');
                else
                    throw new FormatException("Unrecognized placeholder");

                builder.Append('}');
                i = j; // Move ahead
            }
            else
            {
                builder.Append(pattern[i]);
            }
        }

        return builder.ToString();
    }

    public static string Format(string format, Guid operationId)
        => string.Format(CultureInfo.InvariantCulture, format, operationId);

    public static string Format(string format, Guid operationId, VersionedInstanceIdentifier identifier)
    {
        EnsureArg.IsNotNull(identifier, nameof(identifier));
        return string.Format(CultureInfo.InvariantCulture, format, operationId, identifier.StudyInstanceUid, identifier.SeriesInstanceUid, identifier.SopInstanceUid);
    }
}
