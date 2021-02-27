// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading;
using Dicom;
using EnsureThat;
using Hl7.Fhir.Model;
using Hl7.Fhir.Utility;
using Microsoft.Health.DicomCast.Core.Exceptions;
using Microsoft.Health.DicomCast.Core.Extensions;
using Microsoft.Health.DicomCast.Core.Features.ExceptionStorage;
using Task = System.Threading.Tasks.Task;

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

        public static ImagingStudy.SeriesComponent GetSeriesWithinAStudy(string seriesInstanceUid, IEnumerable<ImagingStudy.SeriesComponent> existingSeriesCollection)
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
            EnsureArg.IsNotNull(imagingStudy, nameof(imagingStudy));

            return new Bundle.RequestComponent()
            {
                Method = Bundle.HTTPVerb.PUT,
                IfMatch = GenerateEtag(imagingStudy.Meta.VersionId),
                Url = $"{ResourceType.ImagingStudy.GetLiteral()}/{imagingStudy.Id}",
            };
        }

        public static Bundle.RequestComponent GenerateDeleteRequest(ImagingStudy imagingStudy)
        {
            EnsureArg.IsNotNull(imagingStudy, nameof(imagingStudy));

            return new Bundle.RequestComponent()
            {
                Method = Bundle.HTTPVerb.DELETE,
                Url = $"{ResourceType.ImagingStudy.GetLiteral()}/{imagingStudy.Id}",
            };
        }

        public static string GetModalityInString(DicomDataset dataset)
        {
            EnsureArg.IsNotNull(dataset, nameof(dataset));
            return dataset.GetSingleValueOrDefault<string>(DicomTag.Modality, default);
        }

        public static string GetAccessionNumberInString(DicomDataset dataset)
        {
            EnsureArg.IsNotNull(dataset, nameof(dataset));
            return dataset.GetSingleValueOrDefault<string>(DicomTag.AccessionNumber, default);
        }

        public static Identifier GetAccessionNumber(string accessionNumber)
        {
            EnsureArg.IsNotNull(accessionNumber, nameof(accessionNumber));
            var coding = new Coding(system: FhirTransactionConstants.AccessionNumberTypeSystem, code: FhirTransactionConstants.AccessionNumberTypeCode);
            var codeableConcept = new CodeableConcept();
            codeableConcept.Coding.Add(coding);
            return new Identifier(system: null, value: accessionNumber)
            {
                Type = codeableConcept,
            };
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
            EnsureArg.IsNotNull(context, nameof(context));

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

        public static async Task SynchronizePropertiesAsync<T>(T component, FhirTransactionContext context, Action<T, FhirTransactionContext> synchronizeAction, bool requiredProperty, bool enforceAllFields, IExceptionStore exceptionStore, CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNull(context, nameof(context));
            EnsureArg.IsNotNull(synchronizeAction, nameof(synchronizeAction));
            EnsureArg.IsNotNull(exceptionStore, nameof(exceptionStore));

            try
            {
                synchronizeAction(component, context);
            }
            catch (DicomTagException ex)
            {
                if (!enforceAllFields && !requiredProperty)
                {
                    await exceptionStore.WriteExceptionAsync(
                        context.ChangeFeedEntry,
                        ex,
                        ErrorType.DicomValidationError,
                        cancellationToken);
                }
                else
                {
                    throw;
                }
            }
        }
    }
}
