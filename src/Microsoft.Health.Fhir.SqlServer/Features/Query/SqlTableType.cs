// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------
namespace Microsoft.Health.Fhir.SqlServer.Features.Query
{
    public enum SqlTableType : int
    {
        StudyTable,
        SeriesTable,
        InstanceTable,
    }
}
