// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.ComponentModel.DataAnnotations;

namespace Microsoft.Health.Dicom.Operations.Functions.Indexing.Configuration
{
    /// <summary>
    /// Represents configuration settings related to the indexing of data.
    /// </summary>
    public class IndexingConfiguration
    {
        public const string SectionName = "Indexing";

        /// <summary>
        /// Gets or sets the settings for re-indexing DICOM instances
        /// </summary>
        [Required]
        public ReindexConfiguration Add { get; set; }
    }
}
