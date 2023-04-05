// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using EnsureThat;
using FellowOakDicom;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Validation;
using Microsoft.Health.Dicom.Core.Messages.Update;
using Microsoft.Health.Dicom.Core.Models.Update;

namespace Microsoft.Health.Dicom.Core.Features.Update;

/// <summary>
/// Provides functionality to validate an <see cref="UpdateInstanceRequest"/>.
/// </summary>
public static class UpdateRequestValidator
{
    /// <summary>
    /// Validates an <see cref="UpdateSpecification"/>.
    /// </summary>
    /// <param name="updateSpecification">The request to validate.</param>
    /// <exception cref="BadRequestException">Thrown when request body is missing.</exception>
    /// <exception cref="UidValidation">Thrown when the specified StudyInstanceUID is not a valid identifier.</exception>
    public static void ValidateRequest(UpdateSpecification updateSpecification)
    {
        EnsureArg.IsNotNull(updateSpecification, nameof(updateSpecification));
        if (updateSpecification == null)
        {
            throw new BadRequestException(DicomCoreResource.MissingRequestBody);
        }
        else if (updateSpecification.StudyInstanceUids == null)
        {
            throw new BadRequestException(DicomCoreResource.MissingRequiredTag);
        }
        foreach (var StudyInstanceUid in updateSpecification.StudyInstanceUids)
        {
            UidValidation.Validate(StudyInstanceUid, nameof(StudyInstanceUid), allowEmpty: true);
        }
    }

    /// <summary>
    /// Validates a <see cref="DicomDataset"/>.
    /// </summary>
    /// <param name="dataset">The Dicom dataset to validate.</param>
    /// <exception cref="BadRequestException">Thrown when dicom tag or value validation fails</exception>
    public static void ValidateDicomDataset(DicomDataset dataset)
    {
        EnsureArg.IsNotNull(dataset, nameof(dataset));
        List<string> invalidTags = new List<string>();
        List<string> invalidTagValues = new List<string>();
        string errorString = string.Empty;

        foreach (DicomItem item in dataset)
        {
            if (!UpdateTags.UpdateFilterTags.Contains(item.Tag))
            {
                invalidTags.Add(item.Tag.ToString());
            }
            try
            {
                item.Validate();
            }
            catch (DicomValidationException)
            {
                invalidTagValues.Add(item.Tag.ToString());
            }
        }
        if (invalidTags.Count > 0)
        {
            errorString = string.Concat(string.Format(CultureInfo.CurrentCulture, DicomCoreResource.DicomUpdateTagValidationFailed, GetCommaSeparatedTags(invalidTags)), "\n");
        }
        if (invalidTagValues.Count > 0)
        {
            errorString = string.Concat(errorString, string.Format(CultureInfo.CurrentCulture, DicomCoreResource.DicomUpdateTagValueValidationFailed, GetCommaSeparatedTags(invalidTagValues)));
        }
        if (!string.IsNullOrEmpty(errorString))
        {
            throw new BadRequestException(errorString);
        }
    }

    private static string GetCommaSeparatedTags(List<string> tags)
        => string.Join(", ", tags.Select(x => $"'{x}'"));
}
