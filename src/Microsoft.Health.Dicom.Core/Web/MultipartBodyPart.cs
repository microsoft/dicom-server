// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.IO;
using EnsureThat;

namespace Microsoft.Health.Dicom.Core.Web
{
    public class MultipartBodyPart
    {
        public MultipartBodyPart(string contentType, Stream body)
        {
            EnsureArg.IsNotNull(contentType, nameof(contentType));
            EnsureArg.IsNotNull(body, nameof(body));

            ContentType = contentType;
            Body = body;
        }

        public string ContentType { get; }

        public Stream Body { get; }
    }
}
