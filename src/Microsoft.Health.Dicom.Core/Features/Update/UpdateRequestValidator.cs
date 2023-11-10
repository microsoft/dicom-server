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
using Microsoft.Health.Dicom.Core.Features.Store;
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
        if (updateSpecification.StudyInstanceUids == null || updateSpecification.StudyInstanceUids.Count == 0)
        {
            throw new BadRequestException(string.Format(CultureInfo.CurrentCulture, DicomCoreResource.MissingRequiredField, nameof(updateSpecification.StudyInstanceUids)));
        }
        else if (updateSpecification.StudyInstanceUids.Count > UpdateTags.MaxStudyInstanceUidLimit)
        {
            throw new BadRequestException(string.Format(CultureInfo.CurrentCulture, DicomCoreResource.DicomUpdateStudyInstanceUidsExceedMaxCount, UpdateTags.MaxStudyInstanceUidLimit));
        }
        foreach (var StudyInstanceUid in updateSpecification.StudyInstanceUids)
        {
            UidValidation.Validate(StudyInstanceUid, nameof(StudyInstanceUid));
        }
    }

    /// <summary>
    /// Validates a <see cref="DicomDataset"/>.
    /// </summary>
    /// <param name="dataset">The Dicom dataset to validate.</param>
    /// <exception cref="BadRequestException">Thrown when dicom tag or value validation fails</exception>
    public static DicomDataset ValidateDicomDataset(DicomDataset dataset)
    {
        EnsureArg.IsNotNull(dataset, nameof(dataset));
        var errors = new List<string>();
        foreach (DicomItem item in dataset)
        {
            if (!UpdateTags.UpdateStudyFilterTags.Contains(item.Tag))
            {
                var message = string.Format(CultureInfo.CurrentCulture, DicomCoreResource.DicomUpdateTagValidationFailed, item.Tag.ToString());
                errors.Add(string.Format(CultureInfo.InvariantCulture, DicomCoreResource.ErrorMessageFormat, ErrorNumbers.ValidationFailure, item.Tag.ToString(), message));
            }
            else
            {
                try
                {
                    item.Validate();
                }
                catch (DicomValidationException ex)
                {
                    errors.Add(string.Format(CultureInfo.InvariantCulture, DicomCoreResource.ErrorMessageFormat, ErrorNumbers.ValidationFailure, item.Tag.ToString(), ex.Message));
                }
            }
        }
        var failedSop = new DicomDataset();
        if (errors.Count > 0)
        {
            var failedAttributes = errors.Select(error => new DicomDataset(new DicomLongString(DicomTag.ErrorComment, error))).ToArray();
            var failedAttributeSequence = new DicomSequence(DicomTag.FailedAttributesSequence, failedAttributes);
            failedSop.Add(failedAttributeSequence);
        }
        return failedSop;
    }
}
