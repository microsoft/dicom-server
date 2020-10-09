// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Dicom;
using Microsoft.Health.Dicom.Core.Features.Query;
using Microsoft.Health.Dicom.Core.Models;

namespace Microsoft.Health.Dicom.SqlServer.Features.Query
{
    public class CustomTagVRCodeRetriever : IVRCodeRetriever
    {
        public DicomVR Retrieve(DicomAttributeId attributeId)
        {
            throw new System.NotImplementedException();
        }
    }
}
