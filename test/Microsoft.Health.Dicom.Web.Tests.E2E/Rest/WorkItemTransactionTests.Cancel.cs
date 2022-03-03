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

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Rest
{
    public partial class WorkItemTransactionTests
    {
        [Fact]
        public async Task WhenCancelWorkitem_TheServerShouldCancelWorkitemSuccessfully()
        {
            var workitemUid = TestUidGenerator.Generate();
            var patientName = $"TestUser-{workitemUid}";

            // Create
            var dicomDataset = Samples.CreateRandomWorkitemInstanceDataset(workitemUid);
            dicomDataset.AddOrUpdate(DicomTag.PatientName, patientName);

            using var addResponse = await _client.AddWorkitemAsync(Enumerable.Repeat(dicomDataset, 1), workitemUid);
            Assert.True(addResponse.IsSuccessStatusCode);

            // Cancel
            var cancelDicomDataset = Samples.CreateWorkitemCancelRequestDataset(@"Test Cancel");
            using var cancelResponse = await _client.CancelWorkitemAsync(cancelDicomDataset, workitemUid);
            Assert.True(cancelResponse.IsSuccessStatusCode);

            using var queryResponse = await _client.QueryWorkitemAsync($"PatientName={patientName}");
            var responseDatasets = await queryResponse.ToArrayAsync();
            var actualDataset = responseDatasets?.FirstOrDefault();

            Assert.NotNull(actualDataset);
            Assert.Equal(workitemUid, actualDataset.GetSingleValue<string>(DicomTag.SOPInstanceUID));
            Assert.Equal(ProcedureStepState.Canceled, ProcedureStepStateExtensions.GetProcedureState(actualDataset));
        }
    }
}
