// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Health.Dicom.Api.Features.ModelBinders;
using Microsoft.Health.Dicom.Core.Models;

namespace Microsoft.Health.Dicom.Api.Models;

public class WindowedPaginationOptions : PaginationOptions, IValidatableObject
{
    [ModelBinder(typeof(MandatoryTimeZoneBinder))]
    public DateTimeOffset? StartTime { get; set; }

    [ModelBinder(typeof(MandatoryTimeZoneBinder))]
    public DateTimeOffset? EndTime { get; set; }

    public TimeRange Window => new TimeRange(StartTime.GetValueOrDefault(DateTimeOffset.MinValue), EndTime.GetValueOrDefault(DateTimeOffset.MaxValue));

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
