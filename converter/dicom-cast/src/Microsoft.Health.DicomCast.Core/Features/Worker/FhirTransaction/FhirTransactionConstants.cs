// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.DicomCast.Core.Features.Worker.FhirTransaction
{
    public static class FhirTransactionConstants
    {
        public const string EndpointConnectionTypeSystem = "http://terminology.hl7.org/CodeSystem/endpoint-connection-type";
        public const string EndpointConnectionTypeCode = "dicom-wado-rs";
        public const string EndpointName = "DICOM WADO-RS endpoint";
        public const string EndpointPayloadTypeText = "DICOM WADO-RS";
        public const string DicomMimeType = "application/dicom";

        public const string ModalityInSystem = "DCM";

        public const string AccessionNumberTypeSystem = "http://terminology.hl7.org/CodeSystem/v2-0203";
        public const string AccessionNumberTypeCode = "ACSN";

        // Refer http://dicom.nema.org/medical/dicom/current/output/html/part05.html#PS3.5 for format
        public const string UtcTimezoneOffsetFormat = "&zzxx";
    }
}
