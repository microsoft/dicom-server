// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.ObjectModel;

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
        private static readonly (string, string) RadiopharmaceuticalRadiationDoseReport = ("113500", Dcm); // (113500,DCM,"Radiopharmaceutical Radiation Dose Report")
        private static readonly (string, string) XRayRadiationDoseReport = ("113701", Dcm); // (113701,DCM,"X-Ray Radiation Dose Report")


        //------------------------------------------------------------
        // Irradiation Event Codes
        // - When you encounter these code in a structured report, it means to create a new "Irradiation Event" Observation
        //------------------------------------------------------------
        private static readonly (string, string) IrradiationEventXRayData = ("113706", Dcm); // (113706,DCM,"Irradiation Event X-Ray Data")
        private static readonly (string, string) CtAcquisition = ("113819", Dcm); // (113819,DCM,"CT Acquisition")
        private static readonly (string, string) OrganDose = ("113518", "DCM"); // (113518,DCM,"Organ Dose")  => Radiopharmaceutical Radiation Dose Report


        //------------------------------------------------------------
        // Dicom Codes (attribute)
        // - These are report values which map to non component observation attributes.
        //------------------------------------------------------------
        public static readonly (string, string) IrradiationEventUid = ("113769", Dcm); // (113769,DCM,"Irradiation Event UID")
        // public static readonly (string, string) StudyInstanceUid = ("110180", Dcm); // (110180,DCM,"Study Instance UID") TODO maybe (0020,000D) ???
        // public static readonly (string, string) AccessionNumber = ("121022", Dcm); // TODO no sample; maybe (0008,0050) ???
        // public static readonly (string, string) StartOfXrayIrradiation = ("113809", Dcm); // (113809,DCM,"Start of X-ray Irradiation")
        // public static readonly (string, string) IrradiationAuthorizing = ("113850", Dcm); // TODO no sample maybe (121406,DCM,"Reference Authority") ???
        // public static readonly (string, string) PregnancyObservable = ("364320009", Sct); // TODO no sample maybe "(0010,21c0) US" ???

        //------------------------------------------------------------
        // Dicom codes (component)
        // - These are report values which map to Observation.component values
        //------------------------------------------------------------
        // Dose Summary
        // TODO cannot find "DoseSummary.component:effectiveDose" anywhere
        public static readonly (string, string) EntranceExposureAtRp = ("111636", Dcm); // (111636,DCM,"Entrance Exposure at RP")
        public static readonly (string, string) AccumulatedAverageGlandularDose = ("111637", Dcm); // (111637,DCM,"Accumulated Average Glandular Dose")
        public static readonly (string, string) DoseAreaProductTotal = ("113722", Dcm); // (113722,DCM,"Dose Area Product Total")
        public static readonly (string, string) FluoroDoseAreaProductTotal = ("113726", Dcm); // (113726,DCM,"Fluoro Dose Area Product Total")
        public static readonly (string, string) AcquisitionDoseAreaProductTotal = ("113727", Dcm); // (113727,DCM,"Acquisition Dose Area Product Total")
        public static readonly (string, string) TotalFluoroTime = ("113730", Dcm); // (113730,DCM,"Total Fluoro Time")
        public static readonly (string, string) TotalNumberOfRadiographicFrames = ("113731", Dcm); // (113731,DCM,"Total Number of Radiographic Frames")
        public static readonly (string, string) AdministeredActivity = ("113507", Dcm); // (113507,DCM,"Administered activity")
        public static readonly (string, string) CtDoseLengthProductTotal = ("113813", Dcm); // (113813,DCM,"CT Dose Length Product Total") 
        public static readonly (string, string) TotalNumberOfIrradiationEvents = ("113812", Dcm); // (113812,DCM,"Total Number of Irradiation Events")
        public static readonly (string, string) MeanCtdIvol = ("113830", Dcm); // (113830,DCM,"Mean CTDIvol")
        public static readonly (string, string) RadiopharmaceuticalAgent = ("349358000", Sct); // TODO no sample; maybe (F-61FDB,SRT,"Radiopharmaceutical agent") ???
        public static readonly (string, string) RadiopharmaceuticalVolume = ("123005", Dcm); // TODO no sample; maybe (0018,1071) DS ???
        public static readonly (string, string) Radionuclide = ("89457008", Sct); // TODO no sample; maybe (C-10072,SRT,"Radionuclide") ???
        public static readonly (string, string) RouteOfAdministration = ("410675002", Sct); // TODO no sample; maybe (G-C340,SRT,"Route of administration") ???

        // (Ir)radiation Event
        // uses MeanCtdIvol as well
        public static readonly (string, string) Dlp = ("113838", Dcm); // (113838,DCM,"DLP")
        public static readonly (string, string) CtdIwPhantomType = ("113835", Dcm); // (113835,DCM,"CTDIw Phantom Type")

        /// <summary>
        /// DicomStructuredReport codes which mean the start of a Dose Summary
        /// </summary>
        public static readonly Collection<(string, string)> DoseSummaryReportCodes = new()
        {
            RadiopharmaceuticalRadiationDoseReport,
            XRayRadiationDoseReport,
        };

        /// <summary>
        /// DicomStructuredReport codes which mean the start of an Irradiation Event
        /// </summary>
        public static readonly Collection<(string, string)> IrradiationEventCodes = new()
        {
            IrradiationEventXRayData,
            CtAcquisition,
            OrganDose
        };
    }
}
