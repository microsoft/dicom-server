// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Core.Configs;

public class FeatureConfiguration
{
    /// <summary>
    /// Gets or sets a value indicating OHIF viewer should be enabled or not.
    /// </summary>
    public bool EnableOhifViewer { get; set; }

    /// <summary>
    /// Enables stricter validation of each DicomItem value based on their VR type
    /// </summary>
    public bool EnableFullDicomItemValidation { get; set; }

    /// <summary>
    /// Enables Data Partition feature.
    /// </summary>
    public bool EnableDataPartitions { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether bulk export is enabled.
    /// </summary>
    public bool EnableExport { get; set; }

    /// <summary>
    /// Enables the latest API version
    /// </summary>
    public bool EnableLatestApiVersion { get; set; }
}
