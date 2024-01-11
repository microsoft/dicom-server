// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FellowOakDicom;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Context;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Features.Partitioning;
using Microsoft.Health.Dicom.Core.Features.Query;
using Microsoft.Health.Dicom.Core.Features.Query.Model;
using Microsoft.Health.Dicom.Tests.Common;
using Microsoft.Health.Dicom.Tests.Common.Comparers;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Query;

public class QueryServiceTests
{
    private readonly QueryService _queryService;
    private readonly IQueryParser<QueryExpression, QueryParameters> _queryParser;
    private readonly IQueryStore _queryStore;
    private readonly IMetadataStore _metadataStore;
    private readonly IQueryTagService _queryTagService;
    private readonly IDicomRequestContextAccessor _contextAccessor;

    public QueryServiceTests()
    {
        _queryParser = Substitute.For<IQueryParser<QueryExpression, QueryParameters>>();
        _queryStore = Substitute.For<IQueryStore>();
        _metadataStore = Substitute.For<IMetadataStore>();
        _queryTagService = Substitute.For<IQueryTagService>();
        _contextAccessor = Substitute.For<IDicomRequestContextAccessor>();
        _contextAccessor.RequestContext.DataPartition = Partition.Default;

        _queryService = new QueryService(
            _queryParser,
            _queryStore,
            _metadataStore,
            _queryTagService,
            _contextAccessor,
            Substitute.For<ILogger<QueryService>>());
    }

    [Theory]
    [InlineData(QueryResource.StudySeries, "123.001", Skip = "Enable once UID validation rejects leading zeroes.")]
    [InlineData(QueryResource.StudyInstances, "abc.1234")]
    public Task GivenQidoQuery_WithInvalidStudyInstanceUid_ThrowsValidationException(QueryResource resourceType, string studyInstanceUid)
    {
        var parameters = new QueryParameters
        {
            Filters = new Dictionary<string, string>(),
            QueryResourceType = resourceType,
            StudyInstanceUid = studyInstanceUid
        };

        return Assert.ThrowsAsync<InvalidIdentifierException>(() => _queryService.QueryAsync(parameters, CancellationToken.None));
    }

    [Theory]
    [InlineData(QueryResource.StudySeriesInstances, "123.111", "1234.001", Skip = "Enable once UID validation rejects leading zeroes.")]
    [InlineData(QueryResource.StudySeriesInstances, "123.abc", "1234.001")]
    public Task GivenQidoQuery_WithInvalidStudySeriesUid_ThrowsValidationException(QueryResource resourceType, string studyInstanceUid, string seriesInstanceUid)
    {
        var parameters = new QueryParameters
        {
            Filters = new Dictionary<string, string>(),
            QueryResourceType = resourceType,
            SeriesInstanceUid = seriesInstanceUid,
            StudyInstanceUid = studyInstanceUid,
        };

        return Assert.ThrowsAsync<InvalidIdentifierException>(() => _queryService.QueryAsync(parameters, CancellationToken.None));
    }

    [Theory]
    [InlineData(QueryResource.AllInstances)]
    [InlineData(QueryResource.StudyInstances)]
    [InlineData(QueryResource.StudySeriesInstances)]
    public async Task GivenRequestForInstances_WhenRetrievingQueriableExtendedQueryTags_ReturnsAllTags(QueryResource resourceType)
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
        _queryStore.QueryAsync(Arg.Any<int>(), Arg.Any<QueryExpression>(), Arg.Any<CancellationToken>()).ReturnsForAnyArgs(new QueryResult(new List<VersionedInstanceIdentifier>()));
        await _queryService.QueryAsync(parameters, CancellationToken.None);

