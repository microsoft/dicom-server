// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using FellowOakDicom;
using Microsoft.Health.Dicom.Core.Features.Store;
using Microsoft.Health.Dicom.Core.Features.Workitem;
using Microsoft.Health.Dicom.Tests.Common;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Workitem
{
    public sealed class AddWorkitemDatasetValidatorTests
    {
        [Fact]
        public void GivenValidate_WhenDicomDatasetIsNull_ThrowsArgumentNullException()
        {
            DicomDataset dicomDataset = null;
            var workitemInstanceUid = DicomUID.Generate().UID;

            var target = new AddWorkitemDatasetValidator();

            Assert.Throws<ArgumentNullException>(() => target.Validate(dicomDataset, workitemInstanceUid));
        }

        [Fact]
        public void GivenValidate_WhenWorkitemInstanceUidIsNull_DoesNotThrowException()
        {
            string workitemInstanceUid = DicomUID.Generate().UID;
            DicomDataset dicomDataset = CreateDicomDataset(workitemInstanceUid);

            var target = new AddWorkitemDatasetValidator();

            target.Validate(dicomDataset, workitemInstanceUid);
        }

        [Theory]
        [MemberData(nameof(GetWorkitemRequiredTags))]
        public void GivenValidate_WhenRequiredTagIsMissing_ThrowsDatasetValidationException(DicomTag dicomTag)
        {
            string workitemInstanceUid = DicomUID.Generate().UID;
            var dicomDataset = CreateDicomDataset(workitemInstanceUid);
            dicomDataset.Remove(dicomTag);

            var target = new AddWorkitemDatasetValidator();
            Assert.Throws<DatasetValidationException>(() => target.Validate(dicomDataset, workitemInstanceUid));
        }

        [Fact]
        public void GivenValidate_WhenAffectedSOPInstanceUidDoesNotMatch_ThrowsDatasetValidationException()
        {
            string workitemInstanceUid = DicomUID.Generate().UID;
            var dicomDataset = CreateDicomDataset(workitemInstanceUid);
            var target = new AddWorkitemDatasetValidator();

            Assert.Throws<DatasetValidationException>(() => target.Validate(dicomDataset, DicomUID.Generate().UID));
        }

        private static DicomDataset CreateDicomDataset(string workitemInstanceUid)
        {
            var ds = new DicomDataset();

            ds.Add(DicomTag.AffectedSOPInstanceUID, workitemInstanceUid);
            ds.Add(DicomTag.ScheduledProcedureStepPriority, Guid.NewGuid().ToString("N").Substring(0, 16).ToUpper());
            ds.Add(DicomTag.ProcedureStepLabel, Guid.NewGuid().ToString("N").Substring(0, 16).ToUpper());
            ds.Add(DicomTag.WorklistLabel, Guid.NewGuid().ToString("N").Substring(0, 16).ToUpper());
            ds.Add(DicomTag.ScheduledStationNameCodeSequence, new DicomDataset());
            ds.Add(DicomTag.ScheduledStationClassCodeSequence, new DicomDataset());
            ds.Add(DicomTag.ScheduledStationGeographicLocationCodeSequence, new DicomDataset());
            ds.Add(DicomTag.ScheduledHumanPerformersSequence, new DicomDataset());
            ds.Add(DicomTag.HumanPerformerCodeSequence, new DicomDataset());
            ds.Add(DicomTag.ScheduledProcedureStepStartDateTime, DateTime.Now);
            ds.Add(DicomTag.ExpectedCompletionDateTime, DateTime.Now);
            ds.Add(DicomTag.ScheduledWorkitemCodeSequence, new DicomDataset());
            ds.Add(DicomTag.InputReadinessState, Guid.NewGuid().ToString("N").Substring(0, 16).ToUpper());
            ds.Add(DicomTag.PatientName, Guid.NewGuid().ToString("N").Substring(0, 16).ToUpper());
            ds.Add(DicomTag.PatientID, TestUidGenerator.Generate());
            ds.Add(DicomTag.PatientBirthDate, DateTime.Now.ToString(@"yyyyMMdd"));
            ds.Add(DicomTag.PatientSex, Guid.NewGuid().ToString("N").Substring(0, 16).ToUpper());
            ds.Add(DicomTag.AdmissionID, Guid.NewGuid().ToString("N").Substring(0, 16).ToUpper());
            ds.Add(DicomTag.IssuerOfAdmissionIDSequence, new DicomDataset());
            ds.Add(DicomTag.ReferencedRequestSequence, new DicomDataset());
            ds.Add(DicomTag.AccessionNumber, Guid.NewGuid().ToString("N").Substring(0, 16).ToUpper());
            ds.Add(DicomTag.IssuerOfAccessionNumberSequence, new DicomDataset());
            ds.Add(DicomTag.RequestedProcedureID, Guid.NewGuid().ToString("N").Substring(0, 16).ToUpper());
            ds.Add(DicomTag.RequestingService, Guid.NewGuid().ToString("N").Substring(0, 16).ToUpper());
            ds.Add(DicomTag.ReplacedProcedureStepSequence, new DicomDataset());

            return ds;
        }

        public static IEnumerable<object[]> GetWorkitemRequiredTags()
        {
            yield return new object[] { DicomTag.ScheduledProcedureStepPriority };
            yield return new object[] { DicomTag.ProcedureStepLabel };
            yield return new object[] { DicomTag.WorklistLabel };
            yield return new object[] { DicomTag.ScheduledProcedureStepStartDateTime };
            yield return new object[] { DicomTag.ExpectedCompletionDateTime };
            yield return new object[] { DicomTag.InputReadinessState };
            yield return new object[] { DicomTag.PatientName };
            yield return new object[] { DicomTag.PatientID };
            yield return new object[] { DicomTag.PatientBirthDate };
            yield return new object[] { DicomTag.PatientSex };
            yield return new object[] { DicomTag.AdmissionID };
            yield return new object[] { DicomTag.AccessionNumber };
            yield return new object[] { DicomTag.RequestedProcedureID };
            yield return new object[] { DicomTag.RequestingService };

            yield return new object[] { DicomTag.IssuerOfAdmissionIDSequence };
            yield return new object[] { DicomTag.ReferencedRequestSequence };
            yield return new object[] { DicomTag.IssuerOfAccessionNumberSequence };
            yield return new object[] { DicomTag.ScheduledWorkitemCodeSequence };
            yield return new object[] { DicomTag.ScheduledStationNameCodeSequence };
            yield return new object[] { DicomTag.ScheduledStationClassCodeSequence };
            yield return new object[] { DicomTag.ScheduledStationGeographicLocationCodeSequence };
            yield return new object[] { DicomTag.ScheduledHumanPerformersSequence };
            yield return new object[] { DicomTag.HumanPerformerCodeSequence };
            yield return new object[] { DicomTag.ReplacedProcedureStepSequence };
            yield return new object[] { DicomTag.AffectedSOPInstanceUID };
        }
    }
}
