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
using Microsoft.Health.Dicom.Core.Features.CustomTag;
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
        private readonly ICustomTagStore _customTagStore;
        private readonly IDicomTagParser _dicomTagPathParser;

        public QueryService(
            IQueryParser queryParser,
            IQueryStore queryStore,
            IMetadataStore metadataStore,
            ICustomTagStore customTagStore,
            IDicomTagParser dicomTagPathParser)
        {
            EnsureArg.IsNotNull(queryParser, nameof(queryParser));
            EnsureArg.IsNotNull(queryStore, nameof(queryStore));
            EnsureArg.IsNotNull(customTagStore, nameof(customTagStore));
            EnsureArg.IsNotNull(dicomTagPathParser, nameof(dicomTagPathParser));

            _queryParser = queryParser;
            _queryStore = queryStore;
            _metadataStore = metadataStore;
            _customTagStore = customTagStore;
            _dicomTagPathParser = dicomTagPathParser;
        }

        public async Task<QueryResourceResponse> QueryAsync(
            QueryResourceRequest message,
            CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(message);

            ValidateRequestIdentifiers(message);

            IReadOnlyList<CustomTagStoreEntry> customTags = await _customTagStore.GetCustomTagsAsync(null, cancellationToken);

            IDictionary<DicomTag, CustomTagFilterDetails> supportedCustomTags = RetrieveSupportedCustomTagsForQueryResourceType(customTags, message.QueryResourceType);

            QueryExpression queryExpression = _queryParser.Parse(message, supportedCustomTags);

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

        private IDictionary<DicomTag, CustomTagFilterDetails> RetrieveSupportedCustomTagsForQueryResourceType(IReadOnlyList<CustomTagStoreEntry> customTags, QueryResource queryResource)
        {
            var ret = new Dictionary<DicomTag, CustomTagFilterDetails>();

            foreach (CustomTagStoreEntry customTag in customTags)
            {
                DicomTag[] result;
                DicomTag dicomTag;
                if (customTag.Status.Equals(CustomTagStatus.Ready) && _dicomTagPathParser.TryParse(customTag.Path, out result))
                {
                    dicomTag = result[0];
                    if (queryResource.Equals(QueryResource.AllInstances) || queryResource.Equals(QueryResource.StudyInstances) || queryResource.Equals(QueryResource.StudySeriesInstances)
                        || ((queryResource.Equals(QueryResource.AllSeries) || queryResource.Equals(QueryResource.StudySeries)) && (customTag.Level.Equals(CustomTagLevel.Study) || customTag.Level.Equals(CustomTagLevel.Series)))
                        || (queryResource.Equals(QueryResource.AllStudies) && customTag.Level.Equals(CustomTagLevel.Study)))
                    {
                        // When querying for instances, custom tags of all levels can be filtered on.
                        // When querying for series, study and series custom tags can be filtered on.
                        // When querying for studies, study custom tags can be filtered on.
                        ret.Add(dicomTag, new CustomTagFilterDetails(customTag.Key, customTag.Level, DicomVR.Parse(customTag.VR), dicomTag));
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
