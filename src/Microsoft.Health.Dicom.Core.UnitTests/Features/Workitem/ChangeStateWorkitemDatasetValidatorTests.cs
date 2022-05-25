// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using FellowOakDicom;
using Microsoft.Health.Dicom.Core.Features.Store;
using Microsoft.Health.Dicom.Core.Features.Workitem;
using Microsoft.Health.Dicom.Core.Features.Workitem.Model;
using Microsoft.Health.Dicom.Tests.Common;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Workitem;

public class ChangeStateWorkitemDatasetValidatorTests
{
    private readonly string _transactionUid;

    private readonly DicomDataset _requestDataset;

    private readonly WorkitemMetadataStoreEntry _currentWorkitem;

    public ChangeStateWorkitemDatasetValidatorTests()
    {

        _transactionUid = TestUidGenerator.Generate();

        _requestDataset = new DicomDataset
        {
            { DicomTag.TransactionUID, _transactionUid },
            { DicomTag.ProcedureStepState, ProcedureStepStateConstants.InProgress },
        };

        // unclaimed workitem
        _currentWorkitem = new WorkitemMetadataStoreEntry(TestUidGenerator.Generate(), 1, 1);
        _currentWorkitem.ProcedureStepState = ProcedureStepState.Scheduled;
    }

    [Fact]
    public void GivenUnclaimedWorkitem_WhenClaiming_Succeeds()
    {
        var result = ChangeWorkitemStateDatasetValidator.ValidateWorkitemState(_requestDataset, _currentWorkitem);
        Assert.Equal(ProcedureStepState.InProgress, result.State);
    }

    [Fact]
    public void GivenClaimedWorkitem_WhenCorrectTransactionUid_Succeeds()
    {
        _requestDataset.AddOrUpdate(DicomTag.ProcedureStepState, ProcedureStepStateConstants.Completed);
        _currentWorkitem.TransactionUid = _transactionUid;
        _currentWorkitem.ProcedureStepState = ProcedureStepState.InProgress;

        var result = ChangeWorkitemStateDatasetValidator.ValidateWorkitemState(_requestDataset, _currentWorkitem);

        Assert.Equal(ProcedureStepState.Completed, result.State);
    }

    [Fact]
    public void GivenClaimedWorkitem_WhenIncorrectTransactionUid_Throws()
    {
        _requestDataset.AddOrUpdate(DicomTag.ProcedureStepState, ProcedureStepStateConstants.Completed);
        _currentWorkitem.TransactionUid = TestUidGenerator.Generate();
        _currentWorkitem.ProcedureStepState = ProcedureStepState.InProgress;

        Assert.Throws<DatasetValidationException>(() => ChangeWorkitemStateDatasetValidator.ValidateWorkitemState(_requestDataset, _currentWorkitem));
    }
}
