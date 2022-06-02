// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Linq;
using System.Threading.Tasks;
using FellowOakDicom;
using Microsoft.Health.Dicom.Tests.Common;
using Xunit;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Rest;

public partial class WorkItemTransactionTests
{
    [Fact]
    [Trait("Category", "bvt-fe")]
    public async Task GivenUpdateWorkitemTransaction_WhenWorkitemIsFound_TheServerShouldUpdateWorkitemSuccessfully()
    {
        // Create
        var dicomDataset = Samples.CreateRandomWorkitemInstanceDataset();
        var workitemUid = dicomDataset.GetSingleValue<string>(DicomTag.SOPInstanceUID);
        var patientName = $"TestUser-{workitemUid}";

        using var addResponse = await _client.AddWorkitemAsync(Enumerable.Repeat(dicomDataset, 1), workitemUid);
        Assert.True(addResponse.IsSuccessStatusCode);

        string newWorklistLabel = "WORKLIST-TEST";

        // Update Workitem Transaction
        var updateDicomDataset = new DicomDataset
        {
            {DicomTag.WorklistLabel, newWorklistLabel },
        };

        using var updateWorkitemResponse = await _client.UpdateWorkitemAsync(updateDicomDataset, workitemUid)
            .ConfigureAwait(false);
        Assert.True(updateWorkitemResponse.IsSuccessStatusCode);

        using var retrieveResponse = await _client.RetrieveWorkitemAsync(workitemUid)
            .ConfigureAwait(false);
        Assert.True(retrieveResponse.IsSuccessStatusCode);
        var dataset = await retrieveResponse.GetValueAsync().ConfigureAwait(false);

        Assert.NotNull(dataset);
        Assert.Equal(newWorklistLabel, dataset.GetString(DicomTag.WorklistLabel));
    }
}
