// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Dicom;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.Persistence;
using Microsoft.Health.Dicom.Core.Features.Validation;

namespace Microsoft.Health.Dicom.Metadata.Features.Storage.Models
{
    internal static class MetadataExtensions
    {
        public static void AddDicomInstance(this DicomStudyMetadata dicomStudyMetadata, DicomDataset instance, IEnumerable<DicomAttributeId> indexableAttributes)
        {
            EnsureArg.IsNotNull(instance, nameof(instance));
            EnsureArg.IsNotNull(indexableAttributes, nameof(indexableAttributes));

            // If this series instance does not exist, we need to create a new dictionary entry.
            var identity = DicomInstance.Create(instance);
            if (!dicomStudyMetadata.SeriesMetadata.TryGetValue(identity.SeriesInstanceUID, out DicomSeriesMetadata seriesMetadata))
            {
                seriesMetadata = new DicomSeriesMetadata(currentInstanceId: 1, new Dictionary<string, int>(), new HashSet<AttributeValue>());
                dicomStudyMetadata.SeriesMetadata[identity.SeriesInstanceUID] = seriesMetadata;
            }

            AddInstance(seriesMetadata, instance, indexableAttributes);
        }

        public static IEnumerable<DicomInstance> GetDicomInstances(this DicomStudyMetadata dicomStudyMetadata)
        {
            EnsureArg.IsNotNull(dicomStudyMetadata, nameof(dicomStudyMetadata));

            string studyInstanceUID = dicomStudyMetadata.StudyInstanceUID;
            foreach (KeyValuePair<string, DicomSeriesMetadata> series in dicomStudyMetadata.SeriesMetadata)
            {
                foreach (KeyValuePair<string, int> instance in series.Value.Instances)
                {
                    yield return new DicomInstance(studyInstanceUID, series.Key, instance.Key);
                }
            }
        }

        public static IEnumerable<DicomInstance> GetDicomInstances(this DicomStudyMetadata dicomStudyMetadata, string seriesInstanceUID)
        {
            EnsureArg.IsNotNull(dicomStudyMetadata, nameof(dicomStudyMetadata));

            string studyInstanceUID = dicomStudyMetadata.StudyInstanceUID;
            if (dicomStudyMetadata.SeriesMetadata.TryGetValue(seriesInstanceUID, out DicomSeriesMetadata seriesMetadata))
            {
                return seriesMetadata.Instances.Select(x => new DicomInstance(studyInstanceUID, seriesInstanceUID, x.Key));
            }

            throw new ArgumentException($"The provided series instance identifier does not exist: {seriesInstanceUID}", nameof(seriesInstanceUID));
        }

        public static bool TryRemoveInstance(this DicomStudyMetadata dicomStudyMetadata, DicomInstance dicomInstance)
        {
            EnsureArg.IsNotNull(dicomStudyMetadata, nameof(dicomStudyMetadata));
            EnsureArg.IsEqualTo(dicomInstance.StudyInstanceUID, dicomStudyMetadata.StudyInstanceUID, nameof(dicomInstance));

            // Attempt to find the series and the instance (return false if it does not exist)
            if (!dicomStudyMetadata.SeriesMetadata.TryGetValue(dicomInstance.SeriesInstanceUID, out DicomSeriesMetadata dicomSeriesMetadata) ||
                !dicomSeriesMetadata.Instances.TryGetValue(dicomInstance.SopInstanceUID, out int instanceId))
            {
                return false;
            }

            var attributesToRemove = new List<AttributeValue>();
            foreach (AttributeValue attributeValue in dicomSeriesMetadata.AttributeValues)
            {
                if (attributeValue.Instances.Remove(instanceId) && attributeValue.Instances.Count == 0)
                {
                    attributesToRemove.Add(attributeValue);
                }
            }

            attributesToRemove.Each(x => dicomSeriesMetadata.AttributeValues.Remove(x));
            dicomSeriesMetadata.Instances.Remove(dicomInstance.SopInstanceUID);

            if (dicomSeriesMetadata.Instances.Count == 0)
            {
                dicomStudyMetadata.SeriesMetadata.Remove(dicomInstance.SeriesInstanceUID);
            }

            return true;
        }

        private static void AddInstance(DicomSeriesMetadata dicomSeriesMetadata, DicomDataset dicomDataset, IEnumerable<DicomAttributeId> indexableAttributes)
        {
            EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));

            var sopInstanceUID = dicomDataset.GetSingleValueOrDefault(DicomTag.SOPInstanceUID, string.Empty);
            EnsureArg.IsTrue(DicomIdentifierValidator.IdentifierRegex.IsMatch(sopInstanceUID));

            if (!dicomSeriesMetadata.Instances.TryGetValue(sopInstanceUID, out int instanceId))
            {
                instanceId = dicomSeriesMetadata.CurrentInstanceId++;
                dicomSeriesMetadata.Instances.Add(sopInstanceUID, instanceId);
            }

            HashSet<AttributeValue> attributeValues = dicomSeriesMetadata.AttributeValues;
            foreach (DicomAttributeId attribute in indexableAttributes)
            {
                if (dicomDataset.TryGetDicomItems(attribute, out DicomItem[] dicomItems))
                {
                    foreach (DicomItem dicomItem in dicomItems)
                    {
                        var attributeValue = AttributeValue.Create(dicomItem, instanceId);

                        if (attributeValues.TryGetValue(attributeValue, out AttributeValue actualValue))
                        {
                            actualValue.Instances.Add(instanceId);
                        }
                        else
                        {
                            attributeValues.Add(attributeValue);
                        }
                    }
                }
            }
        }
    }
}
