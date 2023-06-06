// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Messages;

namespace Microsoft.Health.Dicom.Core.Features.Query.Model;

/// <summary>
/// Valid parsed object representing query parameters for a QIDO-RS request
/// </summary>
public class QueryExpression : BaseQueryExpression
{
    public QueryExpression(
        QueryResource resourceType,
        QueryIncludeField includeFields,
        bool fuzzyMatching,
        int limit,
        long offset,
        IReadOnlyCollection<QueryFilterCondition> filterConditions,
        IReadOnlyCollection<string> erroneousTags)
        : base(includeFields, fuzzyMatching, limit, offset, filterConditions)
    {
        QueryResource = resourceType;
        ErroneousTags = EnsureArg.IsNotNull(erroneousTags, nameof(erroneousTags));
        SetIELevel();
    }

    /// <summary>
    /// Query Resource type level
    /// </summary>
    public QueryResource QueryResource { get; }

    /// <summary>
    /// Resource level Study/Series
    /// </summary>
    public ResourceType IELevel { get; private set; }

    /// <summary>
    /// List of erroneous tags.
    /// </summary>
    public IReadOnlyCollection<string> ErroneousTags { get; }

    public bool IsInstanceIELevel()
    {
        return IELevel == ResourceType.Instance;
    }

    public bool IsSeriesIELevel()
    {
        return IELevel == ResourceType.Series;
    }

    public bool IsStudyIELevel()
    {
        return IELevel == ResourceType.Study;
    }

    private void SetIELevel()
    {
        switch (QueryResource)
        {
            case QueryResource.AllInstances:
            case QueryResource.StudyInstances:
            case QueryResource.StudySeriesInstances:
                IELevel = ResourceType.Instance;
                break;
            case QueryResource.AllSeries:
            case QueryResource.StudySeries:
                IELevel = ResourceType.Series;
                break;
            case QueryResource.AllStudies:
                IELevel = ResourceType.Study;
                break;
        }
    }
}
