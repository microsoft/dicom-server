// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.Health.Dicom.Core.Models;

namespace Microsoft.Health.Dicom.Api.Models;

public class WindowedPaginationOptions : PaginationOptions, IValidatableObject
{
    public DateTimeOffset? StartTime { get; set; }

    public DateTimeOffset? EndTime { get; set; }

    public DateTimeOffsetRange Window => new DateTimeOffsetRange(StartTime.GetValueOrDefault(DateTimeOffset.MinValue), EndTime.GetValueOrDefault(DateTimeOffset.MaxValue));

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        DateTimeOffset start = StartTime.GetValueOrDefault(DateTimeOffset.MinValue);
        DateTimeOffset end = EndTime.GetValueOrDefault(DateTimeOffset.MaxValue);

        if (end <= start)
        {
            yield return new ValidationResult(
                string.Format(
                    CultureInfo.InvariantCulture,
                    DicomApiResource.InvalidTimeRange,
                    start,
                    end));
        }
    }
}
