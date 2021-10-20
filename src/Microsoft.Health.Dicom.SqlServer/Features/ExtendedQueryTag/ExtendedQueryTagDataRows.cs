// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Microsoft.Health.Dicom.SqlServer.Features.Schema.Model;

namespace Microsoft.Health.Dicom.SqlServer.Features.ExtendedQueryTag
{
    /// <summary>
    /// Represent rows for extended query tag index data.
    /// </summary>
    internal class ExtendedQueryTagDataRows
    {
        public IEnumerable<InsertStringExtendedQueryTagTableTypeV1Row> StringRows { get; set; }

        public IEnumerable<InsertLongExtendedQueryTagTableTypeV1Row> LongRows { get; set; }

        public IEnumerable<InsertDoubleExtendedQueryTagTableTypeV1Row> DoubleRows { get; set; }

        public IEnumerable<InsertDateTimeExtendedQueryTagTableTypeV1Row> DateTimeRows { get; set; }

        public IEnumerable<InsertDateTimeExtendedQueryTagTableTypeV2Row> DateTimeWithUtcRows { get; set; }

        public IEnumerable<InsertPersonNameExtendedQueryTagTableTypeV1Row> PersonNameRows { get; set; }
    }
}
