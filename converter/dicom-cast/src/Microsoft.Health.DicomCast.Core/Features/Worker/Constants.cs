// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.DicomCast.Core.Features.Worker;
public static class Constants
{
    // Represents different metrics the dicomcastworker will emit.
    public const string CastToFhirForbidden = "Cast-To-Fhir-Forbidden";
    public const string DicomToCastforbidden = "Dicom-To-Cast-Forbidden";
    public const string CastMIUnavailable = "Cast-Mi-Unavailable";
    public const string CastingFailedForOtherReasons = "Casting-Failed";
}
