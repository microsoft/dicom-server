// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.DicomCast.TableStorage.Configs
{
    public class TableDataStoreConfiguration
    {
        /// <summary>
        /// The storage table connection string to use. Setting this assumes the use of an account key.
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// The endpoint of the table storage account. Setting this assumes the use of a managed identity to communicate.
        /// </summary>
        public Uri EndpointUri { get; set; }

        /// <summary>
        /// Optional parameter to use to specify the clientId of the managed identity to use.
        /// </summary>
        public string ManagedIdentityClientId { get; set; }
    }
}
