// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Dicom.Core.Shared;

namespace Microsoft.Health.Dicom.Core.Exceptions
{
    public class SeriesNotFoundException : ResourceNotFoundException
    {
        public SeriesNotFoundException()
            : base(DicomCoreResource.SeriesNotFound)
        {
        }

        public SeriesNotFoundException(string message)
            : base(message)
        {
        }
    }
}
