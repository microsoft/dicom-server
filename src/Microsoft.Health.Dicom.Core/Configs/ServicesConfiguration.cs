// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Core.Configs
{
    public class ServicesConfiguration
    {
        public DeletedInstanceCleanupConfiguration DeletedInstanceCleanup { get; } = new DeletedInstanceCleanupConfiguration();

        public StoreConfiguration StoreServiceSettings { get; } = new StoreConfiguration();

        public ExtendedQueryTagConfiguration ExtendedQueryTag { get; } = new ExtendedQueryTagConfiguration();

    }
}
