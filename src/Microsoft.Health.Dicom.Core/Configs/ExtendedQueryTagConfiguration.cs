// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Core.Configs
{
    /// <summary>
    /// Configuration for extended query tag feature.
    /// </summary>
    public class ExtendedQueryTagConfiguration
    {
        /// <summary>
        /// Maximum allowed number of tags.
        /// </summary>
        public int MaxAllowedCount { get; set; } = 128;
    }
}
