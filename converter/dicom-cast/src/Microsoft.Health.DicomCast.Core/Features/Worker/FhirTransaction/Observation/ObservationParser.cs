// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------


using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dicom;
using Dicom.StructuredReport;
using Hl7.Fhir.Model;
using Microsoft.Health.DicomCast.Core.Features.Fhir;

namespace Microsoft.Health.DicomCast.Core.Features.Worker.FhirTransaction
{
    /// <summary>
    /// Container for holding the parsed Observations from a DicomDataset
    /// </summary>
    public class ParsedObservation
    {
        public Collection<Observation> DoseSummaries { get; } = new();
        public Collection<Observation> IrradiationEvents { get; } = new();
    }

    public static class ObservationParser
    {
        /// <summary>
        /// Parses the DicomStructuredReports in a DicomDataset for the contained Observations.
        /// </summary>
        /// <remarks>
        /// For observation profiles see: https://confluence.hl7.org/display/IMIN/Radiation+Dose+Summary+for+Diagnostic+Procedures+on+FHIR#RadiationDoseSummaryforDiagnosticProceduresonFHIR-DoseSummaryObservationProfile
        /// </remarks>
        /// <param name="dataset">The dataset to parse</param>
        /// <param name="patientRef">Reference to an existing patient to link to</param>
        /// <param name="imagingStudyRef">Reference to an existing ImagingStudy to link to</param>
        /// <returns>An object containing the Observations found in the dataset</returns>
        public static ParsedObservation CreateObservations(
            DicomDataset dataset,
            ResourceReference patientRef,
            ResourceReference imagingStudyRef)
        {
            var parsedObservations = new ParsedObservation();

            // Recursive loop to add all found Dose Summaries and Irradiation Events to the passed observation set
            static void ObservationParseLoop(
                DicomDataset dataset,
                ResourceReference patientRef,
                ResourceReference imagingStudyRef,
                ParsedObservation observations
            )
            {
                var report = new DicomStructuredReport(dataset);
                // see if the current report code matches any of the observation container codes; if so, create the appropriate observation
                try
                {
                    (string Value, string Scheme) lookupTuple = (report.Code.Value, report.Code.Scheme);
                    if (DoseSummaryReportCodes.Contains(lookupTuple))
                    {
                        Observation doseSummary = CreateDoseSummary(
                            dataset,
                            imagingStudyRef,
                            patientRef);
                        observations.DoseSummaries.Add(doseSummary);
                    }

                    if (IrradiationEventCodes.Contains(lookupTuple))
                    {
                        Observation irradiationEvent = CreateIrradiationEvent(dataset, patientRef);
                        observations.IrradiationEvents.Add(irradiationEvent);
                    }
                }
                catch (MissingMemberException)
                {
                    // Occurs when a required attribute is unable to be extracted from a dataset.
                    // Ignore and move onto the next one.
                }
                catch (Exception)
                {
                    // exception thrown if the report does not contain a Code. In which case we ignore it
                    // and move onto the next reports
                }

                // Recursively iterate through every child in the document checking for nested observations.
                // Return the final aggregated list of observations.
                foreach (DicomContentItem childItem in report.Children())
                    ObservationParseLoop(childItem.Dataset, patientRef, imagingStudyRef, observations);
            }

            // run the loop
            ObservationParseLoop(dataset, patientRef, imagingStudyRef, parsedObservations);

            // Set each observation status to Preliminary
            foreach (Observation doseSummary in parsedObservations.DoseSummaries)
                doseSummary.Status = ObservationStatus.Preliminary;
            foreach (Observation irradiationEvent in parsedObservations.IrradiationEvents)
                irradiationEvent.Status = ObservationStatus.Preliminary;

            return parsedObservations;
        }

