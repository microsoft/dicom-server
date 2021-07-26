// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Indexing;
using Microsoft.Health.Dicom.Core.Features.Retrieve;
using Microsoft.Health.Dicom.Functions.Indexing;
using Microsoft.Health.Dicom.SqlServer.Features.Schema;
using Newtonsoft.Json;
using NSubstitute;

namespace Microsoft.Health.Dicom.Functions.UnitTests.Indexing
{
    public partial class ReindexDurableFunctionTests
    {
        private readonly ReindexDurableFunction _reindexDurableFunction;
        private readonly IExtendedQueryTagStore _extendedQueryTagStore;
        private readonly IInstanceStore _instanceStore;
        private readonly IInstanceReindexer _instanceReindexer;
        private readonly ISchemaVersionResolver _schemaVersionResolver;
        private readonly JsonSerializer _serializer;
        private readonly QueryTagIndexingOptions _options;

        public ReindexDurableFunctionTests()
        {
            _extendedQueryTagStore = Substitute.For<IExtendedQueryTagStore>();
            _instanceStore = Substitute.For<IInstanceStore>();
            _instanceReindexer = Substitute.For<IInstanceReindexer>();
            _schemaVersionResolver = Substitute.For<ISchemaVersionResolver>();
            _serializer = JsonSerializer.CreateDefault();
            _options = new QueryTagIndexingOptions();
            _reindexDurableFunction = new ReindexDurableFunction(
                _extendedQueryTagStore,
                _instanceStore,
                _instanceReindexer,
                _schemaVersionResolver,
                _serializer,
                Options.Create(_options));
        }
    }
}
