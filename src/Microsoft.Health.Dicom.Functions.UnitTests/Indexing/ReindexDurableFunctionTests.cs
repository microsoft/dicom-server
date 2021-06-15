// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Indexing;
using Microsoft.Health.Dicom.Core.Features.Retrieve;
using Microsoft.Health.Dicom.SqlServer.Features.Schema;
using Microsoft.Health.SqlServer.Features.Schema;
using Microsoft.Health.SqlServer.Features.Schema.Manager;
using Microsoft.Health.Dicom.Functions.Indexing;
using NSubstitute;
using Microsoft.Health.Dicom.Operations.Functions.Configs;

namespace Microsoft.Health.Dicom.Functions.UnitTests.Indexing
{
    public partial class ReindexDurableFunctionTests
    {
        private readonly ReindexOperationConfiguration _reindexConfig;
        private readonly IReindexStore _reindexStore;
        private readonly IInstanceReindexer _instanceReindexer;
        private readonly IAddExtendedQueryTagService _addExtendedQueryTagService;
        private readonly IInstanceStore _instanceStore;
        private readonly IExtendedQueryTagStore _extendedQueryTagStore;
        private readonly ReindexDurableFunction _reindexDurableFunction;
        private readonly ISchemaManagerDataStore _schemaManagerDataStore;
        private readonly SchemaInformation _schemaInformation;

        public ReindexDurableFunctionTests()
        {
            _reindexConfig = new ReindexOperationConfiguration();
            _reindexStore = Substitute.For<IReindexStore>();
            _instanceReindexer = Substitute.For<IInstanceReindexer>();
            _addExtendedQueryTagService = Substitute.For<IAddExtendedQueryTagService>();
            _instanceStore = Substitute.For<IInstanceStore>();
            _extendedQueryTagStore = Substitute.For<IExtendedQueryTagStore>();
            _schemaManagerDataStore = Substitute.For<ISchemaManagerDataStore>();
            _schemaInformation = new SchemaInformation(SchemaVersionConstants.Min, SchemaVersionConstants.Max);
            _reindexDurableFunction = new ReindexDurableFunction(
                Options.Create(new DicomFunctionsConfiguration() { Reindex = _reindexConfig }),
                _addExtendedQueryTagService,
                _reindexStore,
                _instanceStore,
                _instanceReindexer,
                _extendedQueryTagStore,
                _schemaManagerDataStore,
                _schemaInformation);
        }
    }
}
