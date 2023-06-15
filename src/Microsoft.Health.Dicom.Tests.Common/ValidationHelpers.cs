// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using EnsureThat;
using FellowOakDicom;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Query;
using Microsoft.Health.Dicom.Core.Features.Query.Model;
using Microsoft.Health.Dicom.Tests.Common.Serialization;
using Xunit;

namespace Microsoft.Health.Dicom.Tests.Common;

public static class ValidationHelpers
{
    public const ushort ValidationFailedFailureCode = 43264;
    public const ushort SopInstanceAlreadyExistsFailureCode = 45070;

    public static void ValidateReferencedSopSequence(
        DicomDataset actualDicomDataset,
        params (
            string SopInstanceUid,
            string RetrieveUri,
            string SopClassUid)[] expectedValues)
    {
        EnsureArg.IsNotNull(actualDicomDataset, nameof(actualDicomDataset));
        EnsureArg.IsNotNull(expectedValues, nameof(expectedValues));
        Assert.True(actualDicomDataset.TryGetSequence(DicomTag.ReferencedSOPSequence, out DicomSequence sequence));
        Assert.Equal(expectedValues.Length, sequence.Count());

        for (int i = 0; i < expectedValues.Length; i++)
        {
            DicomDataset actual = sequence.ElementAt(i);

            Assert.Equal(expectedValues[i].SopInstanceUid, actual.GetFirstValueOrDefault<string>(DicomTag.ReferencedSOPInstanceUID));
            Assert.Equal(expectedValues[i].RetrieveUri, actual.GetFirstValueOrDefault<string>(DicomTag.RetrieveURL));
            Assert.Equal(expectedValues[i].SopClassUid, actual.GetFirstValueOrDefault<string>(DicomTag.ReferencedSOPClassUID));
        }
    }

    public static void ValidateFailedSopSequence(
        DicomDataset actualDicomDataset,
        params (string SopInstanceUid, string SopClassUid, ushort FailureReason)[] expectedValues)
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

            Assert.Equal(expectedValues[i].FailureReason, actual.GetFirstValueOrDefault<ushort>(DicomTag.FailureReason));

            if (expectedValues[i].FailureReason == ValidationFailedFailureCode)
            {
                Assert.True(actual.TryGetSequence(DicomTag.FailedAttributesSequence, out DicomSequence failedAttributeSequence));
                Assert.True(failedAttributeSequence.Any());

                DicomDataset failedAttribute = failedAttributeSequence.ElementAt(0);
                Assert.NotEmpty(failedAttribute.GetFirstValueOrDefault<string>(DicomTag.ErrorComment));
            }
        }

        void ValidateNullOrCorrectValue(string expectedValue, DicomDataset actual, DicomTag dicomTag)
        {
            if (expectedValue == null)
            {
                Assert.False(actual.TryGetSingleValue(dicomTag, out string _));
            }
            else
            {
                Assert.Equal(expectedValue, actual.GetFirstValueOrDefault<string>(dicomTag));
            }
        }
    }

    public static void ValidateResponseDatasetV2(
        QueryResource resource,
        DicomDataset storedInstance,
        DicomDataset responseInstance)
    {
        DicomDataset expectedDataset = storedInstance.Clone();
        IReadOnlyCollection<DicomTag> returnTags = GetExpectedReturnTags(resource, expectedDataset);
        expectedDataset.Remove((di) => !returnTags.Contains(di.Tag));

        // Compare result datasets by serializing.
        Assert.Equal(
            JsonSerializer.Serialize(expectedDataset, AppSerializerOptions.Json),
            JsonSerializer.Serialize(responseInstance, AppSerializerOptions.Json));
        Assert.Equal(expectedDataset.ToString(), responseInstance.ToString());
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

    private static IReadOnlyCollection<DicomTag> GetExpectedReturnTags(QueryResource resource, DicomDataset expectedDataset)
    {
        QueryExpression expression = new QueryExpression(
            resource,
            new QueryIncludeField(new List<DicomTag>()),
            false,
            0,
            0,
            GetQueryFilterConditions(resource, expectedDataset),
            Array.Empty<string>());
        QueryResponseBuilder queryResponseBuilder = new QueryResponseBuilder(expression, useNewDefaults: true);
        IReadOnlyCollection<DicomTag> returnTags = queryResponseBuilder.ReturnTags;
        return returnTags;
    }

    private static IReadOnlyCollection<QueryFilterCondition> GetQueryFilterConditions(
        QueryResource resource,
        DicomDataset expectedDataset)
    {
        // Since filter conditions add to the tags returned, we also have to generate filters based on type of request
        // we are making so we can programmatically verify expected vs response dataset
        var filters = new Dictionary<DicomTag, QueryFilterCondition>() { };
        switch (resource)
        {
            case QueryResource.AllStudies:
                QueryParser.AddInstanceUidFilter(expectedDataset.GetString(DicomTag.StudyInstanceUID), filters);
                break;
            case QueryResource.AllSeries:
                QueryParser.AddSeriesUidFilter(expectedDataset.GetString(DicomTag.SeriesInstanceUID), filters);
                break;
            case QueryResource.AllInstances:
                QueryParser.AddInstanceUidFilter(expectedDataset.GetString(DicomTag.StudyInstanceUID), filters);
                break;
            case QueryResource.StudySeries:
                QueryParser.AddInstanceUidFilter(expectedDataset.GetString(DicomTag.StudyInstanceUID), filters);
                break;
            case QueryResource.StudyInstances:
                QueryParser.AddInstanceUidFilter(expectedDataset.GetString(DicomTag.StudyInstanceUID), filters);
                break;
            case QueryResource.StudySeriesInstances:
                QueryParser.AddInstanceUidFilter(expectedDataset.GetString(DicomTag.StudyInstanceUID), filters);
                QueryParser.AddSeriesUidFilter(expectedDataset.GetString(DicomTag.SeriesInstanceUID), filters);
                break;
        }

        return filters.Values;
    }
}
