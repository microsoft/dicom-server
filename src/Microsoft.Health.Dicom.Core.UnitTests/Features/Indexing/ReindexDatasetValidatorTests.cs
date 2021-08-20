// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Dicom;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Indexing;
using Microsoft.Health.Dicom.Core.Features.Validation;
using Microsoft.Health.Dicom.Tests.Common.Extensions;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Indexing
{
    public class ReindexDatasetValidatorTests
    {
        private readonly IElementMinimumValidator _minimumValidator;
        private readonly IExtendedQueryTagErrorStore _extendedQueryTagErrorStore;

        public ReindexDatasetValidatorTests()
        {
            _extendedQueryTagErrorStore = Substitute.For<IExtendedQueryTagErrorStore>();
            _minimumValidator = Substitute.For<IElementMinimumValidator>();
        }

        [Fact]
        public void GivenReindexDatasetValidator_WhenValidatingInstanceFails_ThenErrorShouldBeRecorded()
        {
            DicomDataset dataset = Substitute.For<DicomDataset>();

            DicomTag tag = DicomTag.PatientName;
            QueryTag queryTag = new QueryTag(tag.BuildExtendedQueryTagStoreEntry(level: QueryTagLevel.Study));
            dataset.Add(tag, "12");

            IReindexDatasetValidator validator = new ReindexDatasetValidator(_minimumValidator, _extendedQueryTagErrorStore);

            _minimumValidator.When(x => x.Validate(Arg.Any<DicomElement>())).Throw(new DicomElementValidationException("name", tag.GetDefaultVR(), "fake error message."));

            validator.Validate(
                dataset,
                300,
                new List<QueryTag>() { queryTag });

            Assert.Throws<DicomElementValidationException>(() => dataset.Received(1).ValidateQueryTag(
                queryTag,
                _minimumValidator));

            _extendedQueryTagErrorStore.Received(1).AddExtendedQueryTagErrorAsync(
                Arg.Any<int>(),
                Arg.Any<string>(),
                Arg.Any<long>());
        }
    }
}
