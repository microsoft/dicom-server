// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Linq;
using System.Threading.Tasks;
using FellowOakDicom;
using Microsoft.Health.Dicom.Core.Features.Workitem;
using Microsoft.Health.Dicom.Tests.Common;
using Xunit;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Rest;

public partial class WorkItemTransactionTests
{
    [Fact]
    [Trait("Category", "bvt")]
    public async Task GivenUpdateWorkitemTransaction_WhenWorkitemIsFound_TheServerShouldUpdateWorkitemSuccessfully()
    {
        // Create
        var dicomDataset = Samples.CreateRandomWorkitemInstanceDataset();
        var workitemUid = dicomDataset.GetSingleValue<string>(DicomTag.SOPInstanceUID);

        using var addResponse = await _client.AddWorkitemAsync(Enumerable.Repeat(dicomDataset, 1), workitemUid);
        Assert.True(addResponse.IsSuccessStatusCode);

        string newWorklistLabel = "WORKLIST-TEST";

        // Update Workitem Transaction
        var updateDicomDataset = new DicomDataset
        {
            { DicomTag.WorklistLabel, newWorklistLabel },
            new DicomSequence(DicomTag.UnifiedProcedureStepPerformedProcedureSequence, new DicomDataset
            {
                new DicomSequence(DicomTag.OutputInformationSequence, new DicomDataset
                {
                    { DicomTag.TypeOfInstances, "SAMPLETYPEOFINST" },
                    new DicomSequence(DicomTag.ReferencedSOPSequence, new DicomDataset
                    {
                        { DicomTag.ReferencedSOPClassUID, "1.2.3" },
                        { DicomTag.ReferencedSOPInstanceUID, "1.2.3" }
                    })
                })
            }),
        };

        using var updateWorkitemResponse = await _client.UpdateWorkitemAsync(Enumerable.Repeat(updateDicomDataset, 1), workitemUid);
        Assert.True(updateWorkitemResponse.IsSuccessStatusCode);

        using var retrieveResponse = await _client.RetrieveWorkitemAsync(workitemUid);
        Assert.True(retrieveResponse.IsSuccessStatusCode);
        var dataset = await retrieveResponse.GetValueAsync();

        Assert.NotNull(dataset);
        Assert.Equal(newWorklistLabel, dataset.GetString(DicomTag.WorklistLabel));
    }

    [Fact]
    [Trait("Category", "bvt")]
    public async Task GivenUpdateWorkitemTransactionWithTransactionUid_WhenWorkitemIsFound_TheServerShouldUpdateWorkitemSuccessfully()
    {
        // Create
        var dicomDataset = Samples.CreateRandomWorkitemInstanceDataset();
        var workitemUid = dicomDataset.GetSingleValue<string>(DicomTag.SOPInstanceUID);

        using var addResponse = await _client.AddWorkitemAsync(Enumerable.Repeat(dicomDataset, 1), workitemUid);
        Assert.True(addResponse.IsSuccessStatusCode);

        var transactionUid = TestUidGenerator.Generate();
        var changeStateRequestDicomDataset = new DicomDataset
        {
            { DicomTag.TransactionUID, transactionUid },
            { DicomTag.ProcedureStepState, ProcedureStepStateConstants.InProgress }
        };
        using var changeStateResponse = await _client
            .ChangeWorkitemStateAsync(Enumerable.Repeat(changeStateRequestDicomDataset, 1), workitemUid);
        Assert.True(changeStateResponse.IsSuccessStatusCode);

        string newWorklistLabel = "WORKLIST-TEST";

        // Update Workitem Transaction
        var updateDicomDataset = new DicomDataset
        {
            { DicomTag.WorklistLabel, newWorklistLabel },
            new DicomSequence(DicomTag.UnifiedProcedureStepPerformedProcedureSequence, new DicomDataset
            {
                new DicomSequence(DicomTag.OutputInformationSequence, new DicomDataset
                {
                    { DicomTag.TypeOfInstances, "SAMPLETYPEOFINST" },
                    new DicomSequence(DicomTag.ReferencedSOPSequence, new DicomDataset
                    {
                        { DicomTag.ReferencedSOPClassUID, "1.2.3" },
                        { DicomTag.ReferencedSOPInstanceUID, "1.2.3" }
                    })
                })
            }),
        };

        using var updateWorkitemResponse = await _client.UpdateWorkitemAsync(Enumerable.Repeat(updateDicomDataset, 1), workitemUid, transactionUid);
        Assert.True(updateWorkitemResponse.IsSuccessStatusCode);

        using var retrieveResponse = await _client.RetrieveWorkitemAsync(workitemUid);
        Assert.True(retrieveResponse.IsSuccessStatusCode);
        var dataset = await retrieveResponse.GetValueAsync();

        Assert.NotNull(dataset);
        Assert.Equal(newWorklistLabel, dataset.GetString(DicomTag.WorklistLabel));
    }
}
