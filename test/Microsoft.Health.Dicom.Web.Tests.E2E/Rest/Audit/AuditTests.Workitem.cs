// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FellowOakDicom;
using Microsoft.Health.Dicom.Core.Features.Audit;
using Microsoft.Health.Dicom.Core.Features.Workitem;
using Microsoft.Health.Dicom.Tests.Common;
using Xunit;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Rest.Audit;

public partial class AuditTests
{
    [Fact]
    public async Task GivenAddWorkitemRequest_WhenAddingUsingWorkitemInstanceUid_ThenAuditLogEntriesShouldBeCreated()
    {
        var workitemUid = TestUidGenerator.Generate();
        var workitemDataset = Samples.CreateRandomWorkitemInstanceDataset(workitemUid);

        await ExecuteAndValidate(
            () => _instancesManager.AddWorkitemAsync(
                Enumerable.Repeat(workitemDataset, 1),
                workitemUid,
                null,
                CancellationToken.None),
            AuditEventSubType.AddWorkitem,
            $"workitems?{workitemUid}",
            HttpStatusCode.Created);
    }

    [Fact]
    public async Task GivenQueryWorkitemRequest_WhenWorkitemIsFound_ThenAuditLogEntriesShouldBeCreated()
    {
        var workitemUid = TestUidGenerator.Generate();
        var dicomDataset = Samples.CreateRandomWorkitemInstanceDataset(workitemUid);
        dicomDataset.AddOrUpdate(DicomTag.PatientName, "Foo");

        using var response = await _client.AddWorkitemAsync(Enumerable.Repeat(dicomDataset, 1), workitemUid);
        Assert.True(response.IsSuccessStatusCode);

        var queryString = @"PatientName=Foo";

        await ExecuteAndValidate(
            () => _instancesManager.QueryWorkitemAsync(queryString, default, CancellationToken.None),
            AuditEventSubType.QueryWorkitem,
            $"workitems?{queryString}",
            HttpStatusCode.OK);
    }

    [Fact]
    public async Task GivenCancelWorkitemRequest_WhenWorkitemIsCanceled_ThenAuditLogEntriesShouldBeCreated()
    {
        var workitemUid = TestUidGenerator.Generate();
        var dicomDataset = Samples.CreateRandomWorkitemInstanceDataset(workitemUid);

        using var response = await _client.AddWorkitemAsync(Enumerable.Repeat(dicomDataset, 1), workitemUid);
        Assert.True(response.IsSuccessStatusCode);

        await ExecuteAndValidate(
            () => _instancesManager.CancelWorkitemAsync(Enumerable.Repeat(dicomDataset, 1), workitemUid, default, CancellationToken.None),
            AuditEventSubType.CancelWorkitem,
            $"workitems/{workitemUid}/cancelrequest",
            HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task GivenRetrieveWorkitemRequest_WhenWorkitemIsFound_ThenAuditLogEntriesShouldBeCreated()
    {
        var workitemUid = TestUidGenerator.Generate();
        var dicomDataset = Samples.CreateRandomWorkitemInstanceDataset(workitemUid);

        using var response = await _client.AddWorkitemAsync(Enumerable.Repeat(dicomDataset, 1), workitemUid);
        Assert.True(response.IsSuccessStatusCode);

        await ExecuteAndValidate(
            () => _instancesManager.RetrieveWorkitemAsync(workitemUid, default, CancellationToken.None),
            AuditEventSubType.RetrieveWorkitem,
            $"workitems/{workitemUid}",
            HttpStatusCode.OK);
    }

    [Fact]
    public async Task GivenChangeWorkitemStateRequest_WhenWorkitemStateIsChanged_ThenAuditLogEntriesShouldBeCreated()
    {
        var workitemUid = TestUidGenerator.Generate();
        var dicomDataset = Samples.CreateRandomWorkitemInstanceDataset(workitemUid);

        using var response = await _client.AddWorkitemAsync(Enumerable.Repeat(dicomDataset, 1), workitemUid);
        Assert.True(response.IsSuccessStatusCode);

        var changeStateRequestDicomDataset = new DicomDataset
        {
            { DicomTag.TransactionUID, TestUidGenerator.Generate() },
            { DicomTag.ProcedureStepState, ProcedureStepStateConstants.InProgress }
        };

        await ExecuteAndValidate(
            () => _instancesManager.ChangeWorkitemStateAsync(changeStateRequestDicomDataset, workitemUid, default, CancellationToken.None),
            AuditEventSubType.ChangeStateWorkitem,
            $"workitems/{workitemUid}/state",
            HttpStatusCode.OK);
    }

    [Fact]
    public async Task GivenUpdateWorkitemTransactionRequest_WhenWorkitemIsUpdated_ThenAuditLogEntriesShouldBeCreated()
    {
        var workitemUid = TestUidGenerator.Generate();
        var dicomDataset = Samples.CreateRandomWorkitemInstanceDataset(workitemUid);

        using var response = await _client.AddWorkitemAsync(Enumerable.Repeat(dicomDataset, 1), workitemUid);
        Assert.True(response.IsSuccessStatusCode);

        var updateWorkitemRequestDicomDataset = new DicomDataset
        {
            { DicomTag.WorklistLabel, "WORKITEM-TEST" },
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

        await ExecuteAndValidate(
            () => _instancesManager.UpdateWorkitemAsync(updateWorkitemRequestDicomDataset, workitemUid),
            AuditEventSubType.UpdateWorkitem,
            $"workitems/{workitemUid}",
            HttpStatusCode.OK);
    }
}
