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
    public async Task GivenChangeWorkitemState_WhenWorkitemIsFound_TheServerShouldChangeWorkitemStateSuccessfully()
    {
        // Create
        var dicomDataset = Samples.CreateRandomWorkitemInstanceDataset();
        var workitemUid = dicomDataset.GetSingleValue<string>(DicomTag.SOPInstanceUID);

        using var addResponse = await _client.AddWorkitemAsync(Enumerable.Repeat(dicomDataset, 1), workitemUid);
        Assert.True(addResponse.IsSuccessStatusCode);

        // Change Workitem State
        var changeStateDicomDataset = new DicomDataset
        {
            {DicomTag.TransactionUID, TestUidGenerator.Generate()},
            {DicomTag.ProcedureStepState, ProcedureStepStateConstants.InProgress },
        };

        using var changeStateResponse = await _client.ChangeWorkitemStateAsync(Enumerable.Repeat(changeStateDicomDataset, 1), workitemUid);
        Assert.True(changeStateResponse.IsSuccessStatusCode);

        using var retrieveResponse = await _client.RetrieveWorkitemAsync(workitemUid);
        Assert.True(retrieveResponse.IsSuccessStatusCode);
        var dataset = await retrieveResponse.GetValueAsync();

        Assert.NotNull(dataset);
        Assert.Equal(ProcedureStepState.InProgress, dataset.GetProcedureStepState());
    }
}
