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

// This class is meant to help translate patterns like "/%Study%/%Series%/%SopInstance%.dcm"
// into format strings like "/{1}/{2}/{3}.dcm" which we can use when exporting.
// This feature is not currently exposed, but could be should the need arise.

internal static class ExportFilePattern
{
    public static string Parse(string pattern, ExportPatternPlaceholders placeholders = ExportPatternPlaceholders.All)
    {
        var builder = new StringBuilder();
        for (int i = 0; i < pattern.Length; i++)
        {
            if (pattern[i] == '\\')
            {
                if (++i == pattern.Length)
                    throw new FormatException(string.Format(CultureInfo.CurrentCulture, DicomBlobResource.InvalidEscapeSequence, "\\"));

                if (pattern[i] != '%')
                    throw new FormatException(string.Format(CultureInfo.CurrentCulture, DicomBlobResource.InvalidEscapeSequence, "\\" + pattern[i]));

                builder.Append(pattern[i]);
            }
            else if (pattern[i] == '%')
            {
                builder.Append('{');

                int j = pattern.IndexOf('%', i + 1);
                if (j == -1)
                    throw new FormatException(DicomBlobResource.MalformedPlaceholder);

                string p = pattern.Substring(i + 1, j - i - 1);
                if (placeholders.HasFlag(ExportPatternPlaceholders.Operation) && p.Equals(nameof(ExportPatternPlaceholders.Operation), StringComparison.OrdinalIgnoreCase))
                    builder.Append($"0:{OperationId.FormatSpecifier}");
                else if (placeholders.HasFlag(ExportPatternPlaceholders.Study) && p.Equals(nameof(ExportPatternPlaceholders.Study), StringComparison.OrdinalIgnoreCase))
                    builder.Append('1');
                else if (placeholders.HasFlag(ExportPatternPlaceholders.Series) && p.Equals(nameof(ExportPatternPlaceholders.Series), StringComparison.OrdinalIgnoreCase))
                    builder.Append('2');
                else if (placeholders.HasFlag(ExportPatternPlaceholders.SopInstance) && p.Equals(nameof(ExportPatternPlaceholders.SopInstance), StringComparison.OrdinalIgnoreCase))
                    builder.Append('3');
                else
                    throw new FormatException(string.Format(CultureInfo.CurrentCulture, DicomBlobResource.UnknownPlaceholder, p));

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
