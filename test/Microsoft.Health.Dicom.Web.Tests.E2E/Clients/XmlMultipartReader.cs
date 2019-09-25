// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Dicom;
using EnsureThat;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Health.Dicom.Core;
using Microsoft.Net.Http.Headers;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Clients
{
    public class XmlMultipartReader : IDisposable
    {
        private const string MutipartRelated = "multipart/related";
        private readonly string _multipartBoundary;
        private readonly HttpResponseMessage _responseMessage;
        private bool _disposed;

        public XmlMultipartReader(HttpResponseMessage responseMessage)
        {
            EnsureArg.IsNotNull(responseMessage, nameof(responseMessage));
            EnsureArg.IsTrue(MediaTypeHeaderValue.TryParse(responseMessage.Content.Headers.ContentType.MediaType, out MediaTypeHeaderValue media), nameof(responseMessage));

            var isMultipartRelated = media.MediaType.Equals(MutipartRelated, StringComparison.InvariantCultureIgnoreCase);
            EnsureArg.IsTrue(isMultipartRelated, "The response message content must be 'multipart/related'.");

            _multipartBoundary = HeaderUtilities.RemoveQuotes(media.Boundary).ToString();
            _responseMessage = responseMessage;
        }

        public async Task<IReadOnlyList<DicomDataset>> ReadAsync()
        {
            var result = new List<DicomDataset>();
            using (Stream stream = await _responseMessage.Content.ReadAsStreamAsync())
            {
                MultipartMemoryStreamProvider multipartProvider = await _responseMessage.Content.ReadAsMultipartAsync();

                foreach (HttpContent content in multipartProvider.Contents)
                {
                    var xml = await content.ReadAsStringAsync();
                    result.Add(DicomXML.ConvertXMLToDicom(xml));
                }
            }

            return result;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                _responseMessage.Dispose();
            }

            _disposed = true;
        }
    }
}
