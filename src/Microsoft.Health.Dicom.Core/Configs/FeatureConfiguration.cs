// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Core.Configs
{
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
        /// Enables ExtendedQueryTags feature.
        /// </summary>
        public bool EnableExtendedQueryTags { get; set; }

        /// <summary>
        /// Enables AutoGenerateId feature.
        /// </summary>
        public bool EnableAutoGenerateUID { get; set; }
    }
}
