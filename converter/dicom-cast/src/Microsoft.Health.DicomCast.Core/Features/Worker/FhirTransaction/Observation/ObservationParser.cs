// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Dicom;
using Dicom.StructuredReport;
using EnsureThat;
using Hl7.Fhir.Model;

namespace Microsoft.Health.DicomCast.Core.Features.Worker.FhirTransaction
{
    public class ObservationParser
    {
        public IReadOnlyCollection<Observation> Parse(DicomDataset dataset, ResourceReference patientReference, ResourceReference imagingStudyReference, Identifier identifier)
        {
            EnsureArg.IsNotNull(dataset, nameof(dataset));
            EnsureArg.IsNotNull(patientReference, nameof(patientReference));
            EnsureArg.IsNotNull(imagingStudyReference, nameof(imagingStudyReference));
            EnsureArg.IsNotNull(identifier, nameof(identifier));

            var observations = new List<Observation>();
            ParseDicomDataset(dataset, patientReference, imagingStudyReference, observations, identifier);

            return observations;
        }

        private void ParseDicomDataset(DicomDataset dataset, ResourceReference patientReference, ResourceReference imagingStudyReference, List<Observation> observations, Identifier identifier)
        {
            var structuredReport = new DicomStructuredReport(dataset);

            try
            {
                if (ObservationConstants.IrradiationEvents.Contains(structuredReport.Code))
                {
                    observations.Add(CreateIrradiationEvent(dataset, patientReference, identifier));
                }

                if (ObservationConstants.DoseSummaryReportCodes.Contains(structuredReport.Code))
                {
                    observations.Add(CreateDoseSummary(dataset, imagingStudyReference, patientReference, identifier));
                }
            }
            catch (DicomDataException)
            {
                // Ignore, this occurs when the report has no content sequence and we attempt to access report.Code; i.e and empty report
            }
            catch (MissingMemberException)
            {
                // Occurs when a required attribute is unable to be extracted from a dataset.
                // Ignore and move onto the next one.
            }

            // Recursively iterate through every child in the document checking for nested observations.
            // Return the final aggregated list of observations.
            foreach (DicomContentItem childItem in structuredReport.Children())
            {
                ParseDicomDataset(childItem.Dataset, patientReference, imagingStudyReference, observations, identifier);
            }
        }

