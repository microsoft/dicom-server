// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Dicom.Core.Features.Model;

namespace Microsoft.Health.Dicom.Core.Features.Reindex
{
    /// <summary>
    /// Representation of a extended query tag entry.
    /// </summary>
    public class ReindexJobReportEntry
    {
        public VersionedInstanceIdentifier Instance { get; set; }

        public ReindexInstanceResult Result { get; set; }
    }
}
