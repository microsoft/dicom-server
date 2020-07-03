// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Dicom;
using Dicom.Serialization;
using EnsureThat;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Health.Dicom.Core.Features.ChangeFeed;
using Microsoft.Health.Dicom.Core.Web;
using Newtonsoft.Json;

namespace Microsoft.Health.Dicom.Api.Features.Formatters
{
    public class DicomJsonOutputFormatter : TextOutputFormatter
    {
        private readonly JsonDicomConverter _jsonDicomConverter = new JsonDicomConverter(writeTagsAsKeywords: false);
        private readonly JsonSerializer _jsonSerializer = new JsonSerializer();

        public DicomJsonOutputFormatter()
        {
            SupportedMediaTypes.Add(KnownContentTypes.ApplicationDicomJson);
            SupportedEncodings.Add(Encoding.UTF8);
            SupportedEncodings.Add(Encoding.Unicode);

            _jsonSerializer.Converters.Add(_jsonDicomConverter);
        }

        protected override bool CanWriteType(Type type)
        {
            if (type == null)
            {
                return false;
            }

            return _jsonDicomConverter.CanConvert(type) ||
                typeof(IEnumerable<DicomDataset>).IsAssignableFrom(type) ||
                typeof(IEnumerable<ChangeFeedEntry>).IsAssignableFrom(type) ||
                typeof(ChangeFeedEntry).IsAssignableFrom(type);
        }

        public override async Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
        {
            EnsureArg.IsNotNull(context, nameof(context));
            EnsureArg.IsNotNull(selectedEncoding, nameof(selectedEncoding));

            using (TextWriter textWriter = context.WriterFactory(context.HttpContext.Response.Body, selectedEncoding))
            {
                using (var writer = new JsonTextWriter(textWriter))
                {
                    _jsonSerializer.Serialize(writer, context.Object);

                    // try async flushing all the underlaying buffers before disposing the writer that is calling sync write
                    await writer.FlushAsync();
                    await textWriter.FlushAsync();
                    await context.HttpContext.Response.Body.FlushAsync();
                    await writer.CloseAsync();
                    await textWriter.DisposeAsync();
                    await context.HttpContext.Response.Body.DisposeAsync();
                }
            }
        }
    }
}
