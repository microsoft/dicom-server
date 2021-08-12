// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading.Tasks;

namespace Microsoft.Health.Dicom.Tests.Integration.Persistence
{
    public interface IExtendedQueryTagErrorStoreTestHelper
    {
        Task ClearExtendedQueryTagErrorTableAsync();

        Task<bool> DoesExtendedQueryTagErrorExistAsync(int tagKey);
    }
}
