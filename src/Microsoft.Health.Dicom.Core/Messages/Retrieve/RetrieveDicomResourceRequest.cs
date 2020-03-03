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
    public class RetrieveDicomResourceRequest : IRequest<RetrieveDicomResourceResponse>
    {
        /// <summary>
        /// If the requested transfer syntax equals '*', the caller is requesting the original transfer syntax of the uploaded file.
        /// </summary>
        private const string OriginalTransferSyntaxRequest = "*";

        public RetrieveDicomResourceRequest(string requestedTransferSyntax, string studyInstanceUID)
            : this(ResourceType.Study, requestedTransferSyntax)
        {
            StudyInstanceUID = studyInstanceUID;
        }

        public RetrieveDicomResourceRequest(string requestedTransferSyntax, string studyInstanceUID, string seriesInstanceUID)
            : this(ResourceType.Series, requestedTransferSyntax)
        {
            StudyInstanceUID = studyInstanceUID;
            SeriesInstanceUID = seriesInstanceUID;
        }

        public RetrieveDicomResourceRequest(
            string requestedTransferSyntax, string studyInstanceUID, string seriesInstanceUID, string sopInstanceUID)
            : this(ResourceType.Instance, requestedTransferSyntax)
        {
            StudyInstanceUID = studyInstanceUID;
            SeriesInstanceUID = seriesInstanceUID;
            SopInstanceUID = sopInstanceUID;
        }

        public RetrieveDicomResourceRequest(
            string requestedTransferSyntax, string studyInstanceUID, string seriesInstanceUID, string sopInstanceUID, IEnumerable<int> frames)
            : this(ResourceType.Frames, requestedTransferSyntax)
        {
            StudyInstanceUID = studyInstanceUID;
            SeriesInstanceUID = seriesInstanceUID;
            SopInstanceUID = sopInstanceUID;

            // Per DICOMWeb spec (http://dicom.nema.org/medical/dicom/current/output/html/part18.html#sect_9.5.1.2.1)
            // frame number in the URI is 1-based, unlike fo-dicom representation where it's 0-based.
            Frames = frames?.Select(x => x - 1);
        }

        private RetrieveDicomResourceRequest(ResourceType resourceType, string requestedRepresentation)
        {
            ResourceType = resourceType;
            RequestedRepresentation = string.IsNullOrWhiteSpace(requestedRepresentation) ? null : requestedRepresentation;
        }

        public ResourceType ResourceType { get; }

        public string RequestedRepresentation { get; }

        public string StudyInstanceUID { get; }

        public string SeriesInstanceUID { get; }

        public string SopInstanceUID { get; }

        public IEnumerable<int> Frames { get; }

        public bool OriginalTransferSyntaxRequested()
        {
            return RequestedRepresentation != null && RequestedRepresentation.Equals(OriginalTransferSyntaxRequest, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
