// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

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
        private readonly IElementMinimumValidator _minimumValidator;
        private readonly IExtendedQueryTagErrorsService _tagErrorsService;
        public ReindexDatasetValidatorTests()
        {
            _minimumValidator = Substitute.For<IElementMinimumValidator>();
            _tagErrorsService = Substitute.For<IExtendedQueryTagErrorsService>();
            _datasetValidator = new ReindexDatasetValidator(_minimumValidator, _tagErrorsService);
        }

        [Fact]
        public async Task GivenValidAndInvalidTagValues_WhenValidate_ThenReturnedValidTagsAndStoredFailure()
        {
            DicomTag tag1 = DicomTag.DeviceSerialNumber;
            DicomTag tag2 = DicomTag.DeviceID;
            DicomDataset ds = Samples.CreateRandomInstanceDataset();
            DicomElement element1 = new DicomLongString(tag1, "testvalue1");
            DicomElement element2 = new DicomLongString(tag2, "testvalue2");
            ds.Add(element1);
            ds.Add(element2);
            QueryTag queryTag1 = new QueryTag(new ExtendedQueryTagStoreEntry(1, tag1.GetPath(), tag1.GetDefaultVR().Code, null, QueryTagLevel.Instance, ExtendedQueryTagStatus.Ready, QueryStatus.Enabled, 0));
            QueryTag queryTag2 = new QueryTag(new ExtendedQueryTagStoreEntry(2, tag2.GetPath(), tag2.GetDefaultVR().Code, null, QueryTagLevel.Instance, ExtendedQueryTagStatus.Ready, QueryStatus.Enabled, 0));

            // Throw exception when validate element1
            var ex = ElementValidationExceptionFactory.CreateDateIsInvalidException("testname", "testvalue");
            _minimumValidator.When(x => x.Validate(element1))
                .Throw(ex);

            // only return querytag2
            long watermark = 1;
            var validQueryTags = await _datasetValidator.ValidateAsync(ds, watermark, new[] { queryTag1, queryTag2 });
            Assert.Single(validQueryTags);
            Assert.Same(queryTag2, validQueryTags.First());

            // error for querytag1 is logged
            await _tagErrorsService.Received(1)
                   .AddExtendedQueryTagErrorAsync(queryTag1.ExtendedQueryTagStoreEntry.Key, ex.ErrorCode, 1, Arg.Any<CancellationToken>());
        }
    }
}
