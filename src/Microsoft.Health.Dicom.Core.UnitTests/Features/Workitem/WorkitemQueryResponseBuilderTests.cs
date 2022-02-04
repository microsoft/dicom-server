// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using FellowOakDicom;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Query;
using Microsoft.Health.Dicom.Core.Features.Query.Model;
using Microsoft.Health.Dicom.Core.Features.Workitem;
using Microsoft.Health.Dicom.Tests.Common;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Query
{
    public class WorkitemQueryResponseBuilderTests
    {
        [Fact]
        public void GivenWorkitemInstanceLevel_WithIncludeField_ValidReturned()
        {
            var includeField = new QueryIncludeField(new List<DicomTag> { DicomTag.PatientID });
            var filters = new List<QueryFilterCondition>()
            {
                new StringSingleValueMatchCondition(new QueryTag(DicomTag.PatientName), "Foo"),
            };
            var query = new BaseQueryExpression(includeField, false, 0, 0, filters);

            DicomDataset responseDataset = WorkitemQueryResponseBuilder.GenerateResponseDataset(Samples.CreateRandomWorkitemInstanceDataset(), query);
            var tags = responseDataset.Select(i => i.Tag).ToList();

            Assert.Contains<DicomTag>(DicomTag.PatientID, tags); // Valid inlcude
            Assert.Contains<DicomTag>(DicomTag.PatientName, tags); // Valid filter
            Assert.DoesNotContain<DicomTag>(DicomTag.TransactionUID, tags);
        }
    }
}
