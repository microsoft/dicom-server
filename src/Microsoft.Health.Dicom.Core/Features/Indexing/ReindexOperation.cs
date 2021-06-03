// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Core.Features.Indexing
{
    /// <summary>
    /// External representation of a extended query tag entry for add.
    /// </summary>
    public class ReindexOperation : Operation
    {
        public long StartWatermark { get; set; }

        public long EndWatermark { get; set; }
    }
}
