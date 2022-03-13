// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using FellowOakDicom;
using Microsoft.Health.Dicom.Core.Features.Store;
using Microsoft.Health.Dicom.Core.Features.Workitem;
using Microsoft.Health.Dicom.Tests.Common;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Workitem;

public sealed class WorkitemFinalStateValidatorExtensionTests
{
    [Fact]
    public void GivenValidateFinalStateRequirement_WhenProcedureStepStateIsNotCanceledOrCompleted_ThenNoErrorsThrown()
    {
        var dataset = Samples.CreateRandomWorkitemInstanceDataset();

        dataset.AddOrUpdate(DicomTag.ProcedureStepState, ProcedureStepStateConstants.Scheduled);

        WorkitemFinalStateValidatorExtension.ValidateFinalStateRequirement(dataset);
    }

    [Fact]
    public void GivenValidateFinalStateRequirement_WhenProcedureStepStateIsCanceledAndRequirementIsNotMet_ThenThrows()
    {
        var dataset = Samples.CreateRandomWorkitemInstanceDataset();

        dataset.AddOrUpdate(DicomTag.ProcedureStepState, ProcedureStepStateConstants.Canceled);

        Assert.Throws<DatasetValidationException>(() => WorkitemFinalStateValidatorExtension.ValidateFinalStateRequirement(dataset));
    }

    [Fact]
    public void GivenValidateFinalStateRequirement_WhenProcedureStepStateIsCanceledRequirementIsMet_ThenDoesNotThrow()
    {
        var dataset = Samples.CreateRandomWorkitemInstanceDataset();
        dataset.AddOrUpdate(DicomTag.SOPClassUID, TestUidGenerator.Generate());
        dataset.AddOrUpdate(DicomTag.ProcedureStepState, ProcedureStepStateConstants.Canceled);
        dataset.AddOrUpdate(DicomTag.ScheduledProcedureStepModificationDateTime, DateTime.UtcNow);

        var cancelRequestDataset = Samples.CreateWorkitemCancelRequestDataset(@"Cancel from unit test");
        var fullDataset = PopulateCancelRequestAttributes(dataset, cancelRequestDataset, ProcedureStepState.Canceled);

        WorkitemFinalStateValidatorExtension.ValidateFinalStateRequirement(fullDataset);
    }

    [Fact]
    public void GivenValidateFinalStateRequirement_WhenProcedureStepStateIsCanceledWhenLevel3SequenceTagRequirementIsNotMet_ThenThrows()
    {
        var dataset = Samples.CreateRandomWorkitemInstanceDataset();
        dataset.AddOrUpdate(DicomTag.SOPClassUID, TestUidGenerator.Generate());
        dataset.AddOrUpdate(DicomTag.ProcedureStepState, ProcedureStepStateConstants.Canceled);
        dataset.AddOrUpdate(DicomTag.ScheduledProcedureStepModificationDateTime, DateTime.UtcNow);

        var cancelRequestDataset = Samples.CreateWorkitemCancelRequestDataset(@"Cancel from unit test");
        var fullDataset = PopulateCancelRequestAttributes(dataset, cancelRequestDataset, ProcedureStepState.Canceled);
        fullDataset.AddOrUpdate(new DicomSequence(DicomTag.ProcedureStepProgressInformationSequence, new DicomDataset
        {
            new DicomSequence(DicomTag.ProcedureStepDiscontinuationReasonCodeSequence, new DicomDataset())
        }));

        Assert.Throws<DatasetValidationException>(() => WorkitemFinalStateValidatorExtension.ValidateFinalStateRequirement(fullDataset));
    }

    private static DicomDataset PopulateCancelRequestAttributes(
        DicomDataset workitemDataset,
        DicomDataset cancelRequestDataset,
        ProcedureStepState procedureStepState)
    {
        workitemDataset.AddOrUpdate(DicomTag.ProcedureStepCancellationDateTime, DateTime.UtcNow);
        workitemDataset.AddOrUpdate(DicomTag.ProcedureStepState, procedureStepState.GetStringValue());

        var cancellationReason = cancelRequestDataset.GetSingleValueOrDefault<string>(DicomTag.ReasonForCancellation, string.Empty);
        var discontinuationReasonCodeSequence = new DicomSequence(DicomTag.ProcedureStepDiscontinuationReasonCodeSequence, new DicomDataset
        {
            { DicomTag.ReasonForCancellation, cancellationReason }
        });
        workitemDataset.AddOrUpdate(discontinuationReasonCodeSequence);

        var progressInformationSequence = new DicomSequence(DicomTag.ProcedureStepProgressInformationSequence, new DicomDataset
        {
            { DicomTag.ProcedureStepCancellationDateTime, DateTime.UtcNow },
            new DicomSequence(DicomTag.ProcedureStepDiscontinuationReasonCodeSequence, new DicomDataset
                {
                    { DicomTag.ReasonForCancellation, cancellationReason }
                }),
            new DicomSequence(DicomTag.ProcedureStepCommunicationsURISequence, new DicomDataset
                {
                    { DicomTag.ContactURI, cancelRequestDataset.GetSingleValueOrDefault<string>(DicomTag.ContactURI, string.Empty) },
                    { DicomTag.ContactDisplayName, cancelRequestDataset.GetSingleValueOrDefault<string>(DicomTag.ContactDisplayName, string.Empty) },
                })
        });
        workitemDataset.AddOrUpdate(progressInformationSequence);

        // TODO: Remove this once Update workitem feature is implemented
        // This is a workaround for Cancel workitem to work without Update workitem
        if (cancelRequestDataset.TryGetSequence(DicomTag.UnifiedProcedureStepPerformedProcedureSequence, out var unifiedProcedureStepPerformedProcedureSequence))
        {
            workitemDataset.AddOrUpdate(unifiedProcedureStepPerformedProcedureSequence);
        }

        return workitemDataset;
    }
}
