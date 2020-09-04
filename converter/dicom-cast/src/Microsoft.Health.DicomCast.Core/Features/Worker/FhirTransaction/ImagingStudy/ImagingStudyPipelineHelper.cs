// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http.Headers;
using Dicom;
using Hl7.Fhir.Model;
using Hl7.Fhir.Utility;
using Microsoft.Health.DicomCast.Core.Extensions;

namespace Microsoft.Health.DicomCast.Core.Features.Worker.FhirTransaction
{
    public static class ImagingStudyPipelineHelper
    {
        private const StringComparison EqualsStringComparison = StringComparison.Ordinal;

        public static ImagingStudy.InstanceComponent GetInstanceWithinASeries(string sopInstanceUid, ImagingStudy.SeriesComponent existingSeries)
        {
            if (existingSeries == null)
            {
                return null;
            }

            return existingSeries.Instance.FirstOrDefault(instance => sopInstanceUid.Equals(instance.Uid, EqualsStringComparison));
        }

        public static ImagingStudy.SeriesComponent GetSeriesWithinAStudy(string seriesInstanceUid, List<ImagingStudy.SeriesComponent> existingSeriesCollection)
        {
            if (existingSeriesCollection == null)
            {
                return null;
            }

            return existingSeriesCollection.FirstOrDefault(series => seriesInstanceUid.Equals(series.Uid, EqualsStringComparison));
        }

        public static string GenerateEtag(string versionId)
        {
            var eTag = new EntityTagHeaderValue($"\"{versionId}\"", true);
            return eTag.ToString();
        }

        public static Bundle.RequestComponent GenerateCreateRequest(Identifier imagingStudyIdentifier)
        {
            return new Bundle.RequestComponent()
            {
                Method = Bundle.HTTPVerb.POST,
                IfNoneExist = imagingStudyIdentifier.ToSearchQueryParameter(),
                Url = ResourceType.ImagingStudy.GetLiteral(),
            };
        }

        public static Bundle.RequestComponent GenerateUpdateRequest(ImagingStudy imagingStudy)
        {
            return new Bundle.RequestComponent()
            {
                Method = Bundle.HTTPVerb.PUT,
                IfMatch = ImagingStudyPipelineHelper.GenerateEtag(imagingStudy.Meta.VersionId),
                Url = $"{ResourceType.ImagingStudy.GetLiteral()}/{imagingStudy.Id}",
            };
        }

        public static string GetModalityInString(DicomDataset dataset)
        {
            return dataset.GetSingleValueOrDefault<string>(DicomTag.Modality, default);
        }

        public static Coding GetModality(string modalityInString)
        {
            if (modalityInString != null)
            {
                return new Coding(FhirTransactionConstants.ModalityInSystem, modalityInString);
            }

            return null;
        }

        public static void SetDateTimeOffSet(FhirTransactionContext context)
        {
            DicomDataset metadata = context.ChangeFeedEntry.Metadata;

            if (metadata != null &&
                metadata.TryGetSingleValue(DicomTag.TimezoneOffsetFromUTC, out string utcOffsetInString))
            {
                try
                {
                    context.UtcDateTimeOffset = DateTimeOffset.ParseExact(utcOffsetInString, FhirTransactionConstants.UtcTimezoneOffsetFormat, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces).Offset;
                }
                catch (FormatException)
                {
                    throw new InvalidDicomTagValueException(nameof(DicomTag.TimezoneOffsetFromUTC), utcOffsetInString);
                }
            }
        }
    }
}
