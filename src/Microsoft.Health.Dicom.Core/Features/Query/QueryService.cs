// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dicom;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.Common;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Query.Model;
using Microsoft.Health.Dicom.Core.Features.Validation;
using Microsoft.Health.Dicom.Core.Messages.Query;

namespace Microsoft.Health.Dicom.Core.Features.Query
{
    public class QueryService : IQueryService
    {
        private readonly IQueryParser _queryParser;
        private readonly IQueryStore _queryStore;
        private readonly IMetadataStore _metadataStore;
        private readonly IExtendedQueryTagStore _extendedQueryTagStore;
        private readonly IDicomTagParser _dicomTagPathParser;

        public QueryService(
            IQueryParser queryParser,
            IQueryStore queryStore,
            IMetadataStore metadataStore,
            IExtendedQueryTagStore extendedQueryTagStore,
            IDicomTagParser dicomTagPathParser)
        {
            EnsureArg.IsNotNull(queryParser, nameof(queryParser));
            EnsureArg.IsNotNull(queryStore, nameof(queryStore));
            EnsureArg.IsNotNull(extendedQueryTagStore, nameof(extendedQueryTagStore));
            EnsureArg.IsNotNull(dicomTagPathParser, nameof(dicomTagPathParser));

            _queryParser = queryParser;
            _queryStore = queryStore;
            _metadataStore = metadataStore;
            _extendedQueryTagStore = extendedQueryTagStore;
            _dicomTagPathParser = dicomTagPathParser;
        }

        public async Task<QueryResourceResponse> QueryAsync(
            QueryResourceRequest message,
            CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(message);

            ValidateRequestIdentifiers(message);

            IReadOnlyList<ExtendedQueryTagStoreEntry> extendedQueryTags = await _extendedQueryTagStore.GetExtendedQueryTagsAsync(null, cancellationToken);

            IDictionary<DicomTag, ExtendedQueryTagFilterDetails> supportedExtendedQueryTags = RetrieveSupportedExtendedQueryTagsForQueryResourceType(extendedQueryTags, message.QueryResourceType);

            QueryExpression queryExpression = _queryParser.Parse(message, supportedExtendedQueryTags);

            QueryResult queryResult = await _queryStore.QueryAsync(queryExpression, cancellationToken);

            if (!queryResult.DicomInstances.Any())
            {
                return new QueryResourceResponse();
            }

            IEnumerable<DicomDataset> instanceMetadata = await Task.WhenAll(
                        queryResult.DicomInstances
                        .Select(x => _metadataStore.GetInstanceMetadataAsync(x, cancellationToken)));

            var responseBuilder = new QueryResponseBuilder(queryExpression);
            IEnumerable<DicomDataset> responseMetadata = instanceMetadata.Select(m => responseBuilder.GenerateResponseDataset(m));

            return new QueryResourceResponse(responseMetadata);
        }

        private IDictionary<DicomTag, ExtendedQueryTagFilterDetails> RetrieveSupportedExtendedQueryTagsForQueryResourceType(IReadOnlyList<ExtendedQueryTagStoreEntry> extendedQueryTags, QueryResource queryResource)
        {
            var ret = new Dictionary<DicomTag, ExtendedQueryTagFilterDetails>();

            foreach (ExtendedQueryTagStoreEntry extendedQueryTag in extendedQueryTags)
            {
                DicomTag[] result;
                DicomTag dicomTag;
                if (extendedQueryTag.Status.Equals(ExtendedQueryTagStatus.Ready) && _dicomTagPathParser.TryParse(extendedQueryTag.Path, out result))
                {
                    dicomTag = result[0];
                    if (queryResource.Equals(QueryResource.AllInstances) || queryResource.Equals(QueryResource.StudyInstances) || queryResource.Equals(QueryResource.StudySeriesInstances)
                        || ((queryResource.Equals(QueryResource.AllSeries) || queryResource.Equals(QueryResource.StudySeries)) && (extendedQueryTag.Level.Equals(ExtendedQueryTagLevel.Study) || extendedQueryTag.Level.Equals(ExtendedQueryTagLevel.Series)))
                        || (queryResource.Equals(QueryResource.AllStudies) && extendedQueryTag.Level.Equals(ExtendedQueryTagLevel.Study)))
                    {
                        // When querying for instances, extended query tags of all levels can be filtered on.
                        // When querying for series, study and series extended query tags can be filtered on.
                        // When querying for studies, study extended query tags can be filtered on.
                        ret.Add(dicomTag, new ExtendedQueryTagFilterDetails(extendedQueryTag.Key, extendedQueryTag.Level, DicomVR.Parse(extendedQueryTag.VR), dicomTag));
                    }
                }
            }

            return ret;
        }

        private static void ValidateRequestIdentifiers(QueryResourceRequest message)
        {
            switch (message.QueryResourceType)
            {
                case QueryResource.StudySeries:
                case QueryResource.StudyInstances:
                    UidValidator.Validate(message.StudyInstanceUid, nameof(message.StudyInstanceUid));
                    break;
                case QueryResource.StudySeriesInstances:
                    UidValidator.Validate(message.StudyInstanceUid, nameof(message.StudyInstanceUid));
                    UidValidator.Validate(message.SeriesInstanceUid, nameof(message.SeriesInstanceUid));
                    break;
                case QueryResource.AllStudies:
                case QueryResource.AllSeries:
                case QueryResource.AllInstances:
                    break;
                default:
                    Debug.Fail("A newly added query resource is not handled.");
                    break;
            }
        }
    }
}