        /// <summary>
        /// Creates an Observation "Dose Summary" profile based on the the spec outlined at:
        /// https://confluence.hl7.org/display/IMIN/Radiation+Dose+Summary+for+Diagnostic+Procedures+on+FHIR#RadiationDoseSummaryforDiagnosticProceduresonFHIR-DoseSummaryObservationProfile
        /// </summary>
        /// <returns>A Dose Summary</returns>
        private static Observation CreateDoseSummary(
            DicomDataset dataset,
            ResourceReference imagingStudyRef,
            ResourceReference patientRef)
        {
            // Create the observation
            var observation = new Observation
            {
                // Set the code.coding
                Code = new CodeableConcept("http://loinc.org", "73569-6", "Radiation exposure and protection information"),
                // Add Patient reference
                Subject = patientRef
            };
            // Add ImagingStudy reference
            observation.PartOf.Add(imagingStudyRef);

            // Set identifiers
            if (dataset.TryGetSingleValue(DicomTag.StudyInstanceUID, out string studyUid))
            {
                Identifier identifier = ImagingStudyIdentifierUtility.CreateIdentifier(studyUid);
                observation.Identifier.Add(identifier);
            }
            else
            {
                throw new MissingMemberException($"Unable to {nameof(DicomTag.StudyInstanceUID)} from dose summary observation dataset");
            }

            if (dataset.TryGetSingleValue(DicomTag.AccessionNumber, out string accessionNumber))
            {
                // TODO system correct? seems like a foobar name
                const string accessionSystem = "http://ginormoushospital.org/accession";
                var identifier = new Identifier(accessionSystem, accessionNumber)
                {
                    Type = new CodeableConcept("http://terminology.hl7.org/CodeSystem/v2-0203", "ACSN")
                };
                observation.Identifier.Add(identifier);
            }
            else
            {
                throw new MissingMemberException($"Unable to {nameof(DicomTag.AccessionNumber)} from dose summary observation dataset");
            }

            // Add all structured report information
            ApplyDicomTransforms(observation, dataset, new List<(string, string)>()
            {
                EntranceExposureAtRp,
                AccumulatedAverageGlandularDose,
                DoseAreaProductTotal,
                FluoroDoseAreaProductTotal,
                AcquisitionDoseAreaProductTotal,
                TotalFluoroTime,
                TotalNumberOfRadiographicFrames,
                AdministeredActivity,
                CtDoseLengthProductTotal,
                TotalNumberOfIrradiationEvents,
                MeanCtdIvol,
                RadiopharmaceuticalAgent,
                RadiopharmaceuticalVolume,
                Radionuclide,
                RouteOfAdministration,
            });

            return observation;
        }

        /// <summary>
        /// Creates an Observation "Irradiation Event" based on the spec outlined at:
        /// https://confluence.hl7.org/display/IMIN/Radiation+Dose+Summary+for+Diagnostic+Procedures+on+FHIR#RadiationDoseSummaryforDiagnosticProceduresonFHIR-DoseSummaryObservationProfile
        /// </summary>
        /// <param name="dataset">The dataset to parse</param>
        /// <param name="patientRef">Patient which the Irradiation Event is a subject of</param>
        private static Observation CreateIrradiationEvent(DicomDataset dataset, ResourceReference patientRef)
        {
            var report = new DicomStructuredReport(dataset);
            // create the observation
            var observation = new Observation
            {
                Code = new CodeableConcept("http://dicom.nema.org/resources/ontology/DCM", "113852", "Irradiation Event"),
                Subject = patientRef
            };

            // try to extract the event UID
            try
            {
                DicomContentItem irradiationEventItem = report.Children()
                    .First(item => (item.Code.Value, item.Code.Scheme) == IrradiationEventUid);
                DicomUID irradiationEventUidValue = irradiationEventItem.Get<DicomUID>();
                // TODO is this the right "system"???
                var system = irradiationEventItem.Code.Scheme == Dcm
                    ? DcmSystem
                    : SctSystem;
                var identifier = new Identifier(irradiationEventUidValue.Name, irradiationEventUidValue.UID);
                observation.Identifier.Add(identifier);
            }
            catch (Exception ex)
            {
                throw new MissingMemberException($"unable to extract {nameof(IrradiationEventUid)} from dataset: {ex.Message}");
            }

            // Extract the necessary information
            ApplyDicomTransforms(observation, report.Dataset, new List<(string, string)>()
            {
                MeanCtdIvol,
                Dlp,
                CtdIwPhantomType
            });

            return observation;
        }

        /// <summary>
        /// Mutates the given Observation by recursively parsing the given DicomDataset into
        /// DicomStructuredReports and searching for tags provided.
        /// </summary>
        /// <param name="observation">Observation to mutate</param>
        /// <param name="dataset">DicomDataset to parse for values</param>
        /// <param name="onlyInclude">Dicom structured report codes to parse values from</param>
        private static void ApplyDicomTransforms(Observation observation, DicomDataset dataset, ICollection<(string, string)> onlyInclude = null)
        {
            var report = new DicomStructuredReport(dataset);
            (string Value, string Scheme) lookupTuple = (report.Code.Value, report.Code.Scheme);
            if (onlyInclude == null || onlyInclude.Contains(lookupTuple))
            {
                if (DicomComponentMutators.TryGetValue(lookupTuple, out Action<Observation, DicomStructuredReport> mutator))
                {
                    mutator(observation, report);
                }
                else
                {
                    throw new InvalidProgramException($"no attribute applicator found for dicom code {lookupTuple.ToString()}");
                }
            }

            foreach (DicomContentItem child in report.Children())
                ApplyDicomTransforms(observation, child.Dataset, onlyInclude);
        }

        // https://www.hl7.org/fhir/terminologies-systems.html
        private const string SctSystem = "http://snomed.info/sct";
        private const string DcmSystem = "http://dicom.nema.org/resources/ontology/DCM";

