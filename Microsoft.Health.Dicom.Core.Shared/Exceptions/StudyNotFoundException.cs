// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Dicom.Core.Shared;

namespace Microsoft.Health.Dicom.Core.Exceptions
{
    public class StudyNotFoundException : ResourceNotFoundException
    {
        public StudyNotFoundException()
            : base(DicomCoreResource.StudyNotFound)
        {
        }

        public StudyNotFoundException(string message)
            : base(message)
        {
        }
    }
}
