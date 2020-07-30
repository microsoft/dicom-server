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
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.WebUtilities;
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

        /// <summary>
        /// Called during serialization to create the <see cref="JsonWriter"/>.
        /// </summary>
        /// <param name="writer">The <see cref="TextWriter"/> used to write.</param>
        /// <returns>The <see cref="JsonWriter"/> used during serialization.</returns>
        protected virtual JsonWriter CreateJsonWriter(TextWriter writer)
        {
            EnsureArg.IsNotNull(writer, nameof(writer));

            var jsonWriter = new JsonTextWriter(writer)
            {
                CloseOutput = false,
                AutoCompleteOnClose = false,
            };

            return jsonWriter;
        }

        public override async Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
        {
            EnsureArg.IsNotNull(context, nameof(context));
            EnsureArg.IsNotNull(selectedEncoding, nameof(selectedEncoding));

            HttpResponse response = context.HttpContext.Response;
            await using var fileBufferingWriteStream = new FileBufferingWriteStream();

            await using (TextWriter textWriter = context.WriterFactory(fileBufferingWriteStream, selectedEncoding))
            {
                using (var jsonWriter = CreateJsonWriter(textWriter))
                {
                    _jsonSerializer.Serialize(jsonWriter, context.Object);

                    await jsonWriter.FlushAsync();
                }
            }

            response.ContentLength = fileBufferingWriteStream.Length;
            await fileBufferingWriteStream.DrainBufferAsync(response.Body);
        }
    }
}