        private const string Dcm = "DCM";
        private const string Sct = "Sct";

        //------------------------------------------------------------
        // Report codes
        // - When you encounter this code in a structured report, it means to create a new "Does Summary" Observation
        //------------------------------------------------------------
        private static readonly (string, string) RadiopharmaceuticalRadiationDoseReport = ("113500", Dcm); // (113500,DCM,"Radiopharmaceutical Radiation Dose Report")
        private static readonly (string, string) XRayRadiationDoseReport = ("113701", Dcm); // (113701,DCM,"X-Ray Radiation Dose Report")


        //------------------------------------------------------------
        // Irradiation Event Codes
        // - When you encounter this code in a structured report, it means to create a new "Irradiation Event" Observation
        //------------------------------------------------------------
        private static readonly (string, string) IrradiationEventXRayData = ("113706", Dcm); // (113706,DCM,"Irradiation Event X-Ray Data")
        private static readonly (string, string) CtAcquisition = ("113819", Dcm); // (113819,DCM,"CT Acquisition")
        private static readonly (string, string) OrganDose = ("113518", "DCM"); // (113518,DCM,"Organ Dose")  => Radiopharmaceutical Radiation Dose Report


        //------------------------------------------------------------
        // Dicom Codes (attribute)
        // - These are report values which map to non component observation attributes.
        //------------------------------------------------------------
        private static readonly (string, string) IrradiationEventUid = ("113769", Dcm); // (113769,DCM,"Irradiation Event UID")
        // private static readonly (string, string) StudyInstanceUid = ("110180", Dcm); // (110180,DCM,"Study Instance UID") TODO maybe (0020,000D) ???
        // private static readonly (string, string) AccessionNumber = ("121022", Dcm); // TODO no sample; maybe (0008,0050) ???
        // private static readonly (string, string) StartOfXrayIrradiation = ("113809", Dcm); // (113809,DCM,"Start of X-ray Irradiation")
        // private static readonly (string, string) IrradiationAuthorizing = ("113850", Dcm); // TODO no sample maybe (121406,DCM,"Reference Authority") ???
        // private static readonly (string, string) PregnancyObservable = ("364320009", Sct); // TODO no sample maybe "(0010,21c0) US" ???

        //------------------------------------------------------------
        // Dicom codes (component)
        // - These are report values which map to Observation.component values
        //------------------------------------------------------------
        // Dose Summary
        // TODO cannot find "DoseSummary.component:effectiveDose" anywhere
        private static readonly (string, string) EntranceExposureAtRp = ("111636", Dcm); // (111636,DCM,"Entrance Exposure at RP")
        private static readonly (string, string) AccumulatedAverageGlandularDose = ("111637", Dcm); // (111637,DCM,"Accumulated Average Glandular Dose")
        private static readonly (string, string) DoseAreaProductTotal = ("113722", Dcm); // (113722,DCM,"Dose Area Product Total")
        private static readonly (string, string) FluoroDoseAreaProductTotal = ("113726", Dcm); // (113726,DCM,"Fluoro Dose Area Product Total")
        private static readonly (string, string) AcquisitionDoseAreaProductTotal = ("113727", Dcm); // (113727,DCM,"Acquisition Dose Area Product Total")
        private static readonly (string, string) TotalFluoroTime = ("113730", Dcm); // (113730,DCM,"Total Fluoro Time")
        private static readonly (string, string) TotalNumberOfRadiographicFrames = ("113731", Dcm); // (113731,DCM,"Total Number of Radiographic Frames")
        private static readonly (string, string) AdministeredActivity = ("113507", Dcm); // (113507,DCM,"Administered activity")
        private static readonly (string, string) CtDoseLengthProductTotal = ("113813", Dcm); // (113813,DCM,"CT Dose Length Product Total") 
        private static readonly (string, string) TotalNumberOfIrradiationEvents = ("113812", Dcm); // (113812,DCM,"Total Number of Irradiation Events")
        private static readonly (string, string) MeanCtdIvol = ("113830", Dcm); // (113830,DCM,"Mean CTDIvol")
        private static readonly (string, string) RadiopharmaceuticalAgent = ("349358000", Sct); // TODO no sample; maybe (F-61FDB,SRT,"Radiopharmaceutical agent") ???
        private static readonly (string, string) RadiopharmaceuticalVolume = ("123005", Dcm); // TODO no sample; maybe (0018,1071) DS ???
        private static readonly (string, string) Radionuclide = ("89457008", Sct); // TODO no sample; maybe (C-10072,SRT,"Radionuclide") ???
        private static readonly (string, string) RouteOfAdministration = ("410675002", Sct); // TODO no sample; maybe (G-C340,SRT,"Route of administration") ???

