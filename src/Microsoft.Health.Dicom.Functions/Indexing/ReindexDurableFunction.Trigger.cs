// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Indexing;
using Microsoft.Health.Dicom.Core.Features.Retrieve;
using Microsoft.Health.Dicom.Functions.Indexing.Configuration;
using Microsoft.Health.SqlServer.Features.Schema;
using Microsoft.Health.SqlServer.Features.Schema.Manager;

namespace Microsoft.Health.Dicom.Functions.Indexing
{
    /// <summary>
    /// Represents the Azure Durable Functions that perform the re-indexing of previously added DICOM instances
    /// based on new tags configured by the user.
    /// </summary>
    public partial class ReindexDurableFunction
    {
        private readonly ReindexConfiguration _reindexConfig;
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

        /// <summary>
        /// The http trigger to add extended Query tags
        /// </summary>
        /// <param name="request">The http request.</param>
        /// <param name="client">The client.</param>
        /// <param name="logger">The logger.</param>
        /// <returns>The task.</returns>
        [FunctionName(nameof(StartAddingTagsAsync))]
        public async Task<HttpResponseMessage> StartAddingTagsAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "Post", Route = "extendedquerytags")] HttpRequestMessage request,
            [DurableClient] IDurableOrchestrationClient client,
            ILogger logger)
        {
            EnsureArg.IsNotNull(request, nameof(request));
            EnsureArg.IsNotNull(client, nameof(client));
            EnsureArg.IsNotNull(logger, nameof(logger));
            var extendedQueryTags = await request.Content.ReadAsAsync<List<AddExtendedQueryTagEntry>>();
            logger.LogInformation("Start adding extended query tags {input}", extendedQueryTags);
            string instanceId = await client.StartNewAsync(nameof(AddAndReindexTagsAsync), instanceId: null, extendedQueryTags);
            logger.LogInformation("Started new orchestration with instanceId {instancId}", instanceId);

            // TODO: these code need to be updated based on contract to client.
            return new HttpResponseMessage { Content = new StringContent(instanceId) };
        }

    }
}
