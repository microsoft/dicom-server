// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Globalization;
using System.Linq.Expressions;
using FellowOakDicom;
using Hl7.Fhir.Model;
using Microsoft.Health.DicomCast.Core.Features.Worker.FhirTransaction;

namespace Microsoft.Health.DicomCast.Core.UnitTests.Features.Worker.FhirTransaction
{
    public static class TestUtility
    {
        public static Expression<Predicate<Identifier>> BuildIdentifierPredicate(string system, string value)
            => identifier => identifier != null &&
            string.Equals(identifier.System, system, StringComparison.Ordinal) &&
            string.Equals(identifier.Value, value, StringComparison.Ordinal);

        public static TimeSpan SetDateTimeOffSet(DicomDataset metadata)
        {
            if (metadata.TryGetSingleValue(DicomTag.TimezoneOffsetFromUTC, out string utcOffsetInString))
            {
                try
                {
                    return DateTimeOffset.ParseExact(utcOffsetInString, "zzz", CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces).Offset;
                }
                catch (FormatException)
                {
                    throw new InvalidDicomTagValueException(nameof(DicomTag.TimezoneOffsetFromUTC), utcOffsetInString);
                }
            }

            return TimeSpan.Zero;
        }
    }
}