        // (Ir)radiation Event
        // uses MeanCtdIvol as well
        private static readonly (string, string) Dlp = ("113838", Dcm); // (113838,DCM,"DLP")
        private static readonly (string, string) CtdIwPhantomType = ("113835", Dcm); // (113835,DCM,"CTDIw Phantom Type")

        /// <summary>
        /// DicomStructuredReport codes which mean the start of a Dose Summary
        /// </summary>
        private static List<(string, string)> DoseSummaryReportCodes = new()
        {
            RadiopharmaceuticalRadiationDoseReport,
            XRayRadiationDoseReport,
        };

        /// <summary>
        /// DicomStructuredReport codes which mean the start of an Irradiation Event
        /// </summary>
        private static List<(string, string)> IrradiationEventCodes = new()
        {
            IrradiationEventXRayData,
            CtAcquisition,
            OrganDose
        };

        /// <summary>
        /// Lookup map of Dicom Report Codes `(code,string)` to Observation mutator
        /// </summary>
        private static readonly Dictionary<(string, string), Action<Observation, DicomStructuredReport>> DicomComponentMutators = new()
        {
            [IrradiationEventUid] = SetIrradiationEventUid,
            [EntranceExposureAtRp] = AddComponentForDicomMeasuredValue,
            [AccumulatedAverageGlandularDose] = AddComponentForDicomMeasuredValue,
            [DoseAreaProductTotal] = AddComponentForDicomMeasuredValue,
            [FluoroDoseAreaProductTotal] = AddComponentForDicomMeasuredValue,
            [AcquisitionDoseAreaProductTotal] = AddComponentForDicomMeasuredValue,
            [TotalFluoroTime] = AddComponentForDicomMeasuredValue,
            [TotalNumberOfRadiographicFrames] = AddComponentForDicomIntegerValue,
            [AdministeredActivity] = AddComponentForDicomMeasuredValue,
            [CtDoseLengthProductTotal] = AddComponentForDicomMeasuredValue,
            [TotalNumberOfIrradiationEvents] = AddComponentForDicomIntegerValue,
            [MeanCtdIvol] = AddComponentForDicomMeasuredValue,
            [RadiopharmaceuticalAgent] = AddComponentForDicomTextValue,
            [RadiopharmaceuticalVolume] = AddComponentForDicomMeasuredValue,
            [Radionuclide] = AddComponentForDicomTextValue,
            [RouteOfAdministration] = AddComponentForDicomMeasuredValue,
            [Dlp] = AddComponentForDicomMeasuredValue,
            [CtdIwPhantomType] = AddComponentForDicomCodeValue,
        };

        private static void SetIrradiationEventUid(Observation observation, DicomStructuredReport report)
        {
            var system = GetSystem(report.Code.Scheme);
            var value = report.Get<string>();
            observation.Identifier.Add(new Identifier(system, value));
        }

        private static void AddComponentForDicomMeasuredValue(Observation observation, DicomStructuredReport report)
        {
            var system = GetSystem(report.Code.Scheme);
            var component = new Observation.ComponentComponent
            {
                Code = new CodeableConcept(system, report.Code.Value, report.Code.Meaning),
            };
            DicomMeasuredValue measuredValue = report.Get<DicomMeasuredValue>();
            component.Value = new Quantity(measuredValue.Value, measuredValue.Code.Value);
            observation.Component.Add(component);
        }

        private static void AddComponentForDicomTextValue(Observation observation, DicomStructuredReport report)
        {
            var system = GetSystem(report.Code.Scheme);
            var component = new Observation.ComponentComponent
            {
                Code = new CodeableConcept(system, report.Code.Value, report.Code.Meaning),
            };
            var value = report.Get<string>();
            component.Value = new FhirString(value);
            observation.Component.Add(component);
        }


        private static void AddComponentForDicomCodeValue(Observation observation, DicomStructuredReport report)
        {
            var system = GetSystem(report.Code.Scheme);
            var component = new Observation.ComponentComponent
            {
                Code = new CodeableConcept(system, report.Code.Value, report.Code.Meaning),
            };
            DicomCodeItem codeItem = report.Get<DicomCodeItem>();
            component.Value = new CodeableConcept(system, codeItem.Value, codeItem.Meaning);
            observation.Component.Add(component);
        }

        private static void AddComponentForDicomIntegerValue(Observation observation, DicomStructuredReport report)
        {
            var system = GetSystem(report.Code.Scheme);
            var component = new Observation.ComponentComponent
            {
                Code = new CodeableConcept(system, report.Code.Value, report.Code.Meaning),
            };
            var value = report.Get<int>();
            component.Value = new Integer(value);
            observation.Component.Add(component);
        }

        private static string GetSystem(string scheme)
        {
            return scheme switch
            {
                Dcm => DcmSystem,
                Sct => SctSystem,
                _ => throw new InvalidOperationException($"unsupported code system: {scheme}")
            };
        }
    }
}