        private static Observation CreateDoseSummary(
            DicomDataset dataset,
            ResourceReference imagingStudyReference,
            ResourceReference patientReference,
            Identifier identifier)
        {
            // Create the observation
            var observation = new Observation
            {
                // Set the code.coding
                Code = ObservationConstants.RadiationExposureCodeableConcept,
                // Add Patient reference
                Subject = patientReference,
                Status = ObservationStatus.Preliminary,
            };
            // Add ImagingStudy reference
            observation.PartOf.Add(imagingStudyReference);

            var report = new DicomStructuredReport(dataset);

            observation.Identifier.Add(identifier);

            // Try to get accession number from report first then tag; ignore if it is not present it is not a required identifier.
            string accessionNumber = report.Get(ObservationConstants.AccessionNumber, string.Empty);
            if (string.IsNullOrEmpty(accessionNumber))
            {
                dataset.TryGetSingleValue(DicomTag.AccessionNumber, out accessionNumber);
            }

            if (!string.IsNullOrEmpty(accessionNumber))
            {
                var accessionIdentifier = new Identifier
                {
                    Value = accessionNumber,
                    Type = ObservationConstants.AccessionCodeableConcept
                };
                observation.Identifier.Add(accessionIdentifier);
            }

            // Add all structured report information
            ApplyDicomTransforms(observation, dataset, new Collection<DicomCodeItem>()
            {
                ObservationConstants.DoseRpTotal,
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

        private static Observation CreateIrradiationEvent(DicomDataset dataset, ResourceReference patientRef, Identifier identifier)
        {
            var report = new DicomStructuredReport(dataset);
            // create the observation
            var observation = new Observation
            {
                Code = ObservationConstants.IrradiationEventCodeableConcept,
                Subject = patientRef,
                Status = ObservationStatus.Preliminary,
            };

            // try to extract the event UID
            DicomUID irradiationEventUidValue = report.Get<DicomUID>(ObservationConstants.IrradiationEventUid, null);
            if (irradiationEventUidValue == null)
            {
                throw new MissingMemberException($"unable to extract {nameof(ObservationConstants.IrradiationEventUid)} from dataset");
            }

            observation.Identifier.Add(identifier);

            DicomCodeItem bodySite = report.Get<DicomCodeItem>(ObservationConstants.TargetRegion, null);
            if (bodySite != null)
            {
                observation.BodySite = new CodeableConcept(
                    GetSystem(bodySite.Scheme),
                    bodySite.Value,
                    bodySite.Meaning);
            }

            // Extract the necessary information
            ApplyDicomTransforms(observation, report.Dataset, new List<DicomCodeItem>()
            {
                ObservationConstants.MeanCtdIvol,
                ObservationConstants.Dlp,
                ObservationConstants.CtdIwPhantomType
            });

            return observation;
        }

        private static void ApplyDicomTransforms(Observation observation,
            DicomDataset dataset,
            IEnumerable<DicomCodeItem> reportCodesToParse)
        {
            var report = new DicomStructuredReport(dataset);
            foreach (DicomCodeItem item in reportCodesToParse)
            {
                if (DicomComponentMutators.TryGetValue(item,
                    out Action<Observation, DicomStructuredReport, DicomCodeItem> mutator))
                {
                    mutator(observation, report, item);
                }
            }

            foreach (DicomContentItem dicomContentItem in report.Children())
            {
                ApplyDicomTransforms(observation, dicomContentItem.Dataset, reportCodesToParse);
            }
        }

        /// <summary>
        /// Lookup map of DicomCodeItem to Fhir Observation mutator
        /// </summary>
        private static readonly Dictionary<DicomCodeItem, Action<Observation, DicomStructuredReport, DicomCodeItem>> DicomComponentMutators = new()
        {
            [ObservationConstants.EntranceExposureAtRp] = AddComponentForDicomMeasuredValue,
            [ObservationConstants.DoseRpTotal] = AddComponentForDicomMeasuredValue,
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
            [ObservationConstants.RouteOfAdministration] = AddComponentForDicomCodeValue,
            [ObservationConstants.Dlp] = AddComponentForDicomMeasuredValue,
            [ObservationConstants.CtdIwPhantomType] = AddComponentForDicomCodeValue,
        };

        private static void AddComponentForDicomMeasuredValue(Observation observation,
            DicomStructuredReport report,
            DicomCodeItem codeItem)
        {
            var system = GetSystem(codeItem.Scheme);
            var component = new Observation.ComponentComponent
            {
                Code = new CodeableConcept(system, codeItem.Value, codeItem.Meaning),
            };
            DicomMeasuredValue measuredValue = report.Get<DicomMeasuredValue>(codeItem, null);
            if (measuredValue != null)
            {
                component.Value = new Quantity(measuredValue.Value, measuredValue.Code.Value);
                observation.Component.Add(component);
            }
        }

        private static void AddComponentForDicomTextValue(Observation observation,
            DicomStructuredReport report,
            DicomCodeItem codeItem)
        {
            string system = GetSystem(codeItem.Scheme);
            var component = new Observation.ComponentComponent
            {
                Code = new CodeableConcept(system, codeItem.Value, codeItem.Meaning),
            };
            string value = report.Get(codeItem, string.Empty);
            if (!string.IsNullOrEmpty(value))
            {
                component.Value = new FhirString(value);
                observation.Component.Add(component);
            }
        }


        private static void AddComponentForDicomCodeValue(Observation observation,
            DicomStructuredReport report,
            DicomCodeItem codeItem)
        {
            string system = GetSystem(codeItem.Scheme);
            var component = new Observation.ComponentComponent
            {
                Code = new CodeableConcept(system, codeItem.Value, codeItem.Meaning),
            };
            var value = report.Get<DicomCodeItem>(codeItem, null);
            if (value != null)
            {
                component.Value = new CodeableConcept(system, value.Value, value.Meaning);
                observation.Component.Add(component);
            }
        }

        private static void AddComponentForDicomIntegerValue(Observation observation,
            DicomStructuredReport report,
            DicomCodeItem codeItem)
        {
            string system = GetSystem(codeItem.Scheme);
            var component = new Observation.ComponentComponent
            {
                Code = new CodeableConcept(system, codeItem.Value, codeItem.Meaning),
            };
            int value = report.Get(codeItem, 0);
            if (value != 0)
            {
                component.Value = new Integer(value);
                observation.Component.Add(component);
            }
        }

        private static string GetSystem(string scheme)
        {
            return scheme switch
            {
                ObservationConstants.Dcm => ObservationConstants.DcmSystem,
                ObservationConstants.Sct => ObservationConstants.SctSystem,
                _ => scheme
            };
        }
    }
}
