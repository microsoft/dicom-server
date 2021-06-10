// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.ComponentModel.DataAnnotations;

namespace Microsoft.Health.Dicom.Functions.Indexing.Configuration
{
    /// <summary>
    /// Represents configuration settings related to the indexing of data.
    /// </summary>
    public class SqlServerConfiguration
    {
        public const string SectionName = "SqlServer";

        /// <summary>
        /// Gets or sets the settings for re-indexing DICOM instances
        /// </summary>
        [Required]
        public string ConnectionString { get; set; }
    }
}
