// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using MediatR;

namespace Microsoft.Health.Dicom.Core.Messages.Retrieve
{
    public class DicomRetrieveResourceRequest : IRequest<DicomRetrieveResourceResponse>
    {
        /// <summary>
        /// If the requested transfer syntax equals '*', the caller is requesting the original transfer syntax of the uploaded file.
        /// </summary>
        private const string OriginalTransferSyntaxRequest = "*";

        public DicomRetrieveResourceRequest(string requestedTransferSyntax, string studyInstanceUid)
            : this(ResourceType.Study, requestedTransferSyntax)
        {
            StudyInstanceUid = studyInstanceUid;
        }

        public DicomRetrieveResourceRequest(string requestedTransferSyntax, string studyInstanceUid, string seriesInstanceUid)
            : this(ResourceType.Series, requestedTransferSyntax)
        {
            StudyInstanceUid = studyInstanceUid;
            SeriesInstanceUid = seriesInstanceUid;
        }

        public DicomRetrieveResourceRequest(
            string requestedTransferSyntax, string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid)
            : this(ResourceType.Instance, requestedTransferSyntax)
        {
            StudyInstanceUid = studyInstanceUid;
            SeriesInstanceUid = seriesInstanceUid;
            SopInstanceUid = sopInstanceUid;
        }

        public DicomRetrieveResourceRequest(
            string requestedTransferSyntax, string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid, IEnumerable<int> frames)
            : this(ResourceType.Frames, requestedTransferSyntax)
        {
            StudyInstanceUid = studyInstanceUid;
            SeriesInstanceUid = seriesInstanceUid;
            SopInstanceUid = sopInstanceUid;

            // Per DICOMWeb spec (http://dicom.nema.org/medical/dicom/current/output/html/part18.html#sect_9.5.1.2.1)
            // frame number in the URI is 1-based, unlike fo-dicom representation where it's 0-based.
            Frames = frames?.Select(x => x - 1);
        }

        private DicomRetrieveResourceRequest(ResourceType resourceType, string requestedRepresentation)
        {
            ResourceType = resourceType;
            RequestedRepresentation = string.IsNullOrWhiteSpace(requestedRepresentation) ? null : requestedRepresentation;
        }

        public ResourceType ResourceType { get; }

        public string RequestedRepresentation { get; }

        public string StudyInstanceUid { get; }

        public string SeriesInstanceUid { get; }

        public string SopInstanceUid { get; }

        public IEnumerable<int> Frames { get; }

        public bool OriginalTransferSyntaxRequested()
        {
            return RequestedRepresentation != null && RequestedRepresentation.Equals(OriginalTransferSyntaxRequest, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
