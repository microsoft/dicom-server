// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dicom.Serialization;
using EnsureThat;
using Microsoft.AspNetCore.Mvc.Formatters;
using Newtonsoft.Json;

namespace Microsoft.Health.Dicom.Api.Features.Formatters
{
    public class DicomJsonOutputFormatter : TextOutputFormatter
    {
        public const string ApplicationDicomJson = "application/dicom+json";
        private readonly JsonDicomConverter _jsonDicomConverter = new JsonDicomConverter(writeTagsAsKeywords: false);
        private readonly JsonSerializer _jsonSerializer = new JsonSerializer();

        public DicomJsonOutputFormatter()
        {
            SupportedMediaTypes.Add(ApplicationDicomJson);
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

            // Check if the type is a collection and check we can convert the collection item type.
            if (typeof(ICollection).IsAssignableFrom(type))
            {
                return _jsonDicomConverter.CanConvert(GetEnumeratedType(type));
            }

            return _jsonDicomConverter.CanConvert(type);
        }

        public override Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
        {
            EnsureArg.IsNotNull(context, nameof(context));
            EnsureArg.IsNotNull(selectedEncoding, nameof(selectedEncoding));

            using (TextWriter textWriter = context.WriterFactory(context.HttpContext.Response.Body, selectedEncoding))
            {
                using (var writer = new JsonTextWriter(textWriter))
                {
                    _jsonSerializer.Serialize(writer, context.Object);
                }
            }

            return Task.CompletedTask;
        }

        private static Type GetEnumeratedType(Type type) => type.GetElementType() ?? type.GenericTypeArguments.Single();
    }
}
