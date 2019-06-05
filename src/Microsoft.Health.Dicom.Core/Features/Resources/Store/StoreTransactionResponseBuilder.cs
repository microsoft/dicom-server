// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Net;
using Dicom;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.Persistence;
using Microsoft.Health.Dicom.Core.Features.Routing;
using Microsoft.Health.Dicom.Core.Messages.Store;

namespace Microsoft.Health.Dicom.Core.Features.Resources.Store
{
    /// <summary>
    /// http://dicom.nema.org/medical/dicom/current/output/chtml/part18/sect_6.6.html#table_6.6.1-4
    /// </summary>
    internal class StoreTransactionResponseBuilder
    {
        private const ushort ProcessingFailure = 272;
        private readonly DicomDataset _dataset;
        private readonly Uri _baseUri;
        private readonly IDicomRouteProvider _dicomRouteProvider;
        private HttpStatusCode _responseStatusCode = HttpStatusCode.BadRequest;
        private bool _successAdded = false;
        private bool _failureAdded = false;

        public StoreTransactionResponseBuilder(Uri baseUri, IDicomRouteProvider dicomRouteProvider, string studyInstanceUID = null)
        {
            EnsureArg.IsNotNull(baseUri, nameof(baseUri));
            EnsureArg.IsNotNull(dicomRouteProvider, nameof(dicomRouteProvider));

            Uri retrieveUri = string.IsNullOrWhiteSpace(studyInstanceUID) ? null : _dicomRouteProvider.GetStudyUri(baseUri, studyInstanceUID);

            _dataset = new DicomDataset { { DicomTag.RetrieveURL, retrieveUri?.ToString() } };
            _baseUri = baseUri;
            _dicomRouteProvider = dicomRouteProvider;
        }

        public StoreDicomResourcesResponse GetStoreResponse(bool hadAnyUnsupportedContentTypes)
        {
            if (_successAdded || _failureAdded)
            {
                return new StoreDicomResourcesResponse(_responseStatusCode, _dataset);
            }

            // If nothing failed or added we should return no content or unsupported media type.
            return new StoreDicomResourcesResponse(hadAnyUnsupportedContentTypes ? HttpStatusCode.UnsupportedMediaType : HttpStatusCode.NoContent);
        }

        public void AddStoreOutcome(StoreOutcome storeOutcome)
        {
            if (storeOutcome.IsStored)
            {
                AddSuccess(storeOutcome.DicomIdentity);
            }
            else
            {
                AddFailure(storeOutcome.DicomIdentity);
            }
        }

        private void AddSuccess(DicomIdentity dicomIdentity)
        {
            EnsureArg.IsNotNull(dicomIdentity);

            Uri retrieveUri = _dicomRouteProvider.GetInstanceUri(
                                    _baseUri, dicomIdentity.StudyInstanceUID, dicomIdentity.SeriesInstanceUID, dicomIdentity.SopInstanceUID);

            DicomSequence referencedSopSequence = _dataset.Contains(DicomTag.ReferencedSOPSequence) ?
                                                        _dataset.GetSequence(DicomTag.ReferencedSOPSequence) :
                                                        new DicomSequence(DicomTag.ReferencedSOPSequence);

            referencedSopSequence.Items.Add(new DicomDataset()
            {
                { DicomTag.ReferencedSOPClassUID, dicomIdentity.SopClassUID },
                { DicomTag.ReferencedSOPInstanceUID, dicomIdentity.SopInstanceUID },
                { DicomTag.RetrieveURL, retrieveUri.ToString() },
            });

            // If any failures when adding, we return Accepted, otherwise OK.
            _responseStatusCode = _failureAdded ? HttpStatusCode.Accepted : HttpStatusCode.OK;

            _dataset.AddOrUpdate(referencedSopSequence);
            _successAdded = true;
        }

        private void AddFailure(DicomIdentity dicomIdentity)
        {
            // If we have added any successfully we return Accepted, otherwise a specific status code.
            _responseStatusCode = _successAdded ? HttpStatusCode.Accepted : HttpStatusCode.Conflict;
            _failureAdded = true;

            DicomSequence failedSopSequence = _dataset.Contains(DicomTag.FailedSOPSequence) ?
                                                    _dataset.GetSequence(DicomTag.FailedSOPSequence) :
                                                    new DicomSequence(DicomTag.FailedSOPSequence);

            if (dicomIdentity != null &&
                !string.IsNullOrWhiteSpace(dicomIdentity.SopInstanceUID) &&
                !string.IsNullOrWhiteSpace(dicomIdentity.SopClassUID))
            {
                failedSopSequence.Items.Add(new DicomDataset()
                {
                    { DicomTag.ReferencedSOPClassUID, dicomIdentity.SopClassUID },
                    { DicomTag.ReferencedSOPInstanceUID, dicomIdentity.SopInstanceUID },
                    { DicomTag.FailureReason, ProcessingFailure },
                });
            }
            else
            {
                failedSopSequence.Items.Add(new DicomDataset());
            }

            _dataset.AddOrUpdate(failedSopSequence);
        }
    }
}
