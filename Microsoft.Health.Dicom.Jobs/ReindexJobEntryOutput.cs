// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Dicom.Core.Features.Model;

namespace Microsoft.Health.Dicom.Jobs
{
    public class ReindexJobEntryOutput
    {
        public bool Succeed { get; set; }

        public VersionedInstanceIdentifier Identifier { get; set; }
    }
}
