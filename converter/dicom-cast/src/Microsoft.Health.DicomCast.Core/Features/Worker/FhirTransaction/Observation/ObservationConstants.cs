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
        public const string Ln = "LN";

        //------------------------------------------------------------
        // Report codes
        // - When you encounter these codes in a structured report, it means to create a new "Does Summary" Observation
        //------------------------------------------------------------
        public static readonly DicomCodeItem RadiopharmaceuticalRadiationDoseReport = new("113500", Dcm, "Radiopharmaceutical Radiation Dose Report");
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
        public static readonly DicomCodeItem IrradiationAuthorizingPerson = new("113850", Dcm, "Irradiation Authorizing");
        public static readonly DicomCodeItem PregnancyObservation = new("364320009", Sct, "Pregnancy observable");
        public static readonly DicomCodeItem IndicationObservation = new("18785-6", Ln, "Indications for Procedure");
        public static readonly DicomCodeItem IrradiatingDevice = new("113859", Dcm, "Irradiating Device");

        public static readonly DicomCodeItem IrradiationEventUid = new("113769", Dcm, "Irradiation Event UID");
        public static readonly DicomCodeItem StudyInstanceUid = new("110180", Dcm, "Study Instance UID");
        public static readonly DicomCodeItem AccessionNumber = new("121022", Dcm, "Accession Number");
        public static readonly DicomCodeItem StartOfXrayIrradiation = new("113809", Dcm, "Start of X-Ray Irradiation”)");

        //------------------------------------------------------------
        // Dicom codes (component)
        // - These are report values which map to Observation.component values
        //------------------------------------------------------------
        // Study
        public static readonly DicomCodeItem EntranceExposureAtRp = new("111636", Dcm, "Entrance Exposure at RP");
        public static readonly DicomCodeItem AccumulatedAverageGlandularDose = new("111637", Dcm, "Accumulated Average Glandular Dose");
        public static readonly DicomCodeItem DoseAreaProductTotal = new("113722", Dcm, "Dose Area Product Total");
        public static readonly DicomCodeItem FluoroDoseAreaProductTotal = new("113726", Dcm, "Fluoro Dose Area Product Total");
        public static readonly DicomCodeItem AcquisitionDoseAreaProductTotal = new("113727", Dcm, "Acquisition Dose Area Product Total");
        public static readonly DicomCodeItem TotalFluoroTime = new("113730", Dcm, "Total Fluoro Time");
        public static readonly DicomCodeItem TotalNumberOfRadiographicFrames = new("113731", Dcm, "Total Number of Radiographic Frames");
        public static readonly DicomCodeItem AdministeredActivity = new("113507", Dcm, "Administered activity");
        public static readonly DicomCodeItem CtDoseLengthProductTotal = new("113813", Dcm, "CT Dose Length Product Total");
        public static readonly DicomCodeItem TotalNumberOfIrradiationEvents = new("113812", Dcm, "");
        public static readonly DicomCodeItem RadiopharmaceuticalAgent = new("349358000", Sct, "Radiopharmaceutical agent");
        public static readonly DicomCodeItem Radionuclide = new("89457008", Sct, "Radionuclide");
        public static readonly DicomCodeItem RadiopharmaceuticalVolume = new("123005", Dcm, "Radiopharmaceutical Volume");
        public static readonly DicomCodeItem RouteOfAdministration = new("410675002", Sct, "Route of administration");

        // (Ir)radiation Event
        // uses MeanCtdIvol as well
        public static readonly DicomCodeItem MeanCtdIvol = new("113830", Dcm, "Mean CTDIvol");
        public static readonly DicomCodeItem Dlp = new("113838", Dcm, "DLP");
        public static readonly DicomCodeItem TargetRegion = new("123014", Dcm, "Target Region");
        public static readonly DicomCodeItem CtdIwPhantomType = new("113835", Dcm, "CTDIw Phantom Type");

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
