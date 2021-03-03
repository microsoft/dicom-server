// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using Dicom;
using Microsoft.Extensions.Primitives;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.CustomTag;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Features.Query;
using Microsoft.Health.Dicom.Core.Features.Query.Model;
using Microsoft.Health.Dicom.Core.Messages.Query;
using Microsoft.Health.Dicom.Tests.Common;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Query
{
    public class DicomQueryServiceTests
    {
        private readonly QueryService _queryService;
        private readonly IQueryParser _queryParser;
        private readonly IQueryStore _queryStore;
        private readonly ICustomTagStore _customTagStore;

        public DicomQueryServiceTests()
        {
            _queryParser = Substitute.For<IQueryParser>();
            _queryStore = Substitute.For<IQueryStore>();
            _customTagStore = Substitute.For<ICustomTagStore>();
            _queryService = new QueryService(
                _queryParser,
                _queryStore,
                Substitute.For<IMetadataStore>(),
                _customTagStore,
                new DicomTagParser());
        }

        [Theory]
        [InlineData(QueryResource.StudySeries, "123.001")]
        [InlineData(QueryResource.StudyInstances, "abc.1234")]
        public void GivenQidoQuery_WithInvalidStudyInstanceUid_ThrowsValidationException(QueryResource resourceType, string studyInstanceUid)
        {
            var request = new QueryResourceRequest(
                Substitute.For<IEnumerable<KeyValuePair<string, StringValues>>>(),
                resourceType,
                studyInstanceUid);
            Assert.ThrowsAsync<InvalidIdentifierException>(async () => await _queryService.QueryAsync(request, CancellationToken.None));
        }

        [Theory]
        [InlineData(QueryResource.StudySeriesInstances, "123.111", "1234.001")]
        [InlineData(QueryResource.StudySeriesInstances, "123.abc", "1234.001")]
        public void GivenQidoQuery_WithInvalidStudySeriesUid_ThrowsValidationException(QueryResource resourceType, string studyInstanceUid, string seriesInstanceUid)
        {
            var request = new QueryResourceRequest(
                Substitute.For<IEnumerable<KeyValuePair<string, StringValues>>>(),
                resourceType,
                studyInstanceUid,
                seriesInstanceUid);
            Assert.ThrowsAsync<InvalidIdentifierException>(async () => await _queryService.QueryAsync(request, CancellationToken.None));
        }

        [Theory]
        [InlineData(QueryResource.AllInstances)]
        [InlineData(QueryResource.StudyInstances)]
        [InlineData(QueryResource.StudySeriesInstances)]
        public async void GivenRequestForInstances_WhenRetrievingQueriableCustomTags_ReturnsAllTags(QueryResource resourceType)
        {
            var request = new QueryResourceRequest(
                Substitute.For<IEnumerable<KeyValuePair<string, StringValues>>>(),
                resourceType,
                TestUidGenerator.Generate(),
                TestUidGenerator.Generate());

            List<CustomTagStoreEntry> storeEntries = new List<CustomTagStoreEntry>()
            {
                new CustomTagStoreEntry(1, "00741000", "CS", CustomTagLevel.Instance, CustomTagStatus.Added),
                new CustomTagStoreEntry(2, "0040A121", "DA", CustomTagLevel.Series, CustomTagStatus.Added),
                new CustomTagStoreEntry(3, "00101005", "PN", CustomTagLevel.Study, CustomTagStatus.Added),
            };

            _customTagStore.GetCustomTagsAsync().ReturnsForAnyArgs(storeEntries);
            _queryStore.QueryAsync(Arg.Any<QueryExpression>(), Arg.Any<CancellationToken>()).ReturnsForAnyArgs(new QueryResult(new List<VersionedInstanceIdentifier>()));
            await _queryService.QueryAsync(request, CancellationToken.None);

            HashSet<CustomTagFilterDetails> filterDetails = new HashSet<CustomTagFilterDetails>()
            {
                new CustomTagFilterDetails(1, CustomTagLevel.Instance, "CS", DicomTag.ProcedureStepState),
                new CustomTagFilterDetails(2, CustomTagLevel.Series, "DA", DicomTag.Date),
                new CustomTagFilterDetails(3, CustomTagLevel.Study, "PN", DicomTag.PatientBirthName),
            };

            _queryParser.Received().Parse(request, Arg.Is<HashSet<CustomTagFilterDetails>>(x => x.SetEquals(filterDetails)));
        }

        [Theory]
        [InlineData(QueryResource.AllSeries)]
        [InlineData(QueryResource.StudySeries)]
        public async void GivenRequestForSeries_WhenRetrievingQueriableCustomTags_ReturnsSeriesAndStudyTags(QueryResource resourceType)
        {
            var request = new QueryResourceRequest(
                Substitute.For<IEnumerable<KeyValuePair<string, StringValues>>>(),
                resourceType,
                TestUidGenerator.Generate(),
                TestUidGenerator.Generate());

            List<CustomTagStoreEntry> storeEntries = new List<CustomTagStoreEntry>()
            {
                new CustomTagStoreEntry(1, "00741000", "CS", CustomTagLevel.Instance, CustomTagStatus.Added),
                new CustomTagStoreEntry(2, "0040A121", "DA", CustomTagLevel.Series, CustomTagStatus.Added),
                new CustomTagStoreEntry(3, "00101005", "PN", CustomTagLevel.Study, CustomTagStatus.Added),
            };

            _customTagStore.GetCustomTagsAsync().ReturnsForAnyArgs(storeEntries);
            _queryStore.QueryAsync(Arg.Any<QueryExpression>(), Arg.Any<CancellationToken>()).ReturnsForAnyArgs(new QueryResult(new List<VersionedInstanceIdentifier>()));
            await _queryService.QueryAsync(request, CancellationToken.None);

            HashSet<CustomTagFilterDetails> filterDetails = new HashSet<CustomTagFilterDetails>()
            {
                new CustomTagFilterDetails(2, CustomTagLevel.Series, "DA", DicomTag.Date),
                new CustomTagFilterDetails(3, CustomTagLevel.Study, "PN", DicomTag.PatientBirthName),
            };

            _queryParser.Received().Parse(request, Arg.Is<HashSet<CustomTagFilterDetails>>(x => x.SetEquals(filterDetails)));
        }

        [Theory]
        [InlineData(QueryResource.AllStudies)]
        public async void GivenRequestForStudies_WhenRetrievingQueriableCustomTags_ReturnsStudyTags(QueryResource resourceType)
        {
            var request = new QueryResourceRequest(
                Substitute.For<IEnumerable<KeyValuePair<string, StringValues>>>(),
                resourceType,
                TestUidGenerator.Generate(),
                TestUidGenerator.Generate());

            List<CustomTagStoreEntry> storeEntries = new List<CustomTagStoreEntry>()
            {
                new CustomTagStoreEntry(1, "00741000", "CS", CustomTagLevel.Instance, CustomTagStatus.Added),
                new CustomTagStoreEntry(2, "0040A121", "DA", CustomTagLevel.Series, CustomTagStatus.Added),
                new CustomTagStoreEntry(3, "00101005", "PN", CustomTagLevel.Study, CustomTagStatus.Added),
            };

            _customTagStore.GetCustomTagsAsync().ReturnsForAnyArgs(storeEntries);
            _queryStore.QueryAsync(Arg.Any<QueryExpression>(), Arg.Any<CancellationToken>()).ReturnsForAnyArgs(new QueryResult(new List<VersionedInstanceIdentifier>()));
            await _queryService.QueryAsync(request, CancellationToken.None);

            HashSet<CustomTagFilterDetails> filterDetails = new HashSet<CustomTagFilterDetails>()
            {
                new CustomTagFilterDetails(1, CustomTagLevel.Instance, "CS", DicomTag.ProcedureStepState),
                new CustomTagFilterDetails(2, CustomTagLevel.Series, "DA", DicomTag.Date),
                new CustomTagFilterDetails(3, CustomTagLevel.Study, "PN", DicomTag.PatientBirthName),
            };

            _queryParser.Received().Parse(request, Arg.Is<HashSet<CustomTagFilterDetails>>(x => x.SetEquals(filterDetails)));
        }
    }
}
