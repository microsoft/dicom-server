// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Net;
using Dicom;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.Routing;

namespace Microsoft.Health.Dicom.Core.Features.Resources.Store
{
    /// <summary>
    /// http://dicom.nema.org/medical/dicom/current/output/chtml/part18/sect_6.6.html#table_6.6.1-4
    /// </summary>
    internal class WadoStoreResponseBuilder
    {
        private const ushort ProcessingFailure = 272;
        private readonly DicomDataset _dataset;
        private readonly IDicomRouteProvider _dicomRouteProvider;
        private readonly string _studyInstanceUID;
        private bool _successAdded = false;
        private bool _failureAdded = false;

        public WadoStoreResponseBuilder(string baseAddress, IDicomRouteProvider dicomRouteProvider, string studyInstanceUID)
        {
            EnsureArg.IsNotNull(dicomRouteProvider);

            _dicomRouteProvider = dicomRouteProvider;
            _studyInstanceUID = studyInstanceUID;
            _dataset = new DicomDataset
            {
                {
                    DicomTag.RetrieveURL,
                    string.IsNullOrWhiteSpace(studyInstanceUID) ? null : _dicomRouteProvider.GetStudyUri(baseAddress, studyInstanceUID).ToString()
                },
            };
        }

        public HttpStatusCode StatusCode { get; private set; } = HttpStatusCode.BadRequest;

        public DicomDataset GetResponseDataset() => _dataset;

        public void AddFailure(DicomIdentity dicomIdentity)
        {
            DicomDataset responseDataset = null;

            if (dicomIdentity != null && !dicomIdentity.IsIdentifiable)
            {
                responseDataset = new DicomDataset()
                {
                    { DicomTag.ReferencedSOPClassUID, dicomIdentity.SopClassUID },
                    { DicomTag.ReferencedSOPInstanceUID, dicomIdentity.SopInstanceUID },
                    { DicomTag.FailureReason, ProcessingFailure },
                };
            }

            AddFailedSopInstance(responseDataset);
        }

        private void AddFailedSopInstance(DicomDataset item)
        {
            // If we have added any successfully we return Accepted, otherwise a specific status code.
            StatusCode = _successAdded ? HttpStatusCode.Accepted : HttpStatusCode.Conflict;
            _failureAdded = true;
            DicomSequence failedSopSequence = GetFailedSopSequence();
            if (item != null)
            {
                failedSopSequence.Items.Add(item);
            }

            _dataset.AddOrUpdate(failedSopSequence);
        }

        private DicomSequence GetFailedSopSequence()
        {
            return _dataset.Contains(DicomTag.FailedSOPSequence) ?
                                            _dataset.GetSequence(DicomTag.FailedSOPSequence) :
                                            new DicomSequence(DicomTag.FailedSOPSequence);
        }

        public void AddResult(string baseAddress, DicomIdentity dicomIdentity)
        {
            EnsureArg.IsNotNull(dicomIdentity);
            if (!dicomIdentity.IsIdentifiable || (_studyInstanceUID != null && _studyInstanceUID != dicomIdentity.StudyInstanceUID))
            {
                // If specified, all instances shall be from that study; instances not matching the StudyInstanceUID shall be rejected.
                AddFailure(dicomIdentity);
                return;
            }

            var item = new DicomDataset()
            {
                { DicomTag.ReferencedSOPClassUID, dicomIdentity.SopClassUID },
                { DicomTag.ReferencedSOPInstanceUID, dicomIdentity.SopInstanceUID },
                {
                    DicomTag.RetrieveURL, _dicomRouteProvider.GetInstanceUri(
                                                                baseAddress,
                                                                dicomIdentity.StudyInstanceUID,
                                                                dicomIdentity.SeriesInstanceUID,
                                                                dicomIdentity.SopInstanceUID).ToString()
                },
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
    }
}
