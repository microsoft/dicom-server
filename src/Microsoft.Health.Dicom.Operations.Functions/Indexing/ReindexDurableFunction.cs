// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Indexing;
using Microsoft.Health.Dicom.Core.Features.Retrieve;
using Microsoft.Health.Dicom.Operations.Functions.Indexing.Configuration;

namespace Microsoft.Health.Dicom.Operations.Functions.Indexing
{
    /// <summary>
    /// Represents the Azure Durable Functions that perform the re-indexing of previously added DICOM instances
    /// based on new tags configured by the user.
    /// </summary>
    public partial class ReindexDurableFunction
    {
        private readonly ReindexOperationConfiguration _reindexConfig;
        private readonly IReindexStore _reindexStore;
        private readonly IInstanceReindexer _instanceReindexer;
        private readonly IAddExtendedQueryTagService _addExtendedQueryTagService;
        private readonly IInstanceStore _instanceStore;
        private readonly IExtendedQueryTagStore _extendedQueryTagStore;
        private readonly ISchemaManagerDataStore _schemaManagerDataStore;
        private readonly SchemaInformation _schemaInformation;

        public ReindexDurableFunction(
            IOptions<DicomFunctionsConfiguration> configOptions,
            IAddExtendedQueryTagService addExtendedQueryTagService,
            IReindexStore reindexStore,
            IInstanceStore instanceStore,
            IInstanceReindexer instanceReindexer,
            IExtendedQueryTagStore extendedQueryTagStore,
            ISchemaManagerDataStore schemaManagerDataStore,
            SchemaInformation schemaInformation)
        {
            EnsureArg.IsNotNull(configOptions, nameof(configOptions));
            EnsureArg.IsNotNull(reindexStore, nameof(reindexStore));
            EnsureArg.IsNotNull(instanceReindexer, nameof(instanceReindexer));
            EnsureArg.IsNotNull(addExtendedQueryTagService, nameof(addExtendedQueryTagService));
            EnsureArg.IsNotNull(instanceStore, nameof(instanceStore));
            EnsureArg.IsNotNull(extendedQueryTagStore, nameof(extendedQueryTagStore));
            EnsureArg.IsNotNull(schemaManagerDataStore, nameof(schemaManagerDataStore));
            EnsureArg.IsNotNull(schemaInformation, nameof(schemaInformation));
            _reindexConfig = configOptions.Value.Reindex;
            _reindexStore = reindexStore;
            _instanceReindexer = instanceReindexer;
            _addExtendedQueryTagService = addExtendedQueryTagService;
            _instanceStore = instanceStore;
            _extendedQueryTagStore = extendedQueryTagStore;
            _schemaManagerDataStore = schemaManagerDataStore;
            _schemaInformation = schemaInformation;
        }
    }
}
