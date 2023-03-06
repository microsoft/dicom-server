// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using FellowOakDicom;
using FellowOakDicom.Imaging;
using FellowOakDicom.IO.Buffer;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Features.Partition;
using Microsoft.Health.Dicom.Core.Features.Query;
using Microsoft.Health.Dicom.Core.Features.Query.Model;
using Microsoft.Health.Dicom.Core.Features.Retrieve;
using Microsoft.Health.Dicom.Core.Serialization;
using Microsoft.Health.FellowOakDicom.Serialization;

namespace Microsoft.Health.Dicom.Functions.Update;

public partial class UpdateDurableFunction
{
    internal static readonly JsonSerializerOptions JsonSerializerOptions = CreateJsonSerializerOptions();

    [FunctionName(nameof(GetInstancesAsync))]
    public async Task<QueryResult> GetInstancesAsync(
        [ActivityTrigger] IDurableActivityContext context,
        ILogger logger)
    {
        EnsureArg.IsNotNull(context, nameof(context));
        EnsureArg.IsNotNull(logger, nameof(logger));

        var includeField = QueryIncludeField.AllFields;
        var filters = new List<QueryFilterCondition>()
        {
            new StringSingleValueMatchCondition(new QueryTag(DicomTag.StudyInstanceUID), "1.2.3.4.3"),
        };
        var query = new QueryExpression(QueryResource.AllInstances, includeField, false, 100, 0, filters, Array.Empty<string>());

        var queryResult = await _queryStore.QueryAsync(PartitionEntry.Default.PartitionKey, query);
        return queryResult;
    }

    [FunctionName(nameof(UpdateInstanceBlobAsync))]
    public async Task UpdateInstanceBlobAsync(
        [ActivityTrigger] UpdateInstanceArgument arg,
        ILogger logger)
    {
        EnsureArg.IsNotNull(arg, nameof(arg));
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(arg.Dataset, nameof(arg.Dataset));

        logger.LogInformation("UpdateInstanceBlobAsync.");

        var includeField = QueryIncludeField.AllFields;
        var filters = new List<QueryFilterCondition>()
        {
            new StringSingleValueMatchCondition(new QueryTag(DicomTag.StudyInstanceUID), "1.2.3.4.3"),
        };
        var query = new QueryExpression(QueryResource.AllInstances, includeField, false, 100, 0, filters, Array.Empty<string>());

        var queryResult = await _queryStore.QueryAsync(PartitionEntry.Default.PartitionKey, query);

        if (queryResult.DicomInstances.Any())
        {
            var updateDateset = new DicomDataset();
            int partitionKey = PartitionEntry.Default.PartitionKey;

            DicomDataset datasetToUpdate = JsonSerializer.Deserialize<DicomDataset>(arg.Dataset, JsonSerializerOptions);

            foreach (var instanceIdentifier in queryResult.DicomInstances)
            {
                // Get Next watermark
                long nextWatermark = await _indexStore.GetInstanceNextWatermark(instanceIdentifier);

                Stream stream = await _fileStore.GetFileAsync(instanceIdentifier);
                stream.Seek(0, SeekOrigin.Begin);
                DicomFile dicomFile;
                try
                {
                    dicomFile = await DicomFile.OpenAsync(stream, FileReadOption.ReadLargeOnDemand);
                }
                catch (DicomFileException)
                {
                    throw;
                }

                var withoutPixelData = dicomFile.Dataset.CopyWithoutPixelDataItems();
                var dicomDataset = dicomFile.Dataset;

                foreach (var item in datasetToUpdate)
                {
                    dicomDataset.AddOrUpdate(item);
                }

                updateDateset.AddOrUpdate(DicomTag.StudyInstanceUID, instanceIdentifier.StudyInstanceUid);
                updateDateset.AddOrUpdate(DicomTag.PatientID, dicomDataset.GetSingleValue<string>(DicomTag.PatientID));
                updateDateset.AddOrUpdate(DicomTag.PatientName, dicomDataset.GetSingleValue<string>(DicomTag.PatientName));
                updateDateset.AddOrUpdate(DicomTag.ReferringPhysicianName, dicomDataset.GetSingleValue<string>(DicomTag.ReferringPhysicianName));
                updateDateset.AddOrUpdate(DicomTag.StudyDate, dicomDataset.GetSingleValue<DateTime>(DicomTag.StudyDate));
                updateDateset.AddOrUpdate(DicomTag.StudyDescription, dicomDataset.GetSingleValue<string>(DicomTag.StudyDescription));
                updateDateset.AddOrUpdate(DicomTag.AccessionNumber, dicomDataset.GetSingleValue<string>(DicomTag.AccessionNumber));

                using (MemoryStream memStream = new MemoryStream())
                {
                    DicomFile file = new DicomFile(dicomDataset);
                    await file.SaveAsync(memStream);
                    var newInstanceIdentifier = new VersionedInstanceIdentifier(
                        instanceIdentifier.SopInstanceUid,
                        instanceIdentifier.SeriesInstanceUid,
                        instanceIdentifier.StudyInstanceUid,
                        nextWatermark,
                        instanceIdentifier.PartitionKey,
                        instanceIdentifier.Revision
                    );

                    await _fileStore.StoreFileAsync(newInstanceIdentifier, memStream);
                    await _metadataStore.StoreInstanceMetadataAsync(dicomDataset, nextWatermark);
                    await StoreFileFramesRangeAsync(dicomDataset, nextWatermark, CancellationToken.None);
                }

                // Store old file without pixel data
                using (MemoryStream memStream1 = new MemoryStream())
                {
                    DicomFile file = new DicomFile(withoutPixelData);
                    await file.SaveAsync(memStream1);
                    await _fileStore.StoreFileAsync(instanceIdentifier, memStream1);
                }

                // Create new instance row
                await _indexStore.CreateInstanceRevision(instanceIdentifier, nextWatermark);

                // Delete old dcm file to reduce storage.
                await _fileStore.DeleteFileIfExistsAsync(instanceIdentifier);
            }


            // Update Study
            await _indexStore.UpdateStudyAsync(partitionKey, updateDateset);
        }
    }

