// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Dicom;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Validation;
using Microsoft.Health.Dicom.Tests.Common;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Validation
{
    public class ImplicitValueRepresentationValidatorTests
    {
        [Theory]
        [MemberData(nameof(GetExplicitVRTransferSyntax))]
        public void GivenDicomDatasetWithNonImplicitVR_WhenValidating_ThenItShouldSucceed(DicomTransferSyntax transferSyntax)
        {
            var dicomDataset = Samples
                .CreateRandomInstanceDataset(dicomTransferSyntax: transferSyntax)
                .NotValidated();

            var exception = Record.Exception(() => ImplicitValueRepresentationValidator.Validate(dicomDataset));
            Assert.Null(exception);
        }

        [Theory]
        [MemberData(nameof(GetNonExplicitVRTransferSyntax))]
        public void GivenDicomDatasetWithImplicitVR_WhenValidating_ThenItShouldThrowNotAcceptableException(DicomTransferSyntax transferSyntax)
        {
            var dicomDataset = Samples
                .CreateRandomInstanceDataset(dicomTransferSyntax: transferSyntax)
                .NotValidated();

            var exception = Record.Exception(() => ImplicitValueRepresentationValidator.Validate(dicomDataset));

            Assert.NotNull(exception);
            Assert.IsType<NotAcceptableException>(exception);
        }

        public static IEnumerable<object[]> GetExplicitVRTransferSyntax()
        {
            foreach (var ts in Samples.GetAllDicomTransferSyntax())
            {
                if (!ts.IsExplicitVR)
                    continue;

                yield return new object[] { ts };
            }
        }

        public static IEnumerable<object[]> GetNonExplicitVRTransferSyntax()
        {
            foreach (var ts in Samples.GetAllDicomTransferSyntax())
            {
                if (ts.IsExplicitVR)
                    continue;

                yield return new object[] { ts };
            }
        }
    }
}
