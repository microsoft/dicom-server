// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Core.Exceptions
{
    public class InstanceNotFoundException : ResourceNotFoundException
    {
        public InstanceNotFoundException()
            : base(DicomCoreResource.InstanceNotFound)
        {
        }

        public InstanceNotFoundException(string message)
            : base(message)
        {
        }
    }
}
