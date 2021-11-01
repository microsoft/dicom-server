// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using EnsureThat;
using FellowOakDicom;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Health.Dicom.Core.Features.ChangeFeed;
using Microsoft.Health.Dicom.Core.Web;

namespace Microsoft.Health.Dicom.Api.Features.Formatters
{
    internal sealed class DicomJsonOutputFormatter : SystemTextJsonOutputFormatter
    {
        public DicomJsonOutputFormatter(JsonOptions jsonOptions)
            : base(EnsureArg.IsNotNull(jsonOptions).JsonSerializerOptions)
        {
            SupportedEncodings.Clear();
            SupportedMediaTypes.Clear();

            SupportedEncodings.Add(Encoding.UTF8);
            SupportedEncodings.Add(Encoding.Unicode);
            SupportedMediaTypes.Add(KnownContentTypes.ApplicationDicomJson);
        }

        protected override bool CanWriteType(Type type)
        {
            if (type == null)
            {
                return false;
            }

            return typeof(DicomDataset).IsAssignableFrom(type) ||
                typeof(IEnumerable<DicomDataset>).IsAssignableFrom(type) ||
                typeof(IEnumerable<ChangeFeedEntry>).IsAssignableFrom(type) ||
                typeof(ChangeFeedEntry).IsAssignableFrom(type);
        }
    }
}
