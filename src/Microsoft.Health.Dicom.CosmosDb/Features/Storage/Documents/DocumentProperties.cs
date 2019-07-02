// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.CosmosDb.Features.Storage.Documents
{
    internal static class DocumentProperties
    {
        public const string StudyInstanceUID = "studyUID";
        public const string SeriesInstanceUID = "seriesUID";
        public const string SopInstanceUID = "instanceUID";
        public const string Instances = "instances";
        public const string Attributes = "attributes";
        public const string DistinctAttributes = "distinctAttributes";
        public const string Values = "values";
        public const string MinimumDateTimeValue = "minDateTime";
        public const string MaximumDateTimeValue = "maxDateTime";
    }
}
