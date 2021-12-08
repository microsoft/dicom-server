// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.DicomCast.TableStorage.Configs
{
    public class TableDataStoreConfiguration
    {
        public string ConnectionString { get; set; }

        public Uri EndpointUri { get; set; }

        public string ManagedIdentityClientId { get; set; }
    }
}