    private async Task<bool> StoreFileFramesRangeAsync(
            DicomDataset dicomDataset,
            long version,
            CancellationToken cancellationToken)
    {
        bool hasFrameMetadata = false;
        Dictionary<int, FrameRange> framesRange = GetFramesOffset(dicomDataset);

        if (framesRange != null && framesRange.Count > 0)
        {
            var identifier = dicomDataset.ToVersionedInstanceIdentifier(version);

            await _metadataStore.StoreInstanceFramesRangeAsync(identifier, framesRange, cancellationToken);
            hasFrameMetadata = true;
        }
        return hasFrameMetadata;
    }

    internal static Dictionary<int, FrameRange> GetFramesOffset(DicomDataset dataset)
    {
        if (!dataset.TryGetPixelData(out DicomPixelData dicomPixel))
        {
            return null;
        }

        if (dicomPixel.NumberOfFrames < 1)
        {
            return null;
        }

        var pixelData = dataset.GetDicomItem<DicomItem>(DicomTag.PixelData);
        var framesRange = new Dictionary<int, FrameRange>();

        // todo support case where fragments != frames.
        // This means offsettable matches the frames and we have to parse the bytes in pixelData to find the right fragment and end at the right fragment.
        // there is also a 8 byte tag inbetween the fragment data that we need to handlee.
        // fo-dicom/DicomPixelData.cs/GetFrame has the logic
        if (pixelData is DicomFragmentSequence pixelDataFragment
            && pixelDataFragment.Fragments.Count == dicomPixel.NumberOfFrames)
        {
            for (int i = 0; i < pixelDataFragment.Fragments.Count; i++)
            {
                var fragment = pixelDataFragment.Fragments[i];
                if (TryGetBufferPosition(fragment, out long position, out long size))
                {
                    framesRange.Add(i, new FrameRange(position, size));
                }
            }
        }
        else if (pixelData is DicomOtherByte)
        {
            var dicomPixelOtherByte = dataset.GetDicomItem<DicomOtherByte>(DicomTag.PixelData);

            for (int i = 0; i < dicomPixel.NumberOfFrames; i++)
            {
                IByteBuffer byteBuffer = dicomPixel.GetFrame(i);
                if (TryGetBufferPosition(dicomPixelOtherByte.Buffer, out long position, out long size)
                    && byteBuffer is RangeByteBuffer rangeByteBuffer)
                {
                    framesRange.Add(i, new FrameRange(position + rangeByteBuffer.Offset, rangeByteBuffer.Length));
                }
            }
        }
        else if (pixelData is DicomOtherWord)
        {
            var dicomPixelWordByte = dataset.GetDicomItem<DicomOtherWord>(DicomTag.PixelData);

            for (int i = 0; i < dicomPixel.NumberOfFrames; i++)
            {
                IByteBuffer byteBuffer = dicomPixel.GetFrame(i);
                if (TryGetBufferPosition(dicomPixelWordByte.Buffer, out long position, out long size)
                    && byteBuffer is RangeByteBuffer rangeByteBuffer)
                {
                    framesRange.Add(i, new FrameRange(position + rangeByteBuffer.Offset, rangeByteBuffer.Length));
                }
            }
        }

        if (framesRange.Any())
        {
            return framesRange;
        }

        return null;
    }

    private static bool TryGetBufferPosition(IByteBuffer buffer, out long position, out long size)
    {
        bool result = false;
        position = 0;
        size = 0;
        if (buffer is StreamByteBuffer streamByteBuffer)
        {
            position = streamByteBuffer.Position;
            size = streamByteBuffer.Size;
            result = true;
        }
        else if (buffer is FileByteBuffer fileByteBuffer)
        {
            position = fileByteBuffer.Position;
            size = fileByteBuffer.Size;
            result = true;
        }
        return result;
    }

    private static JsonSerializerOptions CreateJsonSerializerOptions()
    {
        var options = new JsonSerializerOptions
        {
            AllowTrailingCommas = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
            Encoder = null,
            IgnoreReadOnlyFields = false,
            IgnoreReadOnlyProperties = false,
            IncludeFields = false,
            MaxDepth = 0, // 0 indicates the max depth of 64
            NumberHandling = JsonNumberHandling.Strict,
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            ReadCommentHandling = JsonCommentHandling.Skip,
            WriteIndented = false,
        };

        options.Converters.Add(new DicomIdentifierJsonConverter());
        options.Converters.Add(new DicomJsonConverter(writeTagsAsKeywords: true, autoValidate: false, numberSerializationMode: NumberSerializationMode.PreferablyAsNumber));
        options.Converters.Add(new ExportDataOptionsJsonConverter());
        options.Converters.Add(new JsonStringEnumConverter());

        return options;
    }
}
