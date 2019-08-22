// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.CosmosDb.Features.Storage.Documents
{
    internal static class DocumentProperties
    {
        public const string StudyInstanceUID = "study";
        public const string SeriesInstanceUID = "series";
        public const string SopInstanceUID = "uid";
        public const string Instances = "instances";
        public const string Attributes = "tags";
        public const string DistinctAttributes = "distinct";
        public const string Values = "v";
        public const string MinimumDateTimeValue = "min";
        public const string MaximumDateTimeValue = "max";
    }
}
