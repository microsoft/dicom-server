// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Dicom;
using Dicom.IO;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Core.Configs;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Indexing;
using Microsoft.Health.Dicom.Core.Features.Validation;
using Microsoft.Health.Dicom.Tests.Common;
using Microsoft.Health.Dicom.Tests.Common.Extensions;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Indexing
{
    public class ReindexDatasetValidatorTests
    {
        private readonly IElementMinimumValidator _minimumValidator;
        private readonly IExtendedQueryTagErrorStore _extendedQueryTagErrorStore;
        private readonly IReindexDatasetValidator _validator;

        public ReindexDatasetValidatorTests()
        {
            _extendedQueryTagErrorStore = Substitute.For<IExtendedQueryTagErrorStore>();
            _minimumValidator = new ElementMinimumValidator();
            IOptions<StoreConfiguration> options = Options.Create(new StoreConfiguration());
            _validator = new ReindexDatasetValidator(_minimumValidator, _extendedQueryTagErrorStore, options, NullLogger<ReindexDatasetValidator>.Instance);
        }

        [Fact]
        public void GivenReindexDatasetValidator_WhenValidatingInstanceFails_ThenErrorShouldBeRecorded()
        {
            DicomDataset dataset = new DicomDataset();
            DicomTag tag1 = DicomTag.StudyInstanceUID;
            DicomTag tag2 = DicomTag.SeriesInstanceUID;
            dataset.Add(new DicomUniqueIdentifier(tag1, "1.1"));
            dataset.Add(new DicomUniqueIdentifier(tag2, "1.2"));

            int tagKey1 = 3;
            int tagKey2 = 4;
            long watermark = 1;

            // Use a wrong VR
            QueryTag queryTag1 = new QueryTag(tag1.BuildExtendedQueryTagStoreEntry(key: tagKey1, level: QueryTagLevel.Study, vr: DicomVRCode.AE));
            QueryTag queryTag2 = new QueryTag(tag2.BuildExtendedQueryTagStoreEntry(key: tagKey2, level: QueryTagLevel.Series, vr: DicomVRCode.AE));

            _validator.Validate(
                dataset,
                watermark,
                new[] { queryTag1, queryTag2 });

            _extendedQueryTagErrorStore.Received(1).AddExtendedQueryTagErrorAsync(
                tagKey1,
                Arg.Any<string>(),
                watermark);

            _extendedQueryTagErrorStore.Received(1).AddExtendedQueryTagErrorAsync(
                tagKey2,
                Arg.Any<string>(),
                watermark);
        }


        [Fact]
        public void GivenMismatchVR_WhenRecordErrorMessage_ThenShouldNotTruncate()
        {
            DicomDataset dataset = Samples.CreateRandomInstanceDataset();
#pragma warning disable CS0618 // Type or member is obsolete
            dataset.AutoValidate = false;
#pragma warning restore CS0618 // Type or member is obsolete
            DicomTag tag = DicomTag.DeviceSerialNumber;
            QueryTag queryTag = new QueryTag(tag.BuildExtendedQueryTagStoreEntry(level: QueryTagLevel.Study, vr: DicomVR.PN.Code));
            dataset.Add(tag, "SN");
            _validator.Validate(dataset, 1, new[] { queryTag });
        }


        [Theory]
        [MemberData(nameof(GetElementForValidation))]
        public void GivenValidationFailure_WhenRecordErrorMessage_ThenShouldNotTruncate(DicomElement element)
        {
            DicomDataset dataset = new DicomDataset();
#pragma warning disable CS0618 // Type or member is obsolete
            dataset.AutoValidate = false;
#pragma warning restore CS0618 // Type or member is obsolete
            DicomTag tag = element.Tag;
            QueryTag queryTag = new QueryTag(tag.BuildExtendedQueryTagStoreEntry(level: QueryTagLevel.Study, vr: element.ValueRepresentation.Code));
            dataset.Add(element);
            _validator.Validate(dataset, 1, new[] { queryTag });
        }

        public static IEnumerable<object[]> GetElementForValidation()
        {
            // Has multiple value
            yield return new object[] { new DicomPersonName(DicomTag.DeviceSerialNumber, "p1", "p2") };

            // AE: value is too long (>16)
            yield return new object[] { new DicomApplicationEntity(DicomTag.DeviceSerialNumber, "012345678901234567") };

            // AS: value is not required length (4)
            yield return new object[] { new DicomAgeString(DicomTag.DeviceSerialNumber, "012345") };

            // CS: value is too long (>16)
            yield return new object[] { new DicomCodeString(DicomTag.DeviceSerialNumber, "012345678901234567") };

            // DA: value is invalid date
            yield return new object[] { new DicomDate(DicomTag.DeviceSerialNumber, "012345678") };

            // DS: value is too long (>16)
            yield return new object[] { new DicomDecimalString(DicomTag.DeviceSerialNumber, "01234567890123456") };

            // FL: value is not required length (4)
            yield return new object[] { new DicomFloatingPointSingle(DicomTag.DeviceSerialNumber, ByteConverter.ToByteBuffer(new[] { 1.0 })) };

            // FD: value is not required length (8)
            yield return new object[] { new DicomFloatingPointDouble(DicomTag.DeviceSerialNumber, ByteConverter.ToByteBuffer(new[] { 1.0f })) };

            // IS: value is too long (>12)
            yield return new object[] { new DicomIntegerString(DicomTag.DeviceSerialNumber, "0123456789012") };

            // LO: value is too long (>64)
            yield return new object[] { new DicomLongString(DicomTag.DeviceSerialNumber, "1234567890123456789012345678901234567890123456789012345678901234") };

            // LO: value has invalid char
            yield return new object[] { new DicomLongString(DicomTag.DeviceSerialNumber, "\\123456789012345678901234567890123456789012345678901234567890123") };

            // PN: value has too many groups
            yield return new object[] { new DicomPersonName(DicomTag.DeviceSerialNumber, "a=b=c=d") };

            // PN: value has invalid char
            yield return new object[] { new DicomPersonName(DicomTag.DeviceSerialNumber, "\nb") };

            // PN: group is too long
            yield return new object[] { new DicomPersonName(DicomTag.DeviceSerialNumber, "1234567890123456789012345678901234567890123456789012345678901234=b=c=d") };

            // PN: group too many components
            yield return new object[] { new DicomPersonName(DicomTag.DeviceSerialNumber, "a1^a2^a3^a4^a5^a6=b=c") };

            // SH: value is too long (>16)
            yield return new object[] { new DicomShortString(DicomTag.DeviceSerialNumber, "01234567890123456") };

            // SL: value is not required length(4)
            yield return new object[] { new DicomSignedLong(DicomTag.DeviceSerialNumber, ByteConverter.ToByteBuffer(new long[] { 1 })) };

            // SS: value is not required length(2)
            yield return new object[] { new DicomSignedShort(DicomTag.DeviceSerialNumber, ByteConverter.ToByteBuffer(new long[] { 1 })) };

            // UI: value is empty
            yield return new object[] { new DicomUniqueIdentifier(DicomTag.DeviceSerialNumber, "") };
            // UI: value is too long (>64)
            yield return new object[] { new DicomUniqueIdentifier(DicomTag.DeviceSerialNumber, "12345678901234567890123456789012345678901234567890123456789012345") };
            // UI: value has invalid char
            yield return new object[] { new DicomUniqueIdentifier(DicomTag.DeviceSerialNumber, "c2345678901234567890123456789012345678901234567890123456789012345") };
            // UI: value start with 0
            yield return new object[] { new DicomUniqueIdentifier(DicomTag.DeviceSerialNumber, "0123456789012345678901234567890123456789012345678901234567890123") };

            // UL: value is not required length(4)
            yield return new object[] { new DicomUnsignedLong(DicomTag.DeviceSerialNumber, ByteConverter.ToByteBuffer(new long[] { 1 })) };

            // US: value is not required length(2)
            yield return new object[] { new DicomUnsignedShort(DicomTag.DeviceSerialNumber, ByteConverter.ToByteBuffer(new long[] { 1 })) };
        }
    }
}
