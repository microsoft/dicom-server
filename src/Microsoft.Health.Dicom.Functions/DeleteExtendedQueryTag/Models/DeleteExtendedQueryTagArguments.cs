// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Functions.DeleteExtendedQueryTag.Models;

public class DeleteExtendedQueryTagArguments
{
    public int TagKey { get; set; }

    public string VR { get; set; }
}
