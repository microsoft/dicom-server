// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag
{
    /// <summary>
    /// Resource type of a query tag.
    /// </summary>
    public enum QueryTagResourceType
    {
        /// <summary>
        /// The image instance resource type
        /// </summary>
        Image = 0,

        /// <summary>
        /// The workitem instance resource type
        /// </summary>
        Workitem = 1
    }
}
