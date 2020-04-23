// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Clients
{
    public class DicomWebException<T> : DicomWebException
    {
        public DicomWebException(DicomWebResponse<T> response)
            : base(response)
        {
        }

        public T Value => ((DicomWebResponse<T>)Response).Value;
    }
}
