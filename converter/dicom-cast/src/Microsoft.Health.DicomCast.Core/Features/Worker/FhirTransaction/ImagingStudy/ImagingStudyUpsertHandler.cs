// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
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
    public class ImagingStudyUpsertHandler : IImagingStudyUpsertHandler
    {
        private readonly IFhirService _fhirService;
        private readonly IImagingStudySynchronizer _imagingStudySynchronizer;
        private readonly string _dicomWebEndpoint;

        public ImagingStudyUpsertHandler(
           IFhirService fhirService,
           IImagingStudySynchronizer imagingStudySynchronizer,
           IOptions<DicomWebConfiguration> dicomWebConfiguration)
        {
            EnsureArg.IsNotNull(fhirService, nameof(fhirService));
            EnsureArg.IsNotNull(imagingStudySynchronizer, nameof(_imagingStudySynchronizer));
            EnsureArg.IsNotNull(dicomWebConfiguration?.Value, nameof(dicomWebConfiguration));

            _fhirService = fhirService;
            _imagingStudySynchronizer = imagingStudySynchronizer;
            _dicomWebEndpoint = dicomWebConfiguration.Value.Endpoint.ToString();
        }

        /// <inheritdoc/>
        public async Task<FhirTransactionRequestEntry> BuildAsync(FhirTransactionContext context, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(context, nameof(context));
            EnsureArg.IsNotNull(context.ChangeFeedEntry, nameof(context.ChangeFeedEntry));
            EnsureArg.IsNotNull(context.Request, nameof(context.Request));

            IResourceId patientId = context.Request.Patient.ResourceId;

            ChangeFeedEntry changeFeedEntry = context.ChangeFeedEntry;

            Identifier imagingStudyIdentifier = ImagingStudyIdentifierUtility.CreateIdentifier(changeFeedEntry.StudyInstanceUid);

            ImagingStudy existingImagingStudy = await _fhirService.RetrieveImagingStudyAsync(imagingStudyIdentifier, cancellationToken);
            ImagingStudy imagingStudy = (ImagingStudy)existingImagingStudy?.DeepCopy();

            FhirTransactionRequestMode requestMode = FhirTransactionRequestMode.None;

            if (existingImagingStudy == null)
            {
                imagingStudy = new ImagingStudy()
                {
                    Status = ImagingStudy.ImagingStudyStatus.Available,
                    Subject = patientId.ToResourceReference(),
                };

                imagingStudy.Identifier.Add(imagingStudyIdentifier);
                imagingStudy.Meta = new Meta()
                {
                    Source = _dicomWebEndpoint,
                };
                requestMode = FhirTransactionRequestMode.Create;
            }

            SynchronizeImagingStudyProperties(context, imagingStudy);

            if (requestMode != FhirTransactionRequestMode.Create &&
                !existingImagingStudy.IsExactly(imagingStudy))
            {
                requestMode = FhirTransactionRequestMode.Update;
            }

            Bundle.RequestComponent request = requestMode switch
            {
                FhirTransactionRequestMode.Create => ImagingStudyPipelineHelper.GenerateCreateRequest(imagingStudyIdentifier),
                FhirTransactionRequestMode.Update => ImagingStudyPipelineHelper.GenerateUpdateRequest(imagingStudy),
                _ => null,
            };

            IResourceId resourceId = requestMode switch
            {
                FhirTransactionRequestMode.Create => new ClientResourceId(),
                _ => existingImagingStudy.ToServerResourceId(),
            };

            return new FhirTransactionRequestEntry(
                requestMode,
                request,
                resourceId,
                imagingStudy);
        }

        private void SynchronizeImagingStudyProperties(FhirTransactionContext context, ImagingStudy imagingStudy)
        {
            _imagingStudySynchronizer.SynchronizeStudyProperties(context, imagingStudy);

            AddSeriesToImagingStudy(context, imagingStudy);
        }

        private void AddSeriesToImagingStudy(FhirTransactionContext context, ImagingStudy imagingStudy)
        {
            ChangeFeedEntry changeFeedEntry = context.ChangeFeedEntry;

            List<ImagingStudy.SeriesComponent> existingSeriesCollection = imagingStudy.Series;

            ImagingStudy.InstanceComponent instance = new ImagingStudy.InstanceComponent()
            {
                Uid = changeFeedEntry.SopInstanceUid,
            };

            // Checks if the given series already exists within a study
            ImagingStudy.SeriesComponent series = ImagingStudyPipelineHelper.GetSeriesWithinAStudy(changeFeedEntry.SeriesInstanceUid, existingSeriesCollection);

            if (series == null)
            {
                series = new ImagingStudy.SeriesComponent()
                {
                    Uid = changeFeedEntry.SeriesInstanceUid,
                };

                series.Instance.Add(instance);
                imagingStudy.Series.Add(series);
            }
            else
            {
                ImagingStudy.InstanceComponent existingInstance = ImagingStudyPipelineHelper.GetInstanceWithinASeries(changeFeedEntry.SopInstanceUid, series);

                if (existingInstance == null)
                {
                    series.Instance.Add(instance);
                }
                else
                {
                    instance = existingInstance;
                }
            }

            _imagingStudySynchronizer.SynchronizeSeriesProperties(context, series);
            _imagingStudySynchronizer.SynchronizeInstanceProperties(context, instance);
        }
    }
}
