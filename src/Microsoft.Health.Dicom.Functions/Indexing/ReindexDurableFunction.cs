// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Indexing;
using Microsoft.Health.Dicom.Core.Features.Retrieve;
using Microsoft.Health.Dicom.SqlServer.Features.Schema;

namespace Microsoft.Health.Dicom.Functions.Indexing
{
    /// <summary>
    /// Represents the Azure Durable Functions that perform the re-indexing of previously added DICOM instances
    /// based on new tags configured by the user.
    /// </summary>
    public partial class ReindexDurableFunction
    {
        private readonly IStoreFactory<IExtendedQueryTagStore> _extendedQueryTagStoreFactory;
        private readonly IStoreFactory<IInstanceStore> _instanceStoreFactory;
        private readonly IInstanceReindexer _instanceReindexer;
        private readonly ISchemaVersionResolver _schemaVersionResolver;
        private readonly QueryTagIndexingOptions _options;

        public ReindexDurableFunction(
            IStoreFactory<IExtendedQueryTagStore> extendedQueryTagStoreFactory,
            IStoreFactory<IInstanceStore> instanceStoreFactory,
            IInstanceReindexer instanceReindexer,
            ISchemaVersionResolver schemaVersionResolver,
            IOptions<QueryTagIndexingOptions> configOptions)
        {
            _extendedQueryTagStoreFactory = EnsureArg.IsNotNull(extendedQueryTagStoreFactory, nameof(extendedQueryTagStoreFactory));
            _instanceStoreFactory = EnsureArg.IsNotNull(instanceStoreFactory, nameof(instanceStoreFactory));
            _instanceReindexer = EnsureArg.IsNotNull(instanceReindexer, nameof(instanceReindexer));
            _schemaVersionResolver = EnsureArg.IsNotNull(schemaVersionResolver, nameof(schemaVersionResolver));
            _options = EnsureArg.IsNotNull(configOptions?.Value, nameof(configOptions));
        }
    }
}
