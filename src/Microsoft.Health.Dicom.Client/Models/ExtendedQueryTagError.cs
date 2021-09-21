// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.Dicom.Client.Models
{
    /// <summary>
    /// Represents a problem that arose when re-indexing the given DICOM instance.
    /// </summary>
    public class ExtendedQueryTagError
    {
        /// <summary>
        /// Gets or sets the DICOM study instance UID.
        /// </summary>
        public string StudyInstanceUid { get; set; }

        /// <summary>
        /// Gets or sets the DICOM series instance UID.
        /// </summary>
        public string SeriesInstanceUid { get; set; }

        /// <summary>
        /// Gets or sets the DICOM SOP instance UID.
        /// </summary>
        public string SopInstanceUid { get; set; }

        /// <summary>
        /// Gets or sets the date and time when the error was found.
        /// </summary>
        public DateTime CreatedTime { get; set; }

        /// <summary>
        /// Gets or sets the localized error message.
        /// </summary>
        public string ErrorMessage { get; set; }
    }
}
