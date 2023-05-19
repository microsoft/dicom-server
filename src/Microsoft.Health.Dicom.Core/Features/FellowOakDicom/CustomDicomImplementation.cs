// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Reflection;
using FellowOakDicom;

namespace Microsoft.Health.Dicom.Core.Features.FellowOakDicom;

/// <summary>
/// Represents a custom DICOM implementation.
/// </summary>
public static class CustomDicomImplementation
{
    /// <summary>
    /// Azure Health Data Services specific OID registered under Microsoft OID arc.
    /// Used to identify the DICOM implementation Class UID.
    /// </summary>
    private const string ImplementationClassUid = "1.3.6.1.4.1.311.129";

    /// <summary>
    /// This method sets the DICOM implementation class UID and version.
    /// ImplementationClassUID and ImplementationVersion are used to identify the software that generated or last touched the data.
    /// </summary>
    public static void SetDicomImplementationClassUIDAndVersion()
    {
        Assembly assembly = typeof(CustomDicomImplementation).GetTypeInfo().Assembly;
        AssemblyFileVersionAttribute fileVersionAttribute = assembly.GetCustomAttribute<AssemblyFileVersionAttribute>();

        DicomImplementation.ClassUID = new DicomUID(ImplementationClassUid, "Implementation Class UID", DicomUidType.Unknown);
        DicomImplementation.Version = fileVersionAttribute?.Version ?? "Unknown";
    }
}
