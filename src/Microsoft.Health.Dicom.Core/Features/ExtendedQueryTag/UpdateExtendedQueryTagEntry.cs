// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;

namespace Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag
{
    /// <summary>
    /// Encapsulate parameters for updating extended query tag.
    /// </summary>
    public class UpdateExtendedQueryTagEntry
    {
        public UpdateExtendedQueryTagEntry(QueryStatus queryStatus)
        {
            QueryStatus = EnsureArg.EnumIsDefined(queryStatus, nameof(queryStatus));
        }

        /// <summary>
        /// Gets or sets query status.
        /// </summary>        
        public QueryStatus QueryStatus { get; }
    }
}
