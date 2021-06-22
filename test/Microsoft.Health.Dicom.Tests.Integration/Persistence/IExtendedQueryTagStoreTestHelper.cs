// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Health.Dicom.Tests.Integration.Persistence
{
    public interface IExtendedQueryTagStoreTestHelper
    {
        Task CleanupAsync(CancellationToken cancellationToken = default);
    }
}
