// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using EnsureThat;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Validation;
using Microsoft.Health.Dicom.Core.Features.Validation.Dataset;

namespace Microsoft.Health.Dicom.Core.Features.Store
{
    /// <summary>
    /// Provides functionality to validate a <see cref="DicomDataset"/> to make sure it meets the minimum requirement.
    /// </summary>
    public class DicomDatasetValidator : IDicomDatasetValidator
    {
        private readonly bool _enableFullDicomItemValidation;
        private readonly IDicomElementMinimumValidator _minimumValidator;
        private readonly IQueryTagService _queryTagService;

        public DicomDatasetValidator(IOptions<FeatureConfiguration> featureConfiguration, IDicomElementMinimumValidator minimumValidator, IQueryTagService queryTagService)
        {
            EnsureArg.IsNotNull(featureConfiguration?.Value, nameof(featureConfiguration));
            EnsureArg.IsNotNull(minimumValidator, nameof(minimumValidator));
            EnsureArg.IsNotNull(queryTagService, nameof(queryTagService));

            _enableFullDicomItemValidation = featureConfiguration.Value.EnableFullDicomItemValidation;
            _minimumValidator = minimumValidator;
            _queryTagService = queryTagService;
        }

        public async Task ValidateAsync(DicomDataset dicomDataset, string requiredStudyInstanceUid, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));

            new DatasetCoreTagsValidation(requiredStudyInstanceUid).Validate(dicomDataset);

            // validate input data elements
            if (_enableFullDicomItemValidation)
            {
                new DatasetFullValidation().Validate(dicomDataset);
            }
            else
            {
                IReadOnlyCollection<QueryTag> queryTags = await _queryTagService.GetQueryTagsAsync(cancellationToken);
                new DatasetQueryTagsValidation(queryTags, _minimumValidator).Validate(dicomDataset);
            }
        }
    }
}
