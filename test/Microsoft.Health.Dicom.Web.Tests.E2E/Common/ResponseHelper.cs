// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Linq;
using System.Threading.Tasks;
using FellowOakDicom;
using Microsoft.Health.Dicom.Client;
using Microsoft.Health.Dicom.Core.Web;
using Microsoft.Health.Dicom.Tests.Common;
using Xunit;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Common;

public static class ResponseHelper
{
    internal static async Task<DicomDataset> GetMetadata(IDicomWebClient client, DicomFile dicomFile, string partition = null)
    {
        using DicomWebAsyncEnumerableResponse<DicomDataset> response =
            await client
                .RetrieveStudyMetadataAsync(
                    dicomFile.Dataset.GetString(DicomTag.StudyInstanceUID),
                    partitionName: partition
                );

        DicomDataset[] datasets = await response.ToArrayAsync();

        Assert.Single(datasets);

        DicomDataset retrievedMetadata = datasets[0];
        return retrievedMetadata;
    }

    internal static (string SopInstanceUid, string RetrieveUri, string SopClassUid) ConvertToReferencedSopSequenceEntry(
        IDicomWebClient client,
        DicomDataset dicomDataset,
        string partition = null)
    {
        string studyInstanceUid = dicomDataset.GetSingleValue<string>(DicomTag.StudyInstanceUID);
        string seriesInstanceUid = dicomDataset.GetSingleValue<string>(DicomTag.SeriesInstanceUID);
        string sopInstanceUid = dicomDataset.GetSingleValue<string>(DicomTag.SOPInstanceUID);

        string relativeUri = GetUrl(partition, studyInstanceUid, seriesInstanceUid, sopInstanceUid);

        return (dicomDataset.GetSingleValue<string>(DicomTag.SOPInstanceUID),
            new Uri(client.HttpClient.BaseAddress, relativeUri).ToString(),
            dicomDataset.GetSingleValue<string>(DicomTag.SOPClassUID));
    }

    private static string GetUrl(string partition, string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid)
    {
        if (partition != null)
        {
            return $"{DicomApiVersions.Latest}/partitions/{partition}/studies/{studyInstanceUid}/series/{seriesInstanceUid}/instances/{sopInstanceUid}";
        }
        else
        {
            return $"{DicomApiVersions.Latest}/studies/{studyInstanceUid}/series/{seriesInstanceUid}/instances/{sopInstanceUid}";
        }
    }

    internal static async Task ValidateReferencedSopSequenceAsync(DicomWebResponse<DicomDataset> response, params (string SopInstanceUid, string RetrieveUri, string SopClassUid)[] expectedValues)
    {
        Assert.Equal(KnownContentTypes.ApplicationDicomJson, response.ContentHeaders.ContentType.MediaType);
        ValidationHelpers.ValidateReferencedSopSequence(await response.GetValueAsync(), expectedValues);
    }

    internal static (string SopInstanceUid, string SopClassUid, ushort FailureReason) ConvertToFailedSopSequenceEntry(
        DicomDataset dicomDataset,
        ushort failureReason)
    {
        return (dicomDataset.GetSingleValue<string>(DicomTag.SOPInstanceUID),
            dicomDataset.GetSingleValue<string>(DicomTag.SOPClassUID),
            failureReason);
    }
}
