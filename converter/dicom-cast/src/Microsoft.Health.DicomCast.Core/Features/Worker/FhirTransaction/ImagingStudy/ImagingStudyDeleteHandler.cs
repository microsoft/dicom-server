// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Hl7.Fhir.Model;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Client.Models;
using Microsoft.Health.DicomCast.Core.Configurations;
using Microsoft.Health.DicomCast.Core.Extensions;
using Microsoft.Health.DicomCast.Core.Features.Fhir;

namespace Microsoft.Health.DicomCast.Core.Features.Worker.FhirTransaction
{
    /// <summary>
    /// Builds the request for creating or updating the <see cref="ImagingStudy"/> resource.
    /// </summary>
    public class ImagingStudyDeleteHandler : IImagingStudyDeleteHandler
    {
        private readonly IFhirService _fhirService;
        private readonly string _dicomWebEndpoint;

        public ImagingStudyDeleteHandler(
           IFhirService fhirService,
           IOptions<DicomWebConfiguration> dicomWebConfiguration)
        {
            EnsureArg.IsNotNull(fhirService, nameof(fhirService));
            EnsureArg.IsNotNull(dicomWebConfiguration?.Value, nameof(dicomWebConfiguration));

            _fhirService = fhirService;
            _dicomWebEndpoint = dicomWebConfiguration.Value.Endpoint.ToString();
        }

        /// <inheritdoc/>
        public async Task<FhirTransactionRequestEntry> BuildAsync(FhirTransactionContext context, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(context, nameof(context));
            EnsureArg.IsNotNull(context.ChangeFeedEntry, nameof(context.ChangeFeedEntry));

            ChangeFeedEntry changeFeedEntry = context.ChangeFeedEntry;

            Identifier imagingStudyIdentifier = ImagingStudyIdentifierUtility.CreateIdentifier(changeFeedEntry.StudyInstanceUid);
            ImagingStudy imagingStudy = await _fhirService.RetrieveImagingStudyAsync(imagingStudyIdentifier, cancellationToken);

            // Returns null if imagingStudy does not exists for given studyInstanceUid
            if (imagingStudy == null)
            {
                return null;
            }

            string imagingStudySource = imagingStudy.Meta.Source;

            ImagingStudy.SeriesComponent series = ImagingStudyPipelineHelper.GetSeriesWithinAStudy(changeFeedEntry.SeriesInstanceUid, imagingStudy.Series);
            ImagingStudy.InstanceComponent instance = ImagingStudyPipelineHelper.GetInstanceWithinASeries(changeFeedEntry.SopInstanceUid, series);

            // Return null if the given instance is not present in ImagingStudy
            if (instance == null)
            {
                return null;
            }

            // Removes instance from series collection
            series.Instance.Remove(instance);

            // Removes series from ImagingStudy if its instance collection is empty
            if (series.Instance.Count == 0)
            {
                imagingStudy.Series.Remove(series);
            }

            if (imagingStudy.Series.Count == 0 && _dicomWebEndpoint.Equals(imagingStudySource, System.StringComparison.Ordinal))
            {
                return new FhirTransactionRequestEntry(
                    FhirTransactionRequestMode.Delete,
                    ImagingStudyPipelineHelper.GenerateDeleteRequest(imagingStudy),
                    imagingStudy.ToServerResourceId(),
                    imagingStudy);
            }

            return new FhirTransactionRequestEntry(
                FhirTransactionRequestMode.Update,
                ImagingStudyPipelineHelper.GenerateUpdateRequest(imagingStudy),
                imagingStudy.ToServerResourceId(),
                imagingStudy);
        }
    }
}
