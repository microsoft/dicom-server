// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
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

            IEnumerable<CustomTagEntry> customTags = await _customTagStore.GetCustomTagsAsync(null, cancellationToken);

            Dictionary<QueryResource, HashSet<DicomTag>> queryResourceToCustomTagMapping = GenerateQueryResourceToCustomTagMapping(customTags);

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

        private Dictionary<QueryResource, HashSet<DicomTag>> GenerateQueryResourceToCustomTagMapping(IEnumerable<CustomTagEntry> customTags)
        {
            Dictionary<QueryResource, HashSet<DicomTag>> ret = new Dictionary<QueryResource, HashSet<DicomTag>>();

            foreach (CustomTagEntry customTag in customTags)
            {
                DicomTag[] result;
                DicomTag dicomTag;
                if (CustomTagStatus.Added.Equals(customTag.Status) && _dicomTagPathParser.TryParse(customTag.Path, out result))
                {
                    dicomTag = result[0];
                    switch (customTag.Level)
                    {
                        case CustomTagLevel.Instance:
                            ret.TryAdd(QueryResource.AllInstances, new HashSet<DicomTag>());

                            ret[QueryResource.AllInstances].Add(dicomTag);

                            ret.TryAdd(QueryResource.StudyInstances, new HashSet<DicomTag>());

                            ret[QueryResource.StudyInstances].Add(dicomTag);

                            ret.TryAdd(QueryResource.StudySeriesInstances, new HashSet<DicomTag>());

                            ret[QueryResource.StudySeriesInstances].Add(dicomTag);

                            break;
                        case CustomTagLevel.Series:
                            ret.TryAdd(QueryResource.AllSeries, new HashSet<DicomTag>());

                            ret[QueryResource.AllSeries].Add(dicomTag);

                            ret.TryAdd(QueryResource.StudySeries, new HashSet<DicomTag>());

                            ret[QueryResource.StudySeries].Add(dicomTag);

                            break;
                        case CustomTagLevel.Study:
                            ret.TryAdd(QueryResource.AllStudies, new HashSet<DicomTag>());

                            ret[QueryResource.AllStudies].Add(dicomTag);

                            break;

                        default:
                            throw new ArgumentException("invalid enum value for CustomTagLevel");
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
