// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Linq;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Core.Features.Workitem;
using Microsoft.Health.Dicom.Tests.Common;
using Xunit;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Rest
{
    public partial class WorkItemTransactionTests
    {
        [Fact]
        public async Task WhenCancelWorkitem_TheServerShouldCancelWorkitemSuccessfully()
        {
            var workitemUid = TestUidGenerator.Generate();

            // Create
            var dicomDataset = Samples.CreateRandomWorkitemInstanceDataset(workitemUid);
            using var addResponse = await _client.AddWorkitemAsync(Enumerable.Repeat(dicomDataset, 1), workitemUid);
            Assert.True(addResponse.IsSuccessStatusCode);

            // Cancel
            var cancelDicomDataset = Samples.CreateWorkitemCancelRequestDataset(@"Test Cancel");
            using var cancelResponse = await _client.CancelWorkitemAsync(cancelDicomDataset, workitemUid);
            Assert.True(cancelResponse.IsSuccessStatusCode);

            // Retrieve and Verify
            using var retrieveResponse = await _client.RetrieveWorkitemAsync(workitemUid);
            var dataset = await retrieveResponse.GetValueAsync();
            Assert.NotNull(dataset);
            Assert.Equal(ProcedureStepState.Canceled, dataset.GetProcedureState());
        }
    }
}
