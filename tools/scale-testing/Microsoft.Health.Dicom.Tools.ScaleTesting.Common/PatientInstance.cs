// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Tools.ScaleTesting.Common
{
    public class PatientInstance
    {
        public string Name { get; set; }

        public string PatientId { get; set; }

        public string PatientSex { get; set; }

        public string PatientBirthDate { get; set; }

        public string PatientAge { get; set; }

        public string PatientWeight { get; set; }

        public string PatientOccupation { get; set; }

        public string PhysicianName { get; set; }

        public string StudyUid { get; set; }

        public string SeriesUid { get; set; }

        public string SeriesIndex { get; set; }

        public string InstanceUid { get; set; }

        public string InstanceIndex { get; set; }

        public string Modality { get; set; }

        public string AccessionNumber { get; set; }

        public string StudyDate { get; set; }

        public string StudyDescription { get; set; }

        public string PerformedProcedureStepStartDate { get; set; }
    }
}
