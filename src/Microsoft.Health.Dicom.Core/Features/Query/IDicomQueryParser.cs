// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------
using Microsoft.AspNetCore.Http;
using Microsoft.Health.Dicom.Core.Messages;

namespace Microsoft.Health.Dicom.Core.Features.Query
{
    public interface IDicomQueryParser
    {
        DicomQueryExpression Parse(IQueryCollection requestQuery, ResourceType resourceType);
    }
}
