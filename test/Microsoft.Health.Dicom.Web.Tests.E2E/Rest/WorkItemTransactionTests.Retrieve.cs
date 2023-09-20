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
    [Trait("Category", "bvt")]
    public async Task GivenRetrieveWorkitem_WhenWorkitemIsFound_TheServerShouldRetrieveWorkitemSuccessfully()
    {
        var workitemUid = TestUidGenerator.Generate();
        var patientName = $"TestUser-{workitemUid}";

        // Create
        var dicomDataset = Samples.CreateRandomWorkitemInstanceDataset(workitemUid);
        dicomDataset.AddOrUpdate(DicomTag.PatientName, patientName);

        using var addResponse = await _client.AddWorkitemAsync(Enumerable.Repeat(dicomDataset, 1), workitemUid);
        Assert.True(addResponse.IsSuccessStatusCode);

        // Retrieve
        using var retrieveResponse = await _client.RetrieveWorkitemAsync(workitemUid);
        Assert.True(retrieveResponse.IsSuccessStatusCode);

        var dataset = await retrieveResponse.GetValueAsync();

        // Verify
        Assert.NotNull(dataset);
        Assert.False(dataset.TryGetString(DicomTag.TransactionUID, out var transactionUid));
        Assert.Null(transactionUid);
        Assert.Equal(workitemUid, dataset.GetSingleValue<string>(DicomTag.SOPInstanceUID));
    }
}
