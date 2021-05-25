// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.ObjectModel;
using Dicom.StructuredReport;

namespace Microsoft.Health.DicomCast.Core.Features.Worker.FhirTransaction
{
    /// <summary>
    /// Dicom structured report codes used by FHIR Observation profiles
    /// </summary>
    public static class ObservationConstants
    {
        // https://www.hl7.org/fhir/terminologies-systems.html
        public const string SctSystem = "http://snomed.info/sct";
        public const string DcmSystem = "http://dicom.nema.org/resources/ontology/DCM";

        public const string Dcm = "DCM";
        public const string Sct = "Sct";

        //------------------------------------------------------------
        // Report codes
        // - When you encounter these codes in a structured report, it means to create a new "Does Summary" Observation
        //------------------------------------------------------------
        private static readonly DicomCodeItem RadiopharmaceuticalRadiationDoseReport = new("113500", Dcm, "Radiopharmaceutical Radiation Dose Report");
        public static readonly DicomCodeItem XRayRadiationDoseReport = new("113701", Dcm, "X-Ray Radiation Dose Report");


        //------------------------------------------------------------
        // Irradiation Event Codes
        // - When you encounter these code in a structured report, it means to create a new "Irradiation Event" Observation
        //------------------------------------------------------------
        public static readonly DicomCodeItem IrradiationEventXRayData = new("113706", Dcm, "Irradiation Event X-Ray Data");
        public static readonly DicomCodeItem CtAcquisition = new("113819", Dcm, "CT Acquisition");
        public static readonly DicomCodeItem OrganDose = new("113518", Dcm, "Organ Dose");


        //------------------------------------------------------------
        // Dicom Codes (attribute)
        // - These are report values which map to non component observation attributes.
        //------------------------------------------------------------
        public static readonly DicomCodeItem IrradiationEventUid = new("113769", Dcm, "Irradiation Event UID");
        public static readonly DicomCodeItem StudyInstanceUid = new("110180", Dcm, "Study Instance UID");
        public static readonly DicomCodeItem AccessionNumber = new("121022", Dcm, ""); // TODO no sample; maybe (0008,0050) ???
        public static readonly DicomCodeItem StartOfXrayIrradiation = new("113809", Dcm, ""); // (113809,DCM,"Start of X-ray Irradiation")
        public static readonly DicomCodeItem IrradiationAuthorizing = new("113850", Dcm, ""); // TODO no sample maybe (121406,DCM,"Reference Authority") ???
        public static readonly DicomCodeItem PregnancyObservable = new("364320009", Sct, ""); // TODO no sample maybe "(0010,21c0) US" ???

        //------------------------------------------------------------
        // Dicom codes (component)
        // - These are report values which map to Observation.component values
        //------------------------------------------------------------
        // Dose Summary
        // TODO cannot find "DoseSummary.component:effectiveDose" anywhere
        public static readonly DicomCodeItem EntranceExposureAtRp = new("111636", Dcm, ""); // (111636,DCM,"Entrance Exposure at RP")
        public static readonly DicomCodeItem AccumulatedAverageGlandularDose = new("111637", Dcm, ""); // (111637,DCM,"Accumulated Average Glandular Dose")
        public static readonly DicomCodeItem DoseAreaProductTotal = new("113722", Dcm, ""); // (113722,DCM,"Dose Area Product Total")
        public static readonly DicomCodeItem FluoroDoseAreaProductTotal = new("113726", Dcm, ""); // (113726,DCM,"Fluoro Dose Area Product Total")
        public static readonly DicomCodeItem AcquisitionDoseAreaProductTotal = new("113727", Dcm, ""); // (113727,DCM,"Acquisition Dose Area Product Total")
        public static readonly DicomCodeItem TotalFluoroTime = new("113730", Dcm, ""); // (113730,DCM,"Total Fluoro Time")
        public static readonly DicomCodeItem TotalNumberOfRadiographicFrames = new("113731", Dcm, ""); // (113731,DCM,"Total Number of Radiographic Frames")
        public static readonly DicomCodeItem AdministeredActivity = new("113507", Dcm, ""); // (113507,DCM,"Administered activity")
        public static readonly DicomCodeItem CtDoseLengthProductTotal = new("113813", Dcm, ""); // (113813,DCM,"CT Dose Length Product Total") 
        public static readonly DicomCodeItem TotalNumberOfIrradiationEvents = new("113812", Dcm, ""); // (113812,DCM,"Total Number of Irradiation Events")
        public static readonly DicomCodeItem MeanCtdIvol = new("113830", Dcm, ""); // (113830,DCM,"Mean CTDIvol")
        public static readonly DicomCodeItem RadiopharmaceuticalAgent = new("349358000", Sct, ""); // TODO no sample; maybe (F-61FDB,SRT,"Radiopharmaceutical agent") ???
        public static readonly DicomCodeItem RadiopharmaceuticalVolume = new("123005", Dcm, ""); // TODO no sample; maybe (0018,1071) DS ???
        public static readonly DicomCodeItem Radionuclide = new("89457008", Sct, ""); // TODO no sample; maybe (C-10072,SRT,"Radionuclide") ???
        public static readonly DicomCodeItem RouteOfAdministration = new("410675002", Sct, ""); // TODO no sample; maybe (G-C340,SRT,"Route of administration") ???

        // (Ir)radiation Event
        // uses MeanCtdIvol as well
        public static readonly DicomCodeItem Dlp = new("113838", Dcm, ""); // (113838,DCM,"DLP")
        public static readonly DicomCodeItem CtdIwPhantomType = new("113835", Dcm, ""); // (113835,DCM,"CTDIw Phantom Type")

        /// <summary>
        /// DicomStructuredReport codes which mean the start of a Dose Summary
        /// </summary>
        public static readonly Collection<DicomCodeItem> DoseSummaryReportCodes = new()
        {
            RadiopharmaceuticalRadiationDoseReport,
            XRayRadiationDoseReport,
        };

        /// <summary>
        /// DicomStructuredReport codes which mean the start of an Irradiation Event
        /// </summary>
        public static readonly Collection<DicomCodeItem> IrradiationEventCodes = new()
        {
            IrradiationEventXRayData,
            CtAcquisition,
            OrganDose
        };
    }
}
