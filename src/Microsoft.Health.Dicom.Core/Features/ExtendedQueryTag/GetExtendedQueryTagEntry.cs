// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag
{
    /// <summary>
    /// External representation of a extended query tag entry for get.
    /// </summary>
    public class GetExtendedQueryTagEntry : ExtendedQueryTagEntry
    {
        /// <summary>
        /// Status of this tag. Represents the current state the tag is in.
        /// </summary>
        public ExtendedQueryTagStatus Status { get; set; }

        /// <summary>
        /// Level of this tag. Could be Study, Series or Instance.
        /// </summary>
        public QueryTagLevel Level { get; set; }

        public override string ToString()
        {
            return $"Path: {Path}, VR:{VR}, PrivateCreator:{PrivateCreator}, Level:{Level}, Status:{Status}";
        }
    }
}
