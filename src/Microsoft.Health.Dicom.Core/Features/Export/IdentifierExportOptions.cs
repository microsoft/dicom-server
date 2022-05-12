// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.Health.Dicom.Core.Models.Common;

namespace Microsoft.Health.Dicom.Core.Features.Export;

internal sealed class IdentifierExportOptions : IValidatableObject
{
    public IReadOnlyList<string> Values { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var results = new List<ValidationResult>();
        if (Values == null || Values.Count == 0)
        {
            results.Add(new ValidationResult(string.Format(CultureInfo.CurrentCulture, DicomCoreResource.MissingProperty, nameof(Values)), new string[] { nameof(Values) }));
        }
        else
        {
            foreach (string value in Values)
            {
                if (value == null || !DicomIdentifier.TryParse(value, out _))
                {
                    results.Add(new ValidationResult(string.Format(CultureInfo.CurrentCulture, DicomCoreResource.InvalidDicomIdentifier, value), new string[] { nameof(Values) }));
                    break; // We could also accumulate all of the issues
                }
            }
        }

        return results;
    }
}
