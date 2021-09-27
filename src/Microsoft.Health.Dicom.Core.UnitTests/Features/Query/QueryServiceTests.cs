// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Features.Query;
using Microsoft.Health.Dicom.Core.Features.Query.Model;
using Microsoft.Health.Dicom.Tests.Common;
using Microsoft.Health.Dicom.Tests.Common.Comparers;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Query
{
    public class QueryServiceTests
    {
        private readonly QueryService _queryService;
        private readonly IQueryParser _queryParser;
        private readonly IQueryStore _queryStore;
        private readonly IQueryTagService _queryTagService;

        public QueryServiceTests()
        {
            _queryParser = Substitute.For<IQueryParser>();
            _queryStore = Substitute.For<IQueryStore>();
            _queryTagService = Substitute.For<IQueryTagService>();

            _queryService = new QueryService(
                _queryParser,
                _queryStore,
                Substitute.For<IMetadataStore>(),
                _queryTagService);
        }

        [Theory]
        [InlineData(QueryResource.StudySeries, "123.001")]
        [InlineData(QueryResource.StudyInstances, "abc.1234")]
        public void GivenQidoQuery_WithInvalidStudyInstanceUid_ThrowsValidationException(QueryResource resourceType, string studyInstanceUid)
        {
            var parameters = new QueryParameters
            {
                Filters = new Dictionary<string, string>(),
                QueryResourceType = resourceType,
                StudyInstanceUid = studyInstanceUid
            };
            Assert.ThrowsAsync<InvalidIdentifierException>(() => _queryService.QueryAsync(parameters, CancellationToken.None));
        }

        [Theory]
        [InlineData(QueryResource.StudySeriesInstances, "123.111", "1234.001")]
        [InlineData(QueryResource.StudySeriesInstances, "123.abc", "1234.001")]
        public void GivenQidoQuery_WithInvalidStudySeriesUid_ThrowsValidationException(QueryResource resourceType, string studyInstanceUid, string seriesInstanceUid)
        {
            var parameters = new QueryParameters
            {
                Filters = new Dictionary<string, string>(),
                QueryResourceType = resourceType,
                SeriesInstanceUid = seriesInstanceUid,
                StudyInstanceUid = studyInstanceUid,
            };
            Assert.ThrowsAsync<InvalidIdentifierException>(() => _queryService.QueryAsync(parameters, CancellationToken.None));
        }

        [Theory]
        [InlineData(QueryResource.AllInstances)]
        [InlineData(QueryResource.StudyInstances)]
        [InlineData(QueryResource.StudySeriesInstances)]
        public async void GivenRequestForInstances_WhenRetrievingQueriableExtendedQueryTags_ReturnsAllTags(QueryResource resourceType)
        {
            _queryParser.Parse(default, default).ReturnsForAnyArgs(new QueryExpression(default, default, default, default, default, Array.Empty<QueryFilterCondition>(), Array.Empty<string>()));
            var parameters = new QueryParameters
            {
                Filters = new Dictionary<string, string>(),
                QueryResourceType = resourceType,
                SeriesInstanceUid = TestUidGenerator.Generate(),
                StudyInstanceUid = TestUidGenerator.Generate(),
            };
            List<ExtendedQueryTagStoreEntry> storeEntries = new List<ExtendedQueryTagStoreEntry>()
            {
                new ExtendedQueryTagStoreEntry(1, "00741000", "CS", null, QueryTagLevel.Instance, ExtendedQueryTagStatus.Ready, QueryStatus.Enabled, 0),
                new ExtendedQueryTagStoreEntry(2, "0040A121", "DA", null, QueryTagLevel.Series, ExtendedQueryTagStatus.Ready, QueryStatus.Enabled, 0),
                new ExtendedQueryTagStoreEntry(3, "00101005", "PN", null, QueryTagLevel.Study, ExtendedQueryTagStatus.Ready, QueryStatus.Enabled, 0),
            };

            var list = QueryTagService.CoreQueryTags.Concat(storeEntries.Select(item => new QueryTag(item))).ToList();
            _queryTagService.GetQueryTagsAsync().ReturnsForAnyArgs(list);
            _queryStore.QueryAsync(Arg.Any<QueryExpression>(), Arg.Any<CancellationToken>()).ReturnsForAnyArgs(new QueryResult(new List<VersionedInstanceIdentifier>()));
            await _queryService.QueryAsync(parameters, CancellationToken.None);

            _queryParser.Received().Parse(parameters, Arg.Do<IReadOnlyCollection<QueryTag>>(x => Assert.Equal(x, list, QueryTagComparer.Default)));
        }

        [Theory]
        [InlineData(QueryResource.AllSeries)]
        [InlineData(QueryResource.StudySeries)]
        public async void GivenRequestForSeries_WhenRetrievingQueriableExtendedQueryTags_ReturnsSeriesAndStudyTags(QueryResource resourceType)
        {
            _queryParser.Parse(default, default).ReturnsForAnyArgs(new QueryExpression(default, default, default, default, default, Array.Empty<QueryFilterCondition>(), Array.Empty<string>()));
            var parameters = new QueryParameters
            {
                Filters = new Dictionary<string, string>(),
                QueryResourceType = resourceType,
                SeriesInstanceUid = TestUidGenerator.Generate(),
                StudyInstanceUid = TestUidGenerator.Generate(),
            };
            List<ExtendedQueryTagStoreEntry> storeEntries = new List<ExtendedQueryTagStoreEntry>()
            {
                new ExtendedQueryTagStoreEntry(1, "00741000", "CS", null, QueryTagLevel.Instance, ExtendedQueryTagStatus.Ready, QueryStatus.Enabled, 0),
                new ExtendedQueryTagStoreEntry(2, "0040A121", "DA", null, QueryTagLevel.Series, ExtendedQueryTagStatus.Ready, QueryStatus.Enabled, 0),
                new ExtendedQueryTagStoreEntry(3, "00101005", "PN", null, QueryTagLevel.Study, ExtendedQueryTagStatus.Ready, QueryStatus.Enabled, 0),
            };

            var list = QueryTagService.CoreQueryTags.Concat(storeEntries.Select(item => new QueryTag(item))).ToList();
            _queryStore.QueryAsync(Arg.Any<QueryExpression>(), Arg.Any<CancellationToken>()).ReturnsForAnyArgs(new QueryResult(new List<VersionedInstanceIdentifier>()));
            await _queryService.QueryAsync(parameters, CancellationToken.None);

            _queryParser.Received().Parse(parameters, Arg.Do<IReadOnlyCollection<QueryTag>>(x => Assert.Equal(x, list, QueryTagComparer.Default)));
        }

        [Theory]
        [InlineData(QueryResource.AllStudies)]
        public async void GivenRequestForStudies_WhenRetrievingQueriableExtendedQueryTags_ReturnsStudyTags(QueryResource resourceType)
        {
            _queryParser.Parse(default, default).ReturnsForAnyArgs(new QueryExpression(default, default, default, default, default, Array.Empty<QueryFilterCondition>(), Array.Empty<string>()));
            var parameters = new QueryParameters
            {
                Filters = new Dictionary<string, string>(),
                QueryResourceType = resourceType,
                SeriesInstanceUid = TestUidGenerator.Generate(),
                StudyInstanceUid = TestUidGenerator.Generate(),
            };

            List<ExtendedQueryTagStoreEntry> storeEntries = new List<ExtendedQueryTagStoreEntry>()
            {
                new ExtendedQueryTagStoreEntry(1, "00741000", "CS", null, QueryTagLevel.Instance, ExtendedQueryTagStatus.Ready, QueryStatus.Enabled, 0),
                new ExtendedQueryTagStoreEntry(2, "0040A121", "DA", null, QueryTagLevel.Series, ExtendedQueryTagStatus.Ready, QueryStatus.Enabled, 0),
                new ExtendedQueryTagStoreEntry(3, "00101005", "PN", null, QueryTagLevel.Study, ExtendedQueryTagStatus.Ready, QueryStatus.Enabled, 0),
            };

            var list = QueryTagService.CoreQueryTags.Concat(storeEntries.Select(item => new QueryTag(item))).ToList();
            _queryStore.QueryAsync(Arg.Any<QueryExpression>(), Arg.Any<CancellationToken>()).ReturnsForAnyArgs(new QueryResult(new List<VersionedInstanceIdentifier>()));
            await _queryService.QueryAsync(parameters, CancellationToken.None);

            _queryParser.Received().Parse(parameters, Arg.Do<IReadOnlyCollection<QueryTag>>(x => Assert.Equal(x, list, QueryTagComparer.Default)));
        }
    }
}
