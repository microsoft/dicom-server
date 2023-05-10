// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Reflection;
using FellowOakDicom;

namespace Microsoft.Health.Dicom.Core.Features.FellowOakDicom;

/// <summary>
/// 
/// </summary>
public static class CustomDicomImplementation
{
    /// <summary>
    /// 
    /// </summary>
    private const string ImplementationClassUid = "1.3.6.1.4.1.311.129";

    /// <summary>
    /// 
    /// </summary>
    public static void SetFellowOakDicomImplementation()
    {
        Version version = typeof(CustomDicomImplementation).GetTypeInfo().Assembly.GetName().Version;

        DicomImplementation.ClassUID = new DicomUID(ImplementationClassUid, "Implementation Class UID", DicomUidType.Unknown);
        DicomImplementation.Version = $"{version.Major}.{version.Minor}.{version.Build}";
    }
}
