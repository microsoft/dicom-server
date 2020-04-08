// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.Health.Dicom.Core.Exceptions;

namespace Microsoft.Health.Dicom.Api.Web
{
    internal class MissingMultipartBodyPartException : DicomServerException
    {
        public MissingMultipartBodyPartException(Exception innerException)
            : base(DicomApiResource.MissingMultipartBodyPart, innerException)
        {
        }
    }
}
