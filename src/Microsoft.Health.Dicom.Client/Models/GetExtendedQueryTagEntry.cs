// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Client.Models
{
    /// <summary>
    /// External representation of a extended query tag entry when retrieving.
    /// </summary>
    public class GetExtendedQueryTagEntry
    {
        /// <summary>
        /// Path of this tag. Normally it's composed of groupid and elementid.
        /// E.g: 00100020 is path of patient id.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// VR of this tag.
        /// </summary>
        public string VR { get; set; }

        /// <summary>
        /// Level of this tag. Could be Study, Series or Instance.
        /// </summary>
        public QueryTagLevel Level { get; set; }

        /// <summary>
        /// Status of this tag.
        /// </summary>
        public ExtendedQueryTagStatus Status { get; set; }

        /// <summary>
        /// Identification code of private tag implementer.
        /// </summary>
        public string PrivateCreator { get; set; }
    }
}