        _queryParser.Received().Parse(parameters, Arg.Do<IReadOnlyCollection<QueryTag>>(x => Assert.Equal(x, list, QueryTagComparer.Default)));
    }

    [Theory]
    [InlineData(QueryResource.AllSeries)]
    [InlineData(QueryResource.StudySeries)]
    public async Task GivenRequestForSeries_WhenRetrievingQueriableExtendedQueryTags_ReturnsSeriesAndStudyTags(QueryResource resourceType)
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
        _queryStore.QueryAsync(Arg.Any<int>(), Arg.Any<QueryExpression>(), Arg.Any<CancellationToken>()).ReturnsForAnyArgs(new QueryResult(new List<VersionedInstanceIdentifier>()));
        await _queryService.QueryAsync(parameters, CancellationToken.None);

        _queryParser.Received().Parse(parameters, Arg.Do<IReadOnlyCollection<QueryTag>>(x => Assert.Equal(x, list, QueryTagComparer.Default)));
    }

    [Theory]
    [InlineData(QueryResource.AllStudies)]
    public async Task GivenRequestForStudies_WhenRetrievingQueriableExtendedQueryTags_ReturnsStudyTags(QueryResource resourceType)
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
        _queryStore.QueryAsync(Arg.Any<int>(), Arg.Any<QueryExpression>(), Arg.Any<CancellationToken>()).ReturnsForAnyArgs(new QueryResult(new List<VersionedInstanceIdentifier>()));
        await _queryService.QueryAsync(parameters, CancellationToken.None);

        _queryParser.Received().Parse(parameters, Arg.Do<IReadOnlyCollection<QueryTag>>(x => Assert.Equal(x, list, QueryTagComparer.Default)));
    }

    [Fact]
    public async Task GivenRequest_WhenV2DefaultStudyExpected_OnlyStudyResultPathIsCalled()
    {
        VersionedInstanceIdentifier identifier = new VersionedInstanceIdentifier(TestUidGenerator.Generate(), TestUidGenerator.Generate(), TestUidGenerator.Generate(), 1);
        _queryParser.Parse(default, default).ReturnsForAnyArgs(
            new QueryExpression(QueryResource.AllStudies, new QueryIncludeField(new List<DicomTag>()), default, 0, 0, Array.Empty<QueryFilterCondition>(), Array.Empty<string>()));
        _queryStore.QueryAsync(Arg.Any<int>(), Arg.Any<QueryExpression>(), Arg.Any<CancellationToken>()).ReturnsForAnyArgs(new QueryResult(new List<VersionedInstanceIdentifier>() { identifier }));
        _contextAccessor.RequestContext.Version = 2;

        await _queryService.QueryAsync(new QueryParameters(), CancellationToken.None);

        await _queryStore.Received().GetStudyResultAsync(Arg.Any<int>(), Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>());
        await _queryStore.DidNotReceive().GetSeriesResultAsync(Arg.Any<int>(), Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>());
        await _metadataStore.DidNotReceive().GetInstanceMetadataAsync(Arg.Any<long>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GivenRequest_WhenDefaultStudySeriesExpected_OnlyStudySeriesResultPathIsCalled()
    {
        VersionedInstanceIdentifier identifier = new VersionedInstanceIdentifier(TestUidGenerator.Generate(), TestUidGenerator.Generate(), TestUidGenerator.Generate(), 1);
        _queryParser.Parse(default, default).ReturnsForAnyArgs(
            new QueryExpression(QueryResource.AllSeries, new QueryIncludeField(new List<DicomTag>()), default, 0, 0, Array.Empty<QueryFilterCondition>(), Array.Empty<string>()));
        _queryStore.QueryAsync(Arg.Any<int>(), Arg.Any<QueryExpression>(), Arg.Any<CancellationToken>()).ReturnsForAnyArgs(new QueryResult(new List<VersionedInstanceIdentifier>() { identifier }));
        _contextAccessor.RequestContext.Version = 2;
        var studyResults = GenerateStudyResults(identifier.StudyInstanceUid);
        var seriesResults = GenerateSeriesResults(identifier.StudyInstanceUid, identifier.SeriesInstanceUid, TestUidGenerator.Generate());
        _queryStore.GetStudyResultAsync(Arg.Any<int>(), Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>()).Returns(studyResults);
        _queryStore.GetSeriesResultAsync(Arg.Any<int>(), Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>()).Returns(seriesResults);

        var response = await _queryService.QueryAsync(new QueryParameters(), CancellationToken.None);

        await _queryStore.Received().GetStudyResultAsync(Arg.Any<int>(), Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>());
        await _queryStore.Received().GetSeriesResultAsync(Arg.Any<int>(), Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>());
        await _metadataStore.DidNotReceive().GetInstanceMetadataAsync(Arg.Any<long>(), Arg.Any<CancellationToken>());
        Assert.Equal(2, response.ResponseDataset.Count());
        ValidationResponse(response.ResponseDataset, studyResults.Single(), seriesResults);
    }

    [Fact]
    public async Task GivenRequest_WhenDefaultSeriesExpected_OnlySeriesResultPathIsCalled()
    {
        VersionedInstanceIdentifier identifier = new VersionedInstanceIdentifier(TestUidGenerator.Generate(), TestUidGenerator.Generate(), TestUidGenerator.Generate(), 1);
        _queryParser.Parse(default, default).ReturnsForAnyArgs(
            new QueryExpression(QueryResource.StudySeries, new QueryIncludeField(new List<DicomTag>()), default, 0, 0, Array.Empty<QueryFilterCondition>(), Array.Empty<string>()));
        _queryStore.QueryAsync(Arg.Any<int>(), Arg.Any<QueryExpression>(), Arg.Any<CancellationToken>()).ReturnsForAnyArgs(new QueryResult(new List<VersionedInstanceIdentifier>() { identifier }));
        _contextAccessor.RequestContext.Version = 2;

        await _queryService.QueryAsync(new QueryParameters(), CancellationToken.None);

        await _queryStore.DidNotReceive().GetStudyResultAsync(Arg.Any<int>(), Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>());
        await _queryStore.Received().GetSeriesResultAsync(Arg.Any<int>(), Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>());
        await _metadataStore.DidNotReceive().GetInstanceMetadataAsync(Arg.Any<long>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GivenRequest_WhenAllRequests_MetadataPathCalled()
    {
        VersionedInstanceIdentifier identifier = new VersionedInstanceIdentifier(TestUidGenerator.Generate(), TestUidGenerator.Generate(), TestUidGenerator.Generate(), 1);
        _queryParser.Parse(default, default).ReturnsForAnyArgs(
            new QueryExpression(QueryResource.AllStudies, QueryIncludeField.AllFields, default, 0, 0, Array.Empty<QueryFilterCondition>(), Array.Empty<string>()));
        _queryStore.QueryAsync(Arg.Any<int>(), Arg.Any<QueryExpression>(), Arg.Any<CancellationToken>()).ReturnsForAnyArgs(new QueryResult(new List<VersionedInstanceIdentifier>() { identifier }));
        _contextAccessor.RequestContext.Version = 2;

        await _queryService.QueryAsync(new QueryParameters(), CancellationToken.None);

        await _queryStore.DidNotReceive().GetStudyResultAsync(Arg.Any<int>(), Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>());
        await _queryStore.DidNotReceive().GetSeriesResultAsync(Arg.Any<int>(), Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>());
        await _metadataStore.Received().GetInstanceMetadataAsync(Arg.Any<long>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GivenRequest_WhenAllRequestsAndComputedRequested_MetadataPathAndStudyResultCalled()
    {
        var includeFields = new QueryIncludeField(new List<DicomTag>() { DicomTag.PatientAdditionalPosition, DicomTag.ProposedStudySequence, DicomTag.ModalitiesInStudy });
        VersionedInstanceIdentifier identifier = new VersionedInstanceIdentifier(TestUidGenerator.Generate(), TestUidGenerator.Generate(), TestUidGenerator.Generate(), 1);
        _queryParser.Parse(default, default).ReturnsForAnyArgs(
            new QueryExpression(QueryResource.AllStudies, includeFields, default, 0, 0, Array.Empty<QueryFilterCondition>(), Array.Empty<string>()));
        _queryStore.QueryAsync(Arg.Any<int>(), Arg.Any<QueryExpression>(), Arg.Any<CancellationToken>()).ReturnsForAnyArgs(new QueryResult(new List<VersionedInstanceIdentifier>() { identifier }));
        _contextAccessor.RequestContext.Version = 2;
        var studyResults = GenerateStudyResults(identifier.StudyInstanceUid);
        var metadataResult = GenerateMetadataStoreResponse(identifier);
        _queryStore.GetStudyResultAsync(Arg.Any<int>(), Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>()).Returns(studyResults);
        _metadataStore.GetInstanceMetadataAsync(Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(metadataResult);

        var response = await _queryService.QueryAsync(new QueryParameters(), CancellationToken.None);

        await _queryStore.Received().GetStudyResultAsync(Arg.Any<int>(), Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>());
        await _queryStore.DidNotReceive().GetSeriesResultAsync(Arg.Any<int>(), Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>());
        await _metadataStore.Received().GetInstanceMetadataAsync(Arg.Any<long>(), Arg.Any<CancellationToken>());
        Assert.Single(response.ResponseDataset);
        ValidationResponse(response.ResponseDataset.Single(), studyResults.Single(), metadataResult);
    }

    [Fact]
    public async Task GivenRequest_WhenDefaultSeriesWithComputedCalled_StudyPathIsNotCalled()
    {
        var includeFields = new QueryIncludeField(new List<DicomTag>() { DicomTag.PatientAdditionalPosition, DicomTag.NumberOfSeriesRelatedInstances, DicomTag.Modality });
        VersionedInstanceIdentifier identifier = new VersionedInstanceIdentifier(TestUidGenerator.Generate(), TestUidGenerator.Generate(), TestUidGenerator.Generate(), 1);
        _queryParser.Parse(default, default).ReturnsForAnyArgs(
            new QueryExpression(QueryResource.AllSeries, includeFields, default, 0, 0, Array.Empty<QueryFilterCondition>(), Array.Empty<string>()));
        _queryStore.QueryAsync(Arg.Any<int>(), Arg.Any<QueryExpression>(), Arg.Any<CancellationToken>()).ReturnsForAnyArgs(new QueryResult(new List<VersionedInstanceIdentifier>() { identifier }));
        _contextAccessor.RequestContext.Version = 2;

        await _queryService.QueryAsync(new QueryParameters(), CancellationToken.None);

        await _queryStore.DidNotReceive().GetStudyResultAsync(Arg.Any<int>(), Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>());
        await _queryStore.Received().GetSeriesResultAsync(Arg.Any<int>(), Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>());
        await _metadataStore.Received().GetInstanceMetadataAsync(Arg.Any<long>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GivenRequest_WhenStudiesDeletedFromDBAfterQueryResultBeforeComputeCalled_RequestShouldSucceed()
    {
        var includeFields = new QueryIncludeField(new List<DicomTag>() { DicomTag.PatientAdditionalPosition, DicomTag.ProposedStudySequence, DicomTag.ModalitiesInStudy });
        VersionedInstanceIdentifier identifier = new VersionedInstanceIdentifier(TestUidGenerator.Generate(), TestUidGenerator.Generate(), TestUidGenerator.Generate(), 1);
        _queryParser.Parse(default, default).ReturnsForAnyArgs(
            new QueryExpression(QueryResource.AllStudies, includeFields, default, 0, 0, Array.Empty<QueryFilterCondition>(), Array.Empty<string>()));
        _queryStore.QueryAsync(Arg.Any<int>(), Arg.Any<QueryExpression>(), Arg.Any<CancellationToken>()).ReturnsForAnyArgs(new QueryResult(new List<VersionedInstanceIdentifier>() { identifier }));
        _contextAccessor.RequestContext.Version = 2;
        var studyResults = new List<StudyResult>();
        var metadataResult = GenerateMetadataStoreResponse(identifier);
        _queryStore.GetStudyResultAsync(Arg.Any<int>(), Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>()).Returns(studyResults);
        _metadataStore.GetInstanceMetadataAsync(Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(metadataResult);

        var response = await _queryService.QueryAsync(new QueryParameters(), CancellationToken.None);

        await _queryStore.Received().GetStudyResultAsync(Arg.Any<int>(), Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>());
        await _queryStore.DidNotReceive().GetSeriesResultAsync(Arg.Any<int>(), Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>());
        await _metadataStore.Received().GetInstanceMetadataAsync(Arg.Any<long>(), Arg.Any<CancellationToken>());
        Assert.Empty(response.ResponseDataset);
    }

    private static IReadOnlyCollection<StudyResult> GenerateStudyResults(string studyInstanceUid)
    {
        var studyResult = new StudyResult()
        {
            StudyInstanceUid = studyInstanceUid,
            StudyDescription = "test",
            PatientId = "1234",
            ModalitiesInStudy = new[] { "CT", "MR" }
        };
        return new List<StudyResult>() { studyResult };
    }

    private static IReadOnlyCollection<SeriesResult> GenerateSeriesResults(string studyInstanceUid, params string[] seriesInstanceUids)
    {
        var seriesResults = new List<SeriesResult>();

        foreach (string seUid in seriesInstanceUids)
        {
            var seriesResult = new SeriesResult()
            {
                StudyInstanceUid = studyInstanceUid,
                SeriesInstanceUid = seUid,
                Modality = "CT"
            };
            seriesResults.Add(seriesResult);
        }
        return seriesResults;
    }

    private static void ValidationResponse(IEnumerable<DicomDataset> responseDataset, StudyResult studyResult, IReadOnlyCollection<SeriesResult> seriesResults)
    {
        Dictionary<string, DicomDataset> keyValues = new Dictionary<string, DicomDataset>();
        foreach (SeriesResult result in seriesResults)
        {
            var ds = new DicomDataset(result.DicomDataset);
            ds.AddOrUpdate(studyResult.DicomDataset);
            ds.Remove(DicomTag.NumberOfSeriesRelatedInstances);
            ds.Remove(DicomTag.NumberOfStudyRelatedInstances);
            ds.Remove(DicomTag.ModalitiesInStudy);
            keyValues.Add(result.SeriesInstanceUid, ds);
        }

        foreach (DicomDataset items in responseDataset)
        {
            Assert.Equal(keyValues[items.GetSingleValue<string>(DicomTag.SeriesInstanceUID)], items);
        }
    }

    private static void ValidationResponse(DicomDataset responseDataset, StudyResult studyResult, DicomDataset metadataResult)
    {
        var ds = new DicomDataset(studyResult.DicomDataset);
        ds.AddOrUpdate(metadataResult);
        ds.Remove(DicomTag.NumberOfStudyRelatedInstances);

        Assert.Equal(ds, responseDataset);
    }

    private static DicomDataset GenerateMetadataStoreResponse(VersionedInstanceIdentifier identifier)
    {
        return new DicomDataset()
        {
            { DicomTag.StudyInstanceUID, identifier.StudyInstanceUid },
            { DicomTag.PatientAdditionalPosition, "foobar" }
        };
    }


}
