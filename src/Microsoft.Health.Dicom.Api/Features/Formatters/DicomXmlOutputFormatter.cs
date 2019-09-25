// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dicom;
using EnsureThat;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Health.Dicom.Api.Features.ContentTypes;
using Microsoft.Health.Dicom.Api.Features.Responses;
using Microsoft.Health.Dicom.Core;

namespace Microsoft.Health.Dicom.Api.Features.Formatters
{
    public class DicomXmlOutputFormatter : TextOutputFormatter
    {
        public DicomXmlOutputFormatter()
        {
            SupportedEncodings.Add(Encoding.UTF8);
            SupportedEncodings.Add(Encoding.Unicode);

            SupportedMediaTypes.Add(KnownContentTypes.XmlContentType);
        }

        protected override bool CanWriteType(Type type)
        {
            if (type == null)
            {
                return false;
            }

            return typeof(DicomDataset).IsAssignableFrom(type) || typeof(IEnumerable<DicomDataset>).IsAssignableFrom(type);
        }

        public override async Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
        {
            EnsureArg.IsNotNull(context, nameof(context));
            EnsureArg.IsNotNull(selectedEncoding, nameof(selectedEncoding));

            HttpResponse response = context.HttpContext.Response;
            using (TextWriter textWriter = context.WriterFactory(response.Body, selectedEncoding))
            {
                // Arrays are written as multipart items; otherwise serialise directly as XML.
                if (context.Object is IEnumerable<DicomDataset> array)
                {
                    await WriteMultipartContentAsync(response, array, selectedEncoding);
                }
                else
                {
                    await textWriter.WriteAsync(DicomXML.WriteToXml((DicomDataset)context.Object, selectedEncoding));
                }
            }
        }

        private async Task WriteMultipartContentAsync(HttpResponse response, IEnumerable<DicomDataset> dicomItems, Encoding encoding)
        {
            IEnumerable<MultipartItem> multipartItems = dicomItems.Select(
                x => new MultipartItem(KnownContentTypes.XmlContentType, DicomXML.WriteToXml(x, encoding)));
            await MultipartResult.WriteMultipartItemsAsync(response, multipartItems, response.StatusCode);
        }
    }
}
