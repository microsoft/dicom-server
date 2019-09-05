// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
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
            Frames = frames;
        }

        private RetrieveDicomResourceRequest(ResourceType resourceType, string requestedTransferSyntax)
        {
            ResourceType = resourceType;
            RequestedTransferSyntax = string.IsNullOrWhiteSpace(requestedTransferSyntax) ?
                                        null :
                                        requestedTransferSyntax.Equals(OriginalTransferSyntaxRequest, StringComparison.InvariantCultureIgnoreCase) ?
                                            null : requestedTransferSyntax;
        }

        public ResourceType ResourceType { get; }

        public string RequestedTransferSyntax { get; }

        public string StudyInstanceUID { get; }

        public string SeriesInstanceUID { get; }

        public string SopInstanceUID { get; }

        public IEnumerable<int> Frames { get; }
    }
}
