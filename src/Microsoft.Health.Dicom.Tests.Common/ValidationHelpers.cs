// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using EnsureThat;
using FellowOakDicom;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Query;
using Microsoft.Health.Dicom.Tests.Common.Serialization;
using Xunit;

namespace Microsoft.Health.Dicom.Tests.Common
{
    public static class ValidationHelpers
    {
        public static void ValidateReferencedSopSequence(DicomDataset actualDicomDataset, params (string SopInstanceUid, string RetrieveUri, string SopClassUid)[] expectedValues)
        {
            EnsureArg.IsNotNull(actualDicomDataset, nameof(actualDicomDataset));
            EnsureArg.IsNotNull(expectedValues, nameof(expectedValues));
            Assert.True(actualDicomDataset.TryGetSequence(DicomTag.ReferencedSOPSequence, out DicomSequence sequence));
            Assert.Equal(expectedValues.Length, sequence.Count());

            for (int i = 0; i < expectedValues.Length; i++)
            {
                DicomDataset actual = sequence.ElementAt(i);

                Assert.Equal(expectedValues[i].SopInstanceUid, actual.GetSingleValueOrDefault<string>(DicomTag.ReferencedSOPInstanceUID));
                Assert.Equal(expectedValues[i].RetrieveUri, actual.GetSingleValueOrDefault<string>(DicomTag.RetrieveURL));
                Assert.Equal(expectedValues[i].SopClassUid, actual.GetSingleValueOrDefault<string>(DicomTag.ReferencedSOPClassUID));
            }
        }

        public static void ValidateFailedSopSequence(DicomDataset actualDicomDataset, params (string SopInstanceUid, string SopClassUid, ushort FailureReason)[] expectedValues)
        {
            EnsureArg.IsNotNull(actualDicomDataset, nameof(actualDicomDataset));
            EnsureArg.IsNotNull(expectedValues, nameof(expectedValues));
            Assert.True(actualDicomDataset.TryGetSequence(DicomTag.FailedSOPSequence, out DicomSequence sequence));
            Assert.Equal(expectedValues.Length, sequence.Count());

            for (int i = 0; i < expectedValues.Length; i++)
            {
                DicomDataset actual = sequence.ElementAt(i);

                ValidateNullOrCorrectValue(expectedValues[i].SopInstanceUid, actual, DicomTag.ReferencedSOPInstanceUID);
                ValidateNullOrCorrectValue(expectedValues[i].SopClassUid, actual, DicomTag.ReferencedSOPClassUID);

                Assert.Equal(expectedValues[i].FailureReason, actual.GetSingleValueOrDefault<ushort>(DicomTag.FailureReason));
            }

            void ValidateNullOrCorrectValue(string expectedValue, DicomDataset actual, DicomTag dicomTag)
            {
                if (expectedValue == null)
                {
                    Assert.False(actual.TryGetSingleValue(dicomTag, out string _));
                }
                else
                {
                    Assert.Equal(expectedValue, actual.GetSingleValueOrDefault<string>(dicomTag));
                }
            }
        }

        public static void ValidateResponseDataset(
            QueryResource resource,
            DicomDataset storedInstance,
            DicomDataset responseInstance)
        {
            DicomDataset expectedDataset = storedInstance.Clone();
            HashSet<DicomTag> levelTags = new HashSet<DicomTag>();
            switch (resource)
            {
                case QueryResource.AllStudies:
                    levelTags.Add(DicomTag.StudyInstanceUID);
                    levelTags.Add(DicomTag.PatientID);
                    levelTags.Add(DicomTag.PatientName);
                    levelTags.Add(DicomTag.StudyDate);
                    break;
                case QueryResource.AllSeries:
                    levelTags.Add(DicomTag.StudyInstanceUID);
                    levelTags.Add(DicomTag.PatientID);
                    levelTags.Add(DicomTag.PatientName);
                    levelTags.Add(DicomTag.StudyDate);
                    levelTags.Add(DicomTag.SeriesInstanceUID);
                    levelTags.Add(DicomTag.Modality);
                    break;
                case QueryResource.AllInstances:
                    levelTags.Add(DicomTag.StudyInstanceUID);
                    levelTags.Add(DicomTag.PatientID);
                    levelTags.Add(DicomTag.PatientName);
                    levelTags.Add(DicomTag.StudyDate);
                    levelTags.Add(DicomTag.SeriesInstanceUID);
                    levelTags.Add(DicomTag.Modality);
                    levelTags.Add(DicomTag.SOPInstanceUID);
                    levelTags.Add(DicomTag.SOPClassUID);
                    levelTags.Add(DicomTag.BitsAllocated);
                    break;
                case QueryResource.StudySeries:
                    levelTags.Add(DicomTag.StudyInstanceUID);
                    levelTags.Add(DicomTag.SeriesInstanceUID);
                    levelTags.Add(DicomTag.Modality);
                    break;
                case QueryResource.StudyInstances:
                    levelTags.Add(DicomTag.StudyInstanceUID);
                    levelTags.Add(DicomTag.SeriesInstanceUID);
                    levelTags.Add(DicomTag.Modality);
                    levelTags.Add(DicomTag.SOPInstanceUID);
                    levelTags.Add(DicomTag.SOPClassUID);
                    levelTags.Add(DicomTag.BitsAllocated);
                    break;
                case QueryResource.StudySeriesInstances:
                    levelTags.Add(DicomTag.StudyInstanceUID);
                    levelTags.Add(DicomTag.SeriesInstanceUID);
                    levelTags.Add(DicomTag.SOPInstanceUID);
                    levelTags.Add(DicomTag.SOPClassUID);
                    levelTags.Add(DicomTag.BitsAllocated);
                    break;
            }

            expectedDataset.Remove((di) =>
            {
                return !levelTags.Contains(di.Tag);
            });

            // Compare result datasets by serializing.
            Assert.Equal(
                JsonSerializer.Serialize(expectedDataset, AppSerializerOptions.Json),
                JsonSerializer.Serialize(responseInstance, AppSerializerOptions.Json));
            Assert.Equal(expectedDataset.Count(), responseInstance.Count());
        }
    }
}
