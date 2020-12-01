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

        // Refer to https://docs.microsoft.com/en-us/dotnet/standard/base-types/custom-date-and-time-format-strings#zzzSpecifier for how to convert in c#
        public const string UtcTimezoneOffsetFormat = "zzz";
    }
}
