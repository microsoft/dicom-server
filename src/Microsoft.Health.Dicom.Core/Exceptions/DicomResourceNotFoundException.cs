// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.Dicom.Core.Exceptions
{
    public class DicomResourceNotFoundException : DicomServerException
    {
        public DicomResourceNotFoundException()
            : base(DicomCoreResource.ResourceNotFound)
        {
        }

        public DicomResourceNotFoundException(Exception innerException)
            : base(DicomCoreResource.ResourceNotFound, innerException)
        {
        }
    }
}
