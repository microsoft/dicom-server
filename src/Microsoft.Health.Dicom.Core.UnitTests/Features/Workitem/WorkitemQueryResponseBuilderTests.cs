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
using Microsoft.Health.Dicom.Core.Messages.Workitem;
using Microsoft.Health.Dicom.Tests.Common;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Query;

public class WorkitemQueryResponseBuilderTests
{
    [Fact]
    public void GivenWorkitem_WithIncludeField_ValidReturned()
    {
        var includeField = new QueryIncludeField(new List<DicomTag> { DicomTag.WorklistLabel });
        var filters = new List<QueryFilterCondition>()
        {
            new StringSingleValueMatchCondition(new QueryTag(DicomTag.PatientName), "Foo"),
        };
        var query = new BaseQueryExpression(includeField, false, 0, 0, filters);
        var dataset = Samples
            .CreateRandomWorkitemInstanceDataset()
            .AddOrUpdate(new DicomLongString(DicomTag.MedicalAlerts))
            .AddOrUpdate(new DicomShortString(DicomTag.SnoutID));

        var datasets = new List<DicomDataset>
        {
            dataset
        };

        var response = WorkitemQueryResponseBuilder.BuildWorkitemQueryResponse(datasets, query);
        DicomDataset responseDataset = response.ResponseDatasets.FirstOrDefault();

        Assert.NotNull(responseDataset);
        var tags = responseDataset.Select(i => i.Tag).ToList();

        Assert.Contains(DicomTag.WorklistLabel, tags); // Valid include
        Assert.Contains(DicomTag.PatientName, tags); // Valid filter
        Assert.Contains(DicomTag.MedicalAlerts, tags); // Required return attribute

        Assert.DoesNotContain(DicomTag.TransactionUID, tags); // should never be included
        Assert.DoesNotContain(DicomTag.SnoutID, tags); // Not a required return attribute
    }

    [Fact]
    public void GivenWorkitem_WithIncludeFieldAll_AllReturned()
    {
        var includeField = QueryIncludeField.AllFields;
        var filters = new List<QueryFilterCondition>();
        var query = new BaseQueryExpression(includeField, false, 0, 0, filters);
        var dataset = Samples
            .CreateRandomWorkitemInstanceDataset()
            .AddOrUpdate(new DicomLongString(DicomTag.MedicalAlerts))
            .AddOrUpdate(new DicomShortString(DicomTag.SnoutID));

        var datasets = new List<DicomDataset>
        {
            dataset
        };

        var response = WorkitemQueryResponseBuilder.BuildWorkitemQueryResponse(datasets, query);
        DicomDataset responseDataset = response.ResponseDatasets.FirstOrDefault();

        Assert.NotNull(responseDataset);
        var tags = responseDataset.Select(i => i.Tag).ToList();

        Assert.Contains(DicomTag.MedicalAlerts, tags); // Required return attribute
        Assert.Contains(DicomTag.SnoutID, tags); // Not a required return attribute - set by 'all'

        Assert.DoesNotContain(DicomTag.TransactionUID, tags); // should never be included
    }

    [Fact]
    public void GivenWorkitem_WithIncludeFieldAndPartialMatchingResult_ValidPartialContentReturned()
    {
        var includeField = new QueryIncludeField(new List<DicomTag> { DicomTag.WorklistLabel });
        var filters = new List<QueryFilterCondition>()
        {
            new StringSingleValueMatchCondition(new QueryTag(DicomTag.PatientName), "Foo"),
        };
        var query = new BaseQueryExpression(includeField, false, 0, 0, filters);
        var dataset = Samples
            .CreateRandomWorkitemInstanceDataset()
            .AddOrUpdate(new DicomLongString(DicomTag.MedicalAlerts))
            .AddOrUpdate(new DicomShortString(DicomTag.SnoutID));

        var datasets = new List<DicomDataset>
        {
            dataset,
            null
        };

        var response = WorkitemQueryResponseBuilder.BuildWorkitemQueryResponse(datasets, query);
        Assert.Single(response.ResponseDatasets);
        Assert.Equal(WorkitemResponseStatus.PartialContent, response.Status);
    }
}
