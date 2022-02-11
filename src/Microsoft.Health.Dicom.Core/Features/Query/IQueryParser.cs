// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Query.Model;

namespace Microsoft.Health.Dicom.Core.Features.Query
{
    public interface IQueryParser<TQueryExpression, TQueryParameters>
         where TQueryExpression : BaseQueryExpression
         where TQueryParameters : BaseQueryParameters
    {
        TQueryExpression Parse(TQueryParameters parameters, IReadOnlyCollection<QueryTag> queryTags);
    }
}
