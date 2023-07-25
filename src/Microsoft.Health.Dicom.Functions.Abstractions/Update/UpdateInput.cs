// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Functions.Update;

public class UpdateInput
{
    public string InputIdentifier { get; set; }

    public int TotalNumberOfStudies { get; set; }
}
