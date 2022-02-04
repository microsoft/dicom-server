// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag
{
    /// <summary>
    /// Representation of a extended query tag entry.
    /// </summary>
    public abstract class ExtendedQueryTagEntry : QueryTagEntry
    {
        /// <summary>
        /// Identification code of private tag implementer.
        /// </summary>
        public string PrivateCreator { get; set; }

        public override string ToString()
        {
            return $"Path: {Path}, VR:{VR}, PrivateCreator:{PrivateCreator}";
        }
    }
}
