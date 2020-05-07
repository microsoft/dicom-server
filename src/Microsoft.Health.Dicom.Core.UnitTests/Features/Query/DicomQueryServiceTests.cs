// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.Primitives;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.Query;
using Microsoft.Health.Dicom.Core.Messages.Query;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Query
{
    public class DicomQueryServiceTests
    {
        private readonly QueryService _queryService = null;

        public DicomQueryServiceTests()
        {
            _queryService = new QueryService(
                Substitute.For<IQueryParser>(),
                Substitute.For<IQueryStore>(),
                Substitute.For<IMetadataStore>());
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
    }
}
