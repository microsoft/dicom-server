// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Core.Features.Audit
{
    /// <summary>
    /// Value set defined at http://dicom.nema.org/medical/dicom/current/output/html/part15.html#sect_A.5.1
    /// </summary>
    public static class AuditEventSubType
    {
        public const string System = "http://dicom.nema.org/medical/dicom/current/output/html/part15.html#sect_A.5.1";

        public const string ChangeFeed = "change-feed";

        public const string Delete = "delete";

        public const string Query = "query";

        public const string Retrieve = "retrieve";

        public const string RetrieveMetadata = "retrieve-metadata";

        public const string Store = "store";

        public const string Cohort = "cohort";

        public const string AddExtendedQueryTag = "add-extended-query-tag";

        public const string RemoveExtendedQueryTag = "remove-extended-query-tag";

        public const string GetAllExtendedQueryTags = "get-all-extended-query-tag";

        public const string GetExtendedQueryTag = "get-extended-query-tag";
    }
}
