// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Indexing;
using Microsoft.Health.Dicom.Core.Features.Retrieve;
using Microsoft.Health.Dicom.SqlServer.Features.Schema;
using Microsoft.Health.Dicom.Functions.Indexing;
using NSubstitute;

namespace Microsoft.Health.Dicom.Functions.UnitTests.Indexing
{
    public partial class ReindexDurableFunctionTests
    {
        private readonly QueryTagIndexingOptions _reindexConfig;
        private readonly IInstanceReindexer _instanceReindexer;
        private readonly IAddExtendedQueryTagService _addExtendedQueryTagService;
        private readonly IInstanceStore _instanceStore;
        private readonly IExtendedQueryTagStore _extendedQueryTagStore;
        private readonly ReindexDurableFunction _reindexDurableFunction;
        private readonly ISchemaVersionResolver _schemaVersionResolver;

        public ReindexDurableFunctionTests()
        {
            _reindexConfig = new QueryTagIndexingOptions();
            _instanceReindexer = Substitute.For<IInstanceReindexer>();
            _addExtendedQueryTagService = Substitute.For<IAddExtendedQueryTagService>();
            _instanceStore = Substitute.For<IInstanceStore>();
            _extendedQueryTagStore = Substitute.For<IExtendedQueryTagStore>();
            _schemaVersionResolver = Substitute.For<ISchemaVersionResolver>();
            var configuration = Substitute.For<IOptions<QueryTagIndexingOptions>>();
            configuration.Value.Returns(_reindexConfig);

            _reindexDurableFunction = new ReindexDurableFunction(
                configuration,
                _addExtendedQueryTagService,
                _instanceStore,
                _instanceReindexer,
                _extendedQueryTagStore,
                _schemaVersionResolver);
        }
    }
}
