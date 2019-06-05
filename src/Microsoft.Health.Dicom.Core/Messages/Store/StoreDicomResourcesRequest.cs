// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using EnsureThat;
using MediatR;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;

namespace Microsoft.Health.Dicom.Core.Messages.Store
{
    public class StoreDicomResourcesRequest : IRequest<StoreDicomResourcesResponse>
    {
        private const string MutipartRelated = "multipart/related";
        private readonly MultipartReader _multipartReader;

        public StoreDicomResourcesRequest(Uri requestBaseUri, Stream requestBody, string requestContentType, string studyInstanceUID = null)
        {
            EnsureArg.IsNotNull(requestBaseUri, nameof(requestBaseUri));
            EnsureArg.IsNotNull(requestBody, nameof(requestBody));
            EnsureArg.IsNotNullOrWhiteSpace(requestContentType, nameof(requestContentType));

            StudyInstanceUID = studyInstanceUID;
            RequestBaseUri = requestBaseUri;

            if (!MediaTypeHeaderValue.TryParse(requestContentType, out MediaTypeHeaderValue media))
            {
                return;
            }

            var isMultipartRelated = media.MediaType.Equals(MutipartRelated, StringComparison.InvariantCultureIgnoreCase);
            var boundary = HeaderUtilities.RemoveQuotes(media.Boundary).ToString();

            if (!isMultipartRelated || string.IsNullOrWhiteSpace(boundary))
            {
                return;
            }

            _multipartReader = new MultipartReader(boundary, requestBody);
        }

        public bool IsMultipartRequest => _multipartReader != null;

        public string StudyInstanceUID { get; }

        public Uri RequestBaseUri { get; }

        public MultipartReader GetMultipartReader()
        {
            if (_multipartReader == null)
            {
                throw new InvalidOperationException("The request was not a multi-part request.");
            }

            return _multipartReader;
        }
    }
}
