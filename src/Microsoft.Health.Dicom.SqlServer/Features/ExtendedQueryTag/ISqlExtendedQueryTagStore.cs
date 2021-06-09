// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.SqlServer.Feature.Common;

namespace Microsoft.Health.Dicom.SqlServer.Features.ExtendedQueryTag
{
    internal interface ISqlExtendedQueryTagStore : IExtendedQueryTagStore, IVersioned
    {
    }
}
