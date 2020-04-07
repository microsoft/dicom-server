// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;

namespace Microsoft.Health.Dicom.Api.Web
{
    internal class MissingMultipartBodyPartException : IOException
    {
        public MissingMultipartBodyPartException(Exception innerException)
            : base(null, innerException)
        {
        }
    }
}
