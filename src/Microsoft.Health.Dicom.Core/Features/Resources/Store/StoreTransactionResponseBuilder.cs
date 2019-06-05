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
        private bool _successAdded = false;
        private bool _failureAdded = false;

        public StoreTransactionResponseBuilder(Uri baseUri, IDicomRouteProvider dicomRouteProvider, string studyInstanceUID = null)
        {
            EnsureArg.IsNotNull(baseUri, nameof(baseUri));
            EnsureArg.IsNotNull(dicomRouteProvider, nameof(dicomRouteProvider));

            string retrieveUrl = string.IsNullOrWhiteSpace(studyInstanceUID) ? null : _dicomRouteProvider.GetStudyUri(baseUri, studyInstanceUID).ToString();

            _dataset = new DicomDataset { { DicomTag.RetrieveURL, retrieveUrl } };
            _baseUri = baseUri;
            _dicomRouteProvider = dicomRouteProvider;
        }

        public HttpStatusCode StatusCode { get; private set; } = HttpStatusCode.BadRequest;

        public DicomDataset GetResponseDataset() => _dataset;

        public void AddStoreOutcome(StoreOutcome storeOutcome)
        {
            DicomIdentity dicomIdentity = storeOutcome.DicomIdentity;

            if (storeOutcome.IsStored)
            {
                AddSuccess(dicomIdentity);
            }
            else
            {
                AddFailure(dicomIdentity);
            }
        }

        private void AddSuccess(DicomIdentity dicomIdentity)
        {
            EnsureArg.IsNotNull(dicomIdentity);

            string retrieveUrl = _dicomRouteProvider.GetInstanceUri(
                                                                _baseUri,
                                                                dicomIdentity.StudyInstanceUID,
                                                                dicomIdentity.SeriesInstanceUID,
                                                                dicomIdentity.SopInstanceUID).ToString();
            var item = new DicomDataset()
            {
                { DicomTag.ReferencedSOPClassUID, dicomIdentity.SopClassUID },
                { DicomTag.ReferencedSOPInstanceUID, dicomIdentity.SopInstanceUID },
                { DicomTag.RetrieveURL, retrieveUrl },
            };

            DicomSequence referencedSopSequence = _dataset.Contains(DicomTag.ReferencedSOPSequence) ?
                                                        _dataset.GetSequence(DicomTag.ReferencedSOPSequence) :
                                                        new DicomSequence(DicomTag.ReferencedSOPSequence);

            referencedSopSequence.Items.Add(item);

            // If any failures when adding, we return Accepted, otherwise OK.
            StatusCode = _failureAdded ? HttpStatusCode.Accepted : HttpStatusCode.OK;

            _dataset.AddOrUpdate(referencedSopSequence);
            _successAdded = true;
        }

        private void AddFailure(DicomIdentity dicomIdentity)
        {
            // If we have added any successfully we return Accepted, otherwise a specific status code.
            StatusCode = _successAdded ? HttpStatusCode.Accepted : HttpStatusCode.Conflict;
            _failureAdded = true;

            DicomSequence failedSopSequence = GetFailedSopSequence();

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

            _dataset.AddOrUpdate(failedSopSequence);
        }

        private DicomSequence GetFailedSopSequence()
        {
            return _dataset.Contains(DicomTag.FailedSOPSequence) ?
                                            _dataset.GetSequence(DicomTag.FailedSOPSequence) :
                                            new DicomSequence(DicomTag.FailedSOPSequence);
        }
    }
}
