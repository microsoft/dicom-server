// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Dicom;
using EnsureThat;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Health.Dicom.Core;

namespace Microsoft.Health.Dicom.Api.Features.Formatters
{
    public class DicomXmlOutputFormatter : TextOutputFormatter
    {
        internal const string ApplicationDicomXml = "application/dicom+xml";

        public DicomXmlOutputFormatter()
        {
            SupportedMediaTypes.Add(ApplicationDicomXml);
            SupportedEncodings.Add(Encoding.UTF8);
            SupportedEncodings.Add(Encoding.Unicode);
        }

        protected override bool CanWriteType(Type type)
        {
            if (type == null)
            {
                return false;
            }

            return typeof(DicomDataset).IsAssignableFrom(type) || typeof(IEnumerable<DicomDataset>).IsAssignableFrom(type);
        }

        public override Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
        {
            EnsureArg.IsNotNull(context, nameof(context));
            EnsureArg.IsNotNull(selectedEncoding, nameof(selectedEncoding));

            byte[] data = selectedEncoding.GetBytes(DicomXML.WriteToXml((DicomDataset)context.Object));
            context.HttpContext.Response.Body.Write(data, 0, data.Length);

            return Task.CompletedTask;
        }
    }
}
