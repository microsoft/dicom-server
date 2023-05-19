// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using FellowOakDicom;
using Microsoft.Health.Dicom.Tests.Common;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Validation;

/// <summary>
/// This is a temporary test to show the difference in validation based on how the method is called
/// </summary>
public class FoDicomValidationTests
{
    [Fact]
    public void WhenValidatingDirectly_ExpectNullsAccepted_AndOtherwiseNullRefThrown()
    {
        string nullValue = null;
        DicomDataset dicomDataset = Samples.CreateRandomInstanceDataset(validateItems: false);
        dicomDataset.Add(DicomTag.AcquisitionDateTime, nullValue);

        DicomElement de = dicomDataset.GetDicomItem<DicomElement>(DicomTag.AcquisitionDateTime);
        de.Validate();
        Assert.Throws<NullReferenceException>(() => de.ValueRepresentation.ValidateString(nullValue));
    }
}
