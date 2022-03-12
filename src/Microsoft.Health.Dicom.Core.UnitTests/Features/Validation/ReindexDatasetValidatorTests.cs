// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FellowOakDicom;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Indexing;
using Microsoft.Health.Dicom.Core.Features.Validation;
using Microsoft.Health.Dicom.Tests.Common;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Validation
{
    public class ReindexDatasetValidatorTests
    {
        private readonly IReindexDatasetValidator _datasetValidator;
        private readonly IElementMinimumValidator _validator;
        private readonly IExtendedQueryTagErrorsService _tagErrorsService;

        public ReindexDatasetValidatorTests()
        {
            _validator = Substitute.For<IElementMinimumValidator>();
            _tagErrorsService = Substitute.For<IExtendedQueryTagErrorsService>();
            _datasetValidator = new ReindexDatasetValidator(_validator, _tagErrorsService);

            DicomValidationBuilderExtension.SkipValidation(null);
        }

        [Fact]
        public async Task GivenValidAndInvalidTagValues_WhenValidate_ThenReturnedValidTagsAndStoredFailure()
        {
            DicomTag tag1 = DicomTag.AcquisitionDateTime;
            DicomTag tag2 = DicomTag.DeviceID;
            DicomElement element1 = new DicomDateTime(tag1, "testvalue1");
            DicomElement element2 = new DicomLongString(tag2, "testvalue2");

            DicomDataset ds = Samples.CreateRandomInstanceDataset();
            ds.Add(element1);
            ds.Add(element2);

            var queryTag1 = new QueryTag(new ExtendedQueryTagStoreEntry(1, tag1.GetPath(), element1.ValueRepresentation.Code, null, QueryTagLevel.Instance, ExtendedQueryTagStatus.Ready, QueryStatus.Enabled, 0));
            var queryTag2 = new QueryTag(new ExtendedQueryTagStoreEntry(2, tag2.GetPath(), element2.ValueRepresentation.Code, null, QueryTagLevel.Instance, ExtendedQueryTagStatus.Ready, QueryStatus.Enabled, 0));

            // Throw exception when validate element1
            _validator
                .When(x => x.Validate(element1))
                .Throw(new ElementValidationException(tag1.GetFriendlyName(), DicomVR.DT, ValidationErrorCode.DateTimeIsInvalid));

            using var source = new CancellationTokenSource();

            // only return querytag2
            IReadOnlyCollection<QueryTag> validQueryTags = await _datasetValidator.ValidateAsync(ds, 1, new[] { queryTag1, queryTag2 }, source.Token);
            Assert.Same(queryTag2, validQueryTags.Single());

            // error for querytag1 is logged
            await _tagErrorsService
                .Received(1)
                .AddExtendedQueryTagErrorAsync(queryTag1.ExtendedQueryTagStoreEntry.Key, ValidationErrorCode.DateTimeIsInvalid, 1, source.Token);
        }
    }
}
