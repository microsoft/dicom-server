// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.Dicom.Core.Exceptions
{
    public class DicomInstanceMetadataNotFoundException : Exception
    {
        public DicomInstanceMetadataNotFoundException(string message)
           : base(message)
        {
        }
    }
}
