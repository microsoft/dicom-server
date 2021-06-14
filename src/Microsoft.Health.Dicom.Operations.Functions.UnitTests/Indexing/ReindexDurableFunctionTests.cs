// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Indexing;
using Microsoft.Health.Dicom.Core.Features.Retrieve;
using Microsoft.Health.Dicom.Operations.Functions.Indexing;
using Microsoft.Health.Dicom.Operations.Functions.Indexing.Configuration;
using NSubstitute;

namespace Microsoft.Health.Dicom.Operations.Functions.UnitTests.Indexing
{
    public partial class ReindexDurableFunctionTests
    {
        private readonly ReindexConfiguration _reindexConfig;
        private readonly IReindexStore _reindexStore;
        private readonly IInstanceReindexer _instanceReindexer;
        private readonly IAddExtendedQueryTagService _addExtendedQueryTagService;
        private readonly IInstanceStore _instanceStore;
        private readonly IExtendedQueryTagStore _extendedQueryTagStore;
        private readonly ReindexDurableFunction _reindexDurableFunction;

        public ReindexDurableFunctionTests()
        {
            _reindexConfig = new ReindexConfiguration();
            _reindexStore = Substitute.For<IReindexStore>();
            _instanceReindexer = Substitute.For<IInstanceReindexer>();
            _addExtendedQueryTagService = Substitute.For<IAddExtendedQueryTagService>();
            _instanceStore = Substitute.For<IInstanceStore>();
            _extendedQueryTagStore = Substitute.For<IExtendedQueryTagStore>();
            _reindexDurableFunction = new ReindexDurableFunction(
                Options.Create(new IndexingConfiguration() { Add = _reindexConfig }),
                _addExtendedQueryTagService,
                _reindexStore,
                _instanceStore,
                _instanceReindexer,
                _extendedQueryTagStore);
        }
    }
}
