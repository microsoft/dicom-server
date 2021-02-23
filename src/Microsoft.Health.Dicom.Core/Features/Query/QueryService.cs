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

            IEnumerable<CustomTagStoreEntry> customTags = await _customTagStore.GetCustomTagsAsync(null, cancellationToken);

            Dictionary<QueryResource, HashSet<CustomTagFilterDetails>> queryResourceToCustomTagMapping = GenerateQueryResourceToCustomTagMapping(customTags);

            QueryExpression queryExpression = _queryParser.Parse(message, queryResourceToCustomTagMapping);

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

        private Dictionary<QueryResource, HashSet<CustomTagFilterDetails>> GenerateQueryResourceToCustomTagMapping(IEnumerable<CustomTagStoreEntry> customTags)
        {
            Dictionary<QueryResource, HashSet<CustomTagFilterDetails>> ret = new Dictionary<QueryResource, HashSet<CustomTagFilterDetails>>();

            foreach (CustomTagStoreEntry customTag in customTags)
            {
                DicomTag[] result;
                DicomTag dicomTag;
                if (customTag.Status.Equals(CustomTagStatus.Added) && _dicomTagPathParser.TryParse(customTag.Path, out result))
                {
                    dicomTag = result[0];

                    if (customTag.Level.Equals(CustomTagLevel.Instance))
                    {
                        ret.TryAdd(QueryResource.AllInstances, new HashSet<CustomTagFilterDetails>());

                        ret[QueryResource.AllInstances].Add(new CustomTagFilterDetails(customTag.Key, customTag.Level, dicomTag));

                        ret.TryAdd(QueryResource.StudyInstances, new HashSet<CustomTagFilterDetails>());

                        ret[QueryResource.StudyInstances].Add(new CustomTagFilterDetails(customTag.Key, customTag.Level, dicomTag));

                        ret.TryAdd(QueryResource.StudySeriesInstances, new HashSet<CustomTagFilterDetails>());

                        ret[QueryResource.StudySeriesInstances].Add(new CustomTagFilterDetails(customTag.Key, customTag.Level, dicomTag));
                    }

                    if (customTag.Level.Equals(CustomTagLevel.Instance) || customTag.Level.Equals(CustomTagLevel.Series))
                    {
                        ret.TryAdd(QueryResource.AllSeries, new HashSet<CustomTagFilterDetails>());

                        ret[QueryResource.AllSeries].Add(new CustomTagFilterDetails(customTag.Key, customTag.Level, dicomTag));

                        ret.TryAdd(QueryResource.StudySeries, new HashSet<CustomTagFilterDetails>());

                        ret[QueryResource.StudySeries].Add(new CustomTagFilterDetails(customTag.Key, customTag.Level, dicomTag));
                    }

                    ret.TryAdd(QueryResource.AllStudies, new HashSet<CustomTagFilterDetails>());

                    ret[QueryResource.AllStudies].Add(new CustomTagFilterDetails(customTag.Key, customTag.Level, dicomTag));
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
