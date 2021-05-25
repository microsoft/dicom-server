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

            // run the loop
            ObservationParseLoop(dataset, patientRef, imagingStudyRef, parsedObservations);

            // Set each observation status to Preliminary
            foreach (Observation doseSummary in parsedObservations.DoseSummaries)
            {
                doseSummary.Status = ObservationStatus.Preliminary;
            }

            foreach (Observation irradiationEvent in parsedObservations.IrradiationEvents)
            {
                irradiationEvent.Status = ObservationStatus.Preliminary;
            }

            return parsedObservations;
        }

        /// <summary>
        /// Helper function to add all found Dose Summaries and Irradiation Events to the passed observation set
        /// </summary>
        private static void ObservationParseLoop(
            DicomDataset dataset,
            ResourceReference patientRef,
            ResourceReference imagingStudyRef,
            ParsedObservation observations)
        {
            var report = new DicomStructuredReport(dataset);
            // see if the current report code matches any of the observation container codes; if so, create the appropriate observation
            try
            {
                DicomCodeItem code = report.Code;
                if (code != null)
                {
                    (string Value, string Scheme) lookupTuple = (report.Code.Value, report.Code.Scheme);
                    if (ObservationConstants.DoseSummaryReportCodes.Contains(lookupTuple))
                    {
                        Observation doseSummary = CreateDoseSummary(
                            dataset,
                            imagingStudyRef,
                            patientRef);
                        observations.DoseSummaries.Add(doseSummary);
                    }

                    if (ObservationConstants.IrradiationEventCodes.Contains(lookupTuple))
                    {
                        Observation irradiationEvent = CreateIrradiationEvent(dataset, patientRef);
                        observations.IrradiationEvents.Add(irradiationEvent);
                    }
                }
            }
            catch (MissingMemberException)
            {
                // Occurs when a required attribute is unable to be extracted from a dataset.
                // Ignore and move onto the next one.
            }
            catch (Exception)
            {
                // Occurs when the report does not have a .Code; expected as not all items need to have a code.
                // Ignore and move onto the next one.
            }

            // Recursively iterate through every child in the document checking for nested observations.
            // Return the final aggregated list of observations.
            foreach (DicomContentItem childItem in report.Children())
            {
                ObservationParseLoop(childItem.Dataset, patientRef, imagingStudyRef, observations);
            }
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
            // Attempt to get the StudyInstanaceUID from the report; if it is not there fallback to the Tag value in the dataset
            var report = new DicomStructuredReport(dataset);
            var studyInstanceUid = report.Get<string>(
                new DicomCodeItem(
                    ObservationConstants.StudyInstanceUid.Item1,
                    ObservationConstants.StudyInstanceUid.Item2,
                    "Study Instance UID"),
                "");
            if (string.IsNullOrEmpty(studyInstanceUid))
            {
                if (!dataset.TryGetSingleValue(DicomTag.StudyInstanceUID, out studyInstanceUid))
                {
                    throw new MissingMemberException($"Unable to {nameof(DicomTag.StudyInstanceUID)} from dose summary observation dataset");
                }
            }

            Identifier studyInstanceIdentifier = ImagingStudyIdentifierUtility.CreateIdentifier(studyInstanceUid);
            observation.Identifier.Add(studyInstanceIdentifier);


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
                // throw new MissingMemberException($"Unable to {nameof(DicomTag.AccessionNumber)} from dose summary observation dataset");
                // Accession numbers is marked as a 0..1 identifier. If its not there, ignore it.
            }

            // Add all structured report information
            ApplyDicomTransforms(observation, dataset, new List<(string, string)>()
            {
                ObservationConstants.EntranceExposureAtRp,
                ObservationConstants.AccumulatedAverageGlandularDose,
                ObservationConstants.DoseAreaProductTotal,
                ObservationConstants.FluoroDoseAreaProductTotal,
                ObservationConstants.AcquisitionDoseAreaProductTotal,
                ObservationConstants.TotalFluoroTime,
                ObservationConstants.TotalNumberOfRadiographicFrames,
                ObservationConstants.AdministeredActivity,
                ObservationConstants.CtDoseLengthProductTotal,
                ObservationConstants.TotalNumberOfIrradiationEvents,
                ObservationConstants.MeanCtdIvol,
                ObservationConstants.RadiopharmaceuticalAgent,
                ObservationConstants.RadiopharmaceuticalVolume,
                ObservationConstants.Radionuclide,
                ObservationConstants.RouteOfAdministration,
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
                    .First(item => (item.Code.Value, item.Code.Scheme) == ObservationConstants.IrradiationEventUid);
                DicomUID irradiationEventUidValue = irradiationEventItem.Get<DicomUID>();
                // TODO is this the right "system"???
                var system = irradiationEventItem.Code.Scheme == ObservationConstants.Dcm
                    ? ObservationConstants.DcmSystem
                    : ObservationConstants.SctSystem;
                var identifier = new Identifier(irradiationEventUidValue.Name, irradiationEventUidValue.UID);
                observation.Identifier.Add(identifier);
            }
            catch (Exception ex)
            {
                throw new MissingMemberException($"unable to extract {nameof(ObservationConstants.IrradiationEventUid)} from dataset: {ex.Message}");
            }

            // Extract the necessary information
            ApplyDicomTransforms(observation, report.Dataset, new List<(string, string)>()
            {
                ObservationConstants.MeanCtdIvol,
                ObservationConstants.Dlp,
                ObservationConstants.CtdIwPhantomType
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

        /// <summary>
        /// Lookup map of Dicom Report Codes `(code,string)` to Observation mutator
        /// </summary>
        private static readonly Dictionary<(string, string), Action<Observation, DicomStructuredReport>> DicomComponentMutators = new()
        {
            [ObservationConstants.IrradiationEventUid] = SetIrradiationEventUid,
            [ObservationConstants.EntranceExposureAtRp] = AddComponentForDicomMeasuredValue,
            [ObservationConstants.AccumulatedAverageGlandularDose] = AddComponentForDicomMeasuredValue,
            [ObservationConstants.DoseAreaProductTotal] = AddComponentForDicomMeasuredValue,
            [ObservationConstants.FluoroDoseAreaProductTotal] = AddComponentForDicomMeasuredValue,
            [ObservationConstants.AcquisitionDoseAreaProductTotal] = AddComponentForDicomMeasuredValue,
            [ObservationConstants.TotalFluoroTime] = AddComponentForDicomMeasuredValue,
            [ObservationConstants.TotalNumberOfRadiographicFrames] = AddComponentForDicomIntegerValue,
            [ObservationConstants.AdministeredActivity] = AddComponentForDicomMeasuredValue,
            [ObservationConstants.CtDoseLengthProductTotal] = AddComponentForDicomMeasuredValue,
            [ObservationConstants.TotalNumberOfIrradiationEvents] = AddComponentForDicomIntegerValue,
            [ObservationConstants.MeanCtdIvol] = AddComponentForDicomMeasuredValue,
            [ObservationConstants.RadiopharmaceuticalAgent] = AddComponentForDicomTextValue,
            [ObservationConstants.RadiopharmaceuticalVolume] = AddComponentForDicomMeasuredValue,
            [ObservationConstants.Radionuclide] = AddComponentForDicomTextValue,
            [ObservationConstants.RouteOfAdministration] = AddComponentForDicomMeasuredValue,
            [ObservationConstants.Dlp] = AddComponentForDicomMeasuredValue,
            [ObservationConstants.CtdIwPhantomType] = AddComponentForDicomCodeValue,
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
                ObservationConstants.Dcm => ObservationConstants.DcmSystem,
                ObservationConstants.Sct => ObservationConstants.SctSystem,
                _ => throw new InvalidOperationException($"unsupported code system: {scheme}")
            };
        }
    }
}
