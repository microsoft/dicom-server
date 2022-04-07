// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Core.Features.Model;
public enum MigrationState
{
    NotStarted = 0,
    DataDuplicated = 1,
    OldDataDeleted = 2
}
