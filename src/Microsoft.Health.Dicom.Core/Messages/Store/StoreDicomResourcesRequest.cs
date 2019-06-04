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

        public StoreDicomResourcesRequest(string baseAddress, Stream requestBody, string requestContentType, string studyInstanceUID = null)
        {
            EnsureArg.IsNotNull(requestBody, nameof(requestBody));
            EnsureArg.IsNotNullOrWhiteSpace(requestContentType, nameof(requestContentType));

            StudyInstanceUID = studyInstanceUID;
            BaseAddress = baseAddress;

            if (!MediaTypeHeaderValue.TryParse(requestContentType, out MediaTypeHeaderValue media))
            {
                return;
            }

            var isMultipartRelated = CheckIsMultipartRelated(media);
            var boundary = HeaderUtilities.RemoveQuotes(media.Boundary).ToString();

            if (!isMultipartRelated || string.IsNullOrWhiteSpace(boundary))
            {
                return;
            }

            _multipartReader = new MultipartReader(boundary, requestBody);
        }

        public bool IsMultipartRequest => _multipartReader != null;

        public string StudyInstanceUID { get; }

        public string BaseAddress { get; }

        public MultipartReader GetMultipartReader()
        {
            if (_multipartReader == null)
            {
                throw new InvalidOperationException("The request was not a multi-part request.");
            }

            return _multipartReader;
        }

        private static bool CheckIsMultipartRelated(MediaTypeHeaderValue mediaType)
            => mediaType.MediaType.Equals(MutipartRelated, StringComparison.InvariantCultureIgnoreCase);
    }
}
