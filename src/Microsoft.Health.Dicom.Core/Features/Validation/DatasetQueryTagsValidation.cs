// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using Dicom;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;

namespace Microsoft.Health.Dicom.Core.Features.Validation
{
    public class DatasetQueryTagsValidation : IDatasetValidation
    {
        private readonly IReadOnlyCollection<QueryTag> _queryTags;
        private readonly IDicomElementMinimumValidator _minimumValidator;
        private readonly Func<QueryTag, DicomElementValidationException, bool> _onValidationFailure;

        public DatasetQueryTagsValidation(IReadOnlyCollection<QueryTag> queryTags, IDicomElementMinimumValidator minimumValidator, Func<QueryTag, DicomElementValidationException, bool> onValidationFailure = null)
        {
            _queryTags = EnsureArg.IsNotNull(queryTags, nameof(queryTags));
            _minimumValidator = EnsureArg.IsNotNull(minimumValidator, nameof(minimumValidator));
            _onValidationFailure = onValidationFailure;
        }

        public void Validate(DicomDataset dataset)
        {
            EnsureArg.IsNotNull(dataset, nameof(dataset));
            List<QueryTag> validTags = new List<QueryTag>();

            foreach (QueryTag queryTag in _queryTags)
            {
                DicomElement dicomElement = dataset.GetDicomItem<DicomElement>(queryTag.Tag);

                if (dicomElement != null)
                {
                    if (dicomElement.ValueRepresentation != queryTag.VR)
                    {

                        var exception = new DicomElementValidationException(
                                ElementValidationErrorCode.ElementHasWrongVR,
                                queryTag.Tag.GetFriendlyName(),
                                queryTag.VR,
                                string.Format(
                                    CultureInfo.InvariantCulture,
                                    DicomCoreResource.MismatchVR,
                                    queryTag.Tag,
                                    queryTag.VR,
                                    dicomElement.ValueRepresentation));

                        if (_onValidationFailure == null || _onValidationFailure.Invoke(queryTag, exception))
                        {
                            throw exception;
                        }
                    }

                    try
                    {
                        _minimumValidator.Validate(dicomElement);
                    }
                    catch (DicomElementValidationException exception)
                    {
                        if (_onValidationFailure == null || _onValidationFailure.Invoke(queryTag, exception))
                        {
                            throw;
                        }
                    }
                }
            }
        }
    }
}
