// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Microsoft.Health.Dicom.Core.Features.Validation;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Validation
{
    /// <summary>
    /// Test class for  DicomElementMinimumValidation
    /// </summary>
    public partial class DicomElementMinimumValidationTests
    {
        public static IEnumerable<object[]> AEInvalidValues()
        {
            yield return new object[] { SpecialChars.Backslash.ToString() };
            yield return new object[] { SpecialChars.CarriageReturn.ToString() };
            yield return new object[] { SpecialChars.Escape.ToString() };
            yield return new object[] { SpecialChars.LineFeed.ToString() };
            yield return new object[] { SpecialChars.PageBreak.ToString() };
            yield return new object[] { "123456789101112131415" }; // exceed max allowed length
        }

        public static IEnumerable<object[]> PNValidValues()
        {
            yield return new object[] { SpecialChars.Backslash.ToString() };
            yield return new object[] { SpecialChars.CarriageReturn.ToString() };
            yield return new object[] { SpecialChars.Escape.ToString() };
            yield return new object[] { SpecialChars.LineFeed.ToString() };
            yield return new object[] { SpecialChars.PageBreak.ToString() };
            yield return new object[] { "123456789101112131415" }; // exceed max allowed length
        }
    }
}
