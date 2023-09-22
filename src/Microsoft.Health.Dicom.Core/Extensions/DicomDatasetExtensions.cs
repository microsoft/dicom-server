// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using EnsureThat;
using FellowOakDicom;
using FellowOakDicom.IO.Writer;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Features.Partitioning;
using Microsoft.Health.Dicom.Core.Features.Store;
using Microsoft.Health.Dicom.Core.Features.Validation;
using Microsoft.Health.Dicom.Core.Features.Workitem;
using Microsoft.Health.Dicom.Core.Features.Workitem.Model;
using Microsoft.Health.Dicom.Core.Models;

namespace Microsoft.Health.Dicom.Core.Extensions;

/// <summary>
/// Extension methods for <see cref="DicomDataset"/>.
/// </summary>
public static class DicomDatasetExtensions
{
    private static readonly HashSet<DicomVR> DicomBulkDataVr = new HashSet<DicomVR>
    {
        DicomVR.OB,
        DicomVR.OD,
        DicomVR.OF,
        DicomVR.OL,
        DicomVR.OV,
        DicomVR.OW,
        DicomVR.UN,
    };

    private const string DateFormatDA = "yyyyMMdd";

    private static readonly string[] DateTimeFormatsDT =
    {
        "yyyyMMddHHmmss.FFFFFFzzz",
        "yyyyMMddHHmmsszzz",
        "yyyyMMddHHmmzzz",
        "yyyyMMddHHzzz",
        "yyyyMMddzzz",
        "yyyyMMzzz",
        "yyyyzzz",
        "yyyyMMddHHmmss.FFFFFF",
        "yyyyMMddHHmmss",
        "yyyyMMddHHmm",
        "yyyyMMddHH",
        "yyyyMMdd",
        "yyyyMM",
        "yyyy"
    };

    private static readonly string[] DateTimeOffsetFormats = new string[]
    {
        "hhmm",
        "\\+hhmm",
        "\\-hhmm"
    };

    private static readonly HashSet<string> ByteArrayVRs = new HashSet<string>
    {
        "OB",
        "UN"
    };

    private static readonly HashSet<string> DecimalVRs = new HashSet<string> { "DS" };

    private static readonly HashSet<string> DoubleVRs = new HashSet<string> { "FD" };

    private static readonly HashSet<string> FloatVRs = new HashSet<string> { "FL" };

    private static readonly HashSet<string> IntVRs = new HashSet<string>
    {
        "IS",
        "SL"
    };

    private static readonly HashSet<string> LongVRs = new HashSet<string> { "SV" };

    private static readonly HashSet<string> ShortVRs = new HashSet<string> { "SS" };

    private static readonly HashSet<string> StringVRs = new HashSet<string>
    {
        "AE",
        "AS",
        "CS",
        "DA",
        "DT",
        "LO",
        "LT",
        "PN",
        "SH",
        "ST",
        "TM",
        "UC",
        "UI",
        "UR",
        "UT"
    };

    private static readonly HashSet<string> UIntVRs = new HashSet<string> { "UL" };

    private static readonly HashSet<string> ULongVRs = new HashSet<string> { "UV" };

    private static readonly HashSet<string> UShortVRs = new HashSet<string> { "US" };

    private static readonly HashSet<RequirementCode> MandatoryRequirementCodes = new HashSet<RequirementCode>
    {
        RequirementCode.OneOne,
        RequirementCode.TwoOne,
        RequirementCode.TwoTwo
    };

    private static readonly HashSet<RequirementCode> NonZeroLengthRequirementCodes = new HashSet<RequirementCode>
    {
        RequirementCode.OneOne,
        RequirementCode.ThreeOne,
        RequirementCode.ThreeTwo,
        RequirementCode.ThreeThree,
        RequirementCode.OneCOne,
        RequirementCode.OneCOneC,
        RequirementCode.OneCTwo
    };

    /// <summary>
    /// Gets a single value if the value exists; otherwise the default value for the type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="dicomDataset">The dataset to get the VR value from.</param>
    /// <param name="dicomTag">The DICOM tag.</param>
    /// <param name="expectedVR">Expected VR of the element.</param>
    /// <remarks>If expectedVR is provided, and not match, will return default<typeparamref name="T"/></remarks>
    /// <returns>The value if the value exists; otherwise, the default value for the type <typeparamref name="T"/>.</returns>
    public static T GetFirstValueOrDefault<T>(this DicomDataset dicomDataset, DicomTag dicomTag, DicomVR expectedVR = null)
    {
        EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));
        DicomElement element = dicomDataset.GetDicomItem<DicomElement>(dicomTag);
        if (element == null)
        {
            return default;
        }

        // If VR doesn't match, return default(T)
        if (expectedVR != null && element.ValueRepresentation != expectedVR)
        {
            return default;
        }

        return element.GetFirstValueOrDefault<T>();
    }

    /// <summary>
    /// Gets the DA VR value as <see cref="DateTime"/>.
    /// </summary>
    /// <param name="dicomDataset">The dataset to get the VR value from.</param>
    /// <param name="dicomTag">The DICOM tag.</param>
    /// <param name="expectedVR">Expected VR of the element.</param>
    /// <remarks>If expectedVR is provided, and not match, will return null.</remarks>
    /// <returns>An instance of <see cref="DateTime"/> if the value exists and comforms to the DA format; otherwise <c>null</c>.</returns>
    public static DateTime? GetStringDateAsDate(this DicomDataset dicomDataset, DicomTag dicomTag, DicomVR expectedVR = null)
    {
        EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));
        string stringDate = dicomDataset.GetFirstValueOrDefault<string>(dicomTag, expectedVR: expectedVR);
        return DateTime.TryParseExact(stringDate, DateFormatDA, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime result) ? result : null;
    }

    /// <summary>
    /// Gets the DT VR values as literal <see cref="DateTime"/> and UTC <see cref="DateTime"/>.
    /// If offset is not provided in the value and in the TimezoneOffsetFromUTC fields, UTC DateTime will be null.
    /// </summary>
    /// <param name="dicomDataset">The dataset to get the VR value from.</param>
    /// <param name="dicomTag">The DICOM tag.</param>
    /// <param name="expectedVR">Expected VR of the element.</param>
    /// <remarks>If expectedVR is provided, and not match, will return null.</remarks>
    /// <returns>A <see cref="Tuple{T1, T2}"/> of (<see cref="Nullable{T}"/> <see cref="DateTime"/>, <see cref="Nullable{T}"/> <see cref="DateTime"/>) representing literal date time and Utc date time respectively is returned. If value does not exist or does not conform to the DT format, <c>null</c> is returned for DateTimes. If offset information is not present, <c>null</c> is returned for Item2 i.e. Utc Date Time.</returns>
    public static Tuple<DateTime?, DateTime?> GetStringDateTimeAsLiteralAndUtcDateTimes(this DicomDataset dicomDataset, DicomTag dicomTag, DicomVR expectedVR = null)
    {
        EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));
        string stringDateTime = dicomDataset.GetFirstValueOrDefault<string>(dicomTag, expectedVR: expectedVR);

        if (string.IsNullOrEmpty(stringDateTime))
        {
            // If no valid data found, return null values in tuple.
            return new Tuple<DateTime?, DateTime?>(null, null);
        }

        Tuple<DateTime?, DateTime?> result = new Tuple<DateTime?, DateTime?>(null, null);

        // Parsing as DateTime such that we can know the DateTimeKind.
        // If offset is present in the value, DateTimeKind is Local, else it is Unspecified.
        // Ideally would like to work with just DateTimeOffsets to avoid parsing multiple times, but DateTimeKind does not work as expected with DateTimeOffset.
        if (DateTime.TryParseExact(stringDateTime, DateTimeFormatsDT, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dateTime))
        {
            // Using DateTimeStyles.AssumeUniversal here such that when applying offset, local timezone (offset) is not taken into account.
            DateTimeOffset.TryParseExact(stringDateTime, DateTimeFormatsDT, null, DateTimeStyles.AssumeUniversal, out DateTimeOffset dateTimeOffset);

            // Unspecified means that the offset is not present in the value.
            if (dateTime.Kind == DateTimeKind.Unspecified)
            {
                // Check if offset is present in TimezoneOffsetFromUTC
                TimeSpan? offset = dicomDataset.GetTimezoneOffsetFromUtcAsTimeSpan();

                if (offset != null)
                {
                    // If we can parse the offset, use that offset to calculate UTC Date Time.
                    result = new Tuple<DateTime?, DateTime?>(dateTimeOffset.DateTime, dateTimeOffset.ToOffset(offset.Value).DateTime);
                }
                else
                {
                    // If either offset is not present or could not be parsed, UTC should be null.
                    result = new Tuple<DateTime?, DateTime?>(dateTimeOffset.DateTime, null);
                }
            }
            else
            {
                // If offset is present in the value, it can simply be converted to UTC
                result = new Tuple<DateTime?, DateTime?>(dateTimeOffset.DateTime, dateTimeOffset.UtcDateTime);
            }
        }

        return result;
    }

    /// <summary>
    /// Get TimezoneOffsetFromUTC value as TimeSpan.
    /// </summary>
    /// <param name="dicomDataset">The dataset to get the TimezoneOffsetFromUTC value from.</param>
    /// <returns>An instance of <see cref="Nullable"/> <see cref="TimeSpan"/>. If value is not found or could not be parsed, <c>null</c> is returned.</returns>
    private static TimeSpan? GetTimezoneOffsetFromUtcAsTimeSpan(this DicomDataset dicomDataset)
    {
        // Cannot parse it directly as TimeSpan as the offset needs to follow specific formats.
        string offset = dicomDataset.GetFirstValueOrDefault<string>(DicomTag.TimezoneOffsetFromUTC, expectedVR: DicomVR.SH);

        if (!string.IsNullOrEmpty(offset))
        {
            TimeSpan timeSpan;

            // Need to look at offset string to figure out positive or negative offset
            // as timespan ParseExact does not support negative offsets by default.
            // Applying TimeSpanStyles.AssumeNegative is the only documented way to handle negative offsets for this method.
            bool isSuccess = offset[0] == '-' ?
                TimeSpan.TryParseExact(offset, DateTimeOffsetFormats, CultureInfo.InvariantCulture, TimeSpanStyles.AssumeNegative, out timeSpan) :
                TimeSpan.TryParseExact(offset, DateTimeOffsetFormats, CultureInfo.InvariantCulture, out timeSpan);

            if (isSuccess)
            {
                return timeSpan;
            }
        }

        return null;
    }

    /// <summary>
    /// Gets the TM VR value as <see cref="long"/>.
    /// </summary>
    /// <param name="dicomDataset">The dataset to get the VR value from.</param>
    /// <param name="dicomTag">The DICOM tag.</param>
    /// <param name="expectedVR">Expected VR of the element.</param>
    /// <remarks>If expectedVR is provided, and not match, will return null.</remarks>
    /// <returns>A long value representing the ticks if the value exists and conforms to the TM format; othewise <c>null</c>.</returns>
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Will reevaluate upon inspection of possible exceptions.")]
    public static long? GetStringTimeAsLong(this DicomDataset dicomDataset, DicomTag dicomTag, DicomVR expectedVR = null)
    {
        EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));
        long? result = null;

        try
        {
            result = dicomDataset.GetFirstValueOrDefault<DateTime>(dicomTag, expectedVR: expectedVR).Ticks;
        }
        catch (Exception)
        {
            result = null;
        }

        // If parsing fails for Time, a default value of 0 is returned. Since expected outcome is null, testing here and returning expected result.
        return result == 0 ? null : result;
    }

    /// <summary>
    /// Creates a new copy of DICOM dataset with items of VR types considered to be bulk data removed.
    /// </summary>
    /// <param name="dicomDataset">The DICOM dataset.</param>
    /// <returns>A copy of the <paramref name="dicomDataset"/> with items of VR types considered  to be bulk data removed.</returns>
    public static DicomDataset CopyWithoutBulkDataItems(this DicomDataset dicomDataset)
    {
        EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));

        return CopyDicomDatasetWithoutBulkDataItems(dicomDataset);

        static DicomDataset CopyDicomDatasetWithoutBulkDataItems(DicomDataset dicomDatasetToCopy)
        {
            return new DicomDataset(dicomDatasetToCopy
                .Select(dicomItem =>
                {
                    if (DicomBulkDataVr.Contains(dicomItem.ValueRepresentation))
                    {
                        // If the VR is bulk data type, return null so it can be filtered out later.
                        return null;
                    }
                    else if (dicomItem.ValueRepresentation == DicomVR.SQ)
                    {
                        // If the VR is sequence, then process each item within the sequence.
                        DicomSequence sequenceToCopy = (DicomSequence)dicomItem;

                        return new DicomSequence(
                            sequenceToCopy.Tag,
                            sequenceToCopy.Select(itemToCopy => itemToCopy.CopyWithoutBulkDataItems()).ToArray());
                    }
                    else
                    {
                        // The VR is not bulk data, return it.
                        return dicomItem;
                    }
                })
                .Where(dicomItem => dicomItem != null));
        }
    }

    /// <summary>
    /// Creates an instance of <see cref="InstanceIdentifier"/> from <see cref="DicomDataset"/>.
    /// </summary>
    /// <param name="dicomDataset">The DICOM dataset to get the identifiers from.</param>
    /// <param name="partition">Data Partition entry</param>
    /// <returns>An instance of <see cref="InstanceIdentifier"/> representing the <paramref name="dicomDataset"/>.</returns>
    public static InstanceIdentifier ToInstanceIdentifier(this DicomDataset dicomDataset, Partition partition)
    {
        EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));

        // Note: Here we 'GetSingleValueOrDefault' and let the constructor validate the identifier.
        return new InstanceIdentifier(
            dicomDataset.GetSingleValueOrDefault(DicomTag.StudyInstanceUID, string.Empty),
            dicomDataset.GetSingleValueOrDefault(DicomTag.SeriesInstanceUID, string.Empty),
            dicomDataset.GetSingleValueOrDefault(DicomTag.SOPInstanceUID, string.Empty),
            partition);
    }

    /// <summary>
    /// Creates an instance of <see cref="VersionedInstanceIdentifier"/> from <see cref="DicomDataset"/>.
    /// </summary>
    /// <param name="dicomDataset">The DICOM dataset to get the identifiers from.</param>
    /// <param name="version">The version.</param>
    /// <param name="partition">Data Partition entry</param>
    /// <returns>An instance of <see cref="InstanceIdentifier"/> representing the <paramref name="dicomDataset"/>.</returns>
    public static VersionedInstanceIdentifier ToVersionedInstanceIdentifier(this DicomDataset dicomDataset, long version, Partition partition)
    {
        EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));

        // Note: Here we 'GetSingleValueOrDefault' and let the constructor validate the identifier.
        return new VersionedInstanceIdentifier(
            dicomDataset.GetSingleValueOrDefault(DicomTag.StudyInstanceUID, string.Empty),
            dicomDataset.GetSingleValueOrDefault(DicomTag.SeriesInstanceUID, string.Empty),
            dicomDataset.GetSingleValueOrDefault(DicomTag.SOPInstanceUID, string.Empty),
            version,
            partition);
    }

    /// <summary>
    /// Adds value to the <paramref name="dicomDataset"/> if <paramref name="value"/> is not null.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="dicomDataset">The dataset to add value to.</param>
    /// <param name="dicomTag">The DICOM tag.</param>
    /// <param name="value">The value to add.</param>
    public static void AddValueIfNotNull<T>(this DicomDataset dicomDataset, DicomTag dicomTag, T value)
        where T : class
    {
        EnsureArg.IsNotNull(dicomDataset, nameof(dicomDataset));
        EnsureArg.IsNotNull(dicomTag, nameof(dicomTag));

        if (value != null)
        {
            dicomDataset.Add(dicomTag, value);
        }
    }

    /// <summary>
    /// Validate query tag in Dicom dataset.
    /// </summary>
    /// <param name="dataset">The dicom dataset.</param>
    /// <param name="queryTag">The query tag.</param>
    /// <param name="minimumValidator">The minimum validator.</param>
    public static ValidationWarnings ValidateQueryTag(this DicomDataset dataset, QueryTag queryTag, IElementMinimumValidator minimumValidator)
    {
        EnsureArg.IsNotNull(dataset, nameof(dataset));
        EnsureArg.IsNotNull(queryTag, nameof(queryTag));

        return dataset.ValidateDicomTag(queryTag.Tag, minimumValidator);
    }

    /// <summary>
    /// Validate dicom tag in Dicom dataset.
    /// </summary>
    /// <param name="dataset">The dicom dataset.</param>
    /// <param name="dicomTag">The dicom tag being validated.</param>
    /// <param name="minimumValidator">The minimum validator.</param>
    /// <param name="withLeniency">Whether or not to validate with additional leniency</param>
    public static ValidationWarnings ValidateDicomTag(this DicomDataset dataset, DicomTag dicomTag, IElementMinimumValidator minimumValidator, bool withLeniency = false)
    {
        EnsureArg.IsNotNull(dataset, nameof(dataset));
        EnsureArg.IsNotNull(dicomTag, nameof(dicomTag));
        EnsureArg.IsNotNull(minimumValidator, nameof(minimumValidator));
        DicomElement dicomElement = dataset.GetDicomItem<DicomElement>(dicomTag);

        ValidationWarnings warning = ValidationWarnings.None;
        if (dicomElement != null)
        {
            if (dicomElement.ValueRepresentation != dicomTag.GetDefaultVR())
            {
                string name = dicomElement.Tag.GetFriendlyName();
                DicomVR actualVR = dicomElement.ValueRepresentation;
                throw new ElementValidationException(
                    name,
                    actualVR,
                    ValidationErrorCode.UnexpectedVR,
                    string.Format(CultureInfo.InvariantCulture, DicomCoreResource.ErrorMessageUnexpectedVR, name, dicomTag.GetDefaultVR(), actualVR));
            }

            if (dicomElement.Count > 1)
            {
                warning |= ValidationWarnings.IndexedDicomTagHasMultipleValues;
            }

            minimumValidator.Validate(dicomElement, withLeniency);
        }
        return warning;
    }

    /// <summary>
    /// Gets DicomDatasets that matches a list of tags reprenting a tag path.
    /// </summary>
    /// <param name="dataset">The DicomDataset to be traversed.</param>
    /// <param name="dicomTags">The Dicom tags modelling the path.</param>
    /// <returns>Lists of DicomDataset that matches the list of tags.</returns>
    public static IEnumerable<DicomDataset> GetSequencePathValues(this DicomDataset dataset, ReadOnlyCollection<DicomTag> dicomTags)
    {
        EnsureArg.IsNotNull(dataset, nameof(dataset));
        EnsureArg.IsNotNull(dicomTags, nameof(dicomTags));

        if (dicomTags.Count != 2)
        {
            throw new ElementValidationException(string.Join(", ", dicomTags.Select(x => x.GetPath())), DicomVR.SQ, ValidationErrorCode.NestedSequence);
        }

        var foundSequence = dataset.GetSequence(dicomTags[0]);
        if (foundSequence != null)
        {
            foreach (var childDataset in foundSequence.Items)
            {
                var item = childDataset.GetDicomItem<DicomItem>(dicomTags[1]);

                if (item != null)
                {
                    yield return new DicomDataset(item);
                }
            }
        }
    }

    public static void ValidateAllRequirements(this DicomDataset dataset, IReadOnlyCollection<RequirementDetail> requirements)
    {
        if (requirements == null)
        {
            return;
        }

        foreach (RequirementDetail requirement in requirements)
        {
            dataset.ValidateRequirement(requirement.DicomTag, requirement.RequirementCode);

            // If no sequence requirements are present, move on to the next tag.
            if (requirement.SequenceRequirements == null)
            {
                continue;
            }

            // If current tag is not allowed, no action needed for its potential children.
            if (requirement.RequirementCode == RequirementCode.NotAllowed)
            {
                continue;
            }

            bool isMandatory = MandatoryRequirementCodes.Contains(requirement.RequirementCode);
            bool isNonZero = NonZeroLengthRequirementCodes.Contains(requirement.RequirementCode);
            bool hasChildren = dataset.Contains(requirement.DicomTag) && dataset.GetValueCount(requirement.DicomTag) > 0;

            // Validate sequence only if
            //  1. Parent is mandatory and is non-zero, means it has to have children. OR
            //  2. Parent contains children regardless of being mandatory or not.
            if ((isMandatory && isNonZero) ||
                hasChildren)
            {
                dataset.ValidateSequence(requirement.DicomTag, requirement.SequenceRequirements);
            }
        }
    }

    private static void ValidateSequence(this DicomDataset dataset, DicomTag sequenceTag, IReadOnlyCollection<RequirementDetail> requirements)
    {
        if (requirements == null || requirements.Count == 0 || !dataset.TryGetSequence(sequenceTag, out DicomSequence sequence) || sequence.Items.Count == 0)
        {
            return;
        }

        foreach (DicomDataset sequenceDataset in sequence.Items)
        {
            foreach (RequirementDetail requirement in requirements)
            {
                sequenceDataset.ValidateRequirement(requirement.DicomTag, requirement.RequirementCode);

                if (requirement.SequenceRequirements != null)
                {
                    sequenceDataset.ValidateSequence(requirement.DicomTag, requirement.SequenceRequirements);
                }
            }
        }
    }

    /// <summary>
    /// Validate whether a dataset meets the service class user (SCU) and service class provider (SCP) requirements for a given attribute.
    /// <see href="https://dicom.nema.org/medical/dicom/current/output/html/part04.html#sect_5.4.2.1">Dicom 3.4.5.4.2.1</see>
    /// </summary>
    /// <param name="dataset">The dataset to validate.</param>
    /// <param name="tag">The tag for the attribute that is required.</param>
    /// <param name="requirement">The requirement code expressed as an enum.</param>
    public static void ValidateRequirement(this DicomDataset dataset, DicomTag tag, RequirementCode requirement)
    {
        EnsureArg.IsNotNull(dataset, nameof(dataset));
        EnsureArg.IsNotNull(tag, nameof(tag));

        dataset.ValidateRequiredAttribute(tag, requirement);
    }

    public static void ValidateRequirement(
        this DicomDataset dataset,
        DicomTag tag,
        ProcedureStepState targetProcedureStepState,
        FinalStateRequirementCode requirement,
        Func<DicomDataset, DicomTag, bool> requirementCondition = default)
    {
        EnsureArg.IsNotNull(dataset, nameof(dataset));
        EnsureArg.IsNotNull(tag, nameof(tag));

        var predicate = (requirementCondition == default) ? (ds, tag) => false : requirementCondition;

        if (targetProcedureStepState != ProcedureStepState.Completed &&
            targetProcedureStepState != ProcedureStepState.Canceled)
        {
            return;
        }

        switch (requirement)
        {
            case FinalStateRequirementCode.R:
                dataset.ValidateRequiredAttribute(tag);
                break;
            case FinalStateRequirementCode.RC:
                if (predicate(dataset, tag))
                {
                    dataset.ValidateRequiredAttribute(tag);
                }

                break;
            case FinalStateRequirementCode.P:
                if (ProcedureStepState.Completed == targetProcedureStepState)
                {
                    dataset.ValidateRequiredAttribute(tag);
                }

                break;
            case FinalStateRequirementCode.X:
                if (ProcedureStepState.Canceled == targetProcedureStepState)
                {
                    dataset.ValidateRequiredAttribute(tag);
                }

                break;
            case FinalStateRequirementCode.O:
                break;
        }
    }

    /// <summary>
    /// Returns a dicom item if it matches a large object size criteria.
    /// </summary>
    /// <param name="dataset"></param>
    /// <param name="minLargeObjectsizeInBytes"></param>
    /// <param name="maxLargeObjectsizeInBytes"></param>
    /// <param name="largeDicomItem"></param>
    /// <returns>dicom item</returns>
    public static bool TryGetLargeDicomItem(
        this DicomDataset dataset,
        int minLargeObjectsizeInBytes,
        int maxLargeObjectsizeInBytes,
        out DicomItem largeDicomItem)
    {
        EnsureArg.IsNotNull(dataset, nameof(dataset));
        EnsureArg.IsGte(minLargeObjectsizeInBytes, 0, nameof(minLargeObjectsizeInBytes));
        EnsureArg.IsGte(maxLargeObjectsizeInBytes, 0, nameof(maxLargeObjectsizeInBytes));

        long totalSize = 0;
        largeDicomItem = null;

        var calculator = new DicomWriteLengthCalculator(dataset.InternalTransferSyntax, DicomWriteOptions.Default);

        foreach (var item in dataset)
        {
            long length = calculator.Calculate(item);
            if (length >= minLargeObjectsizeInBytes)
            {
                largeDicomItem = item;
                return true;
            }

            totalSize += length;

            // If the total size is greater than the max block size, we will return the last dicom item
            if (totalSize >= maxLargeObjectsizeInBytes)
            {
                largeDicomItem = item;
                return true;
            }
        }

        return false;
    }

    private static void ValidateRequiredAttribute(this DicomDataset dataset, DicomTag tag, RequirementCode requirementCode)
    {
        EnsureArg.IsNotNull(dataset, nameof(dataset));
        EnsureArg.IsNotNull(tag, nameof(tag));

        switch (requirementCode)
        {
            case RequirementCode.MustBeEmpty:
                dataset.ValidateEmptyValue(tag);
                break;
            case RequirementCode.NotAllowed:
                dataset.ValidateNotPresent(tag);
                break;
            default:
                dataset.ValidateRequiredAttribute(tag, MandatoryRequirementCodes.Contains(requirementCode), NonZeroLengthRequirementCodes.Contains(requirementCode));
                break;
        }
    }

    private static void ValidateRequiredAttribute(this DicomDataset dataset, DicomTag tag, bool isMandatory = true, bool isNonZero = true)
    {
        EnsureArg.IsNotNull(dataset, nameof(dataset));
        EnsureArg.IsNotNull(tag, nameof(tag));

        if (isMandatory && !dataset.Contains(tag))
        {
            throw new DatasetValidationException(
                FailureReasonCodes.MissingAttribute,
                string.Format(
                    CultureInfo.InvariantCulture,
                    DicomCoreResource.MissingRequiredTag,
                    tag));
        }

        if (isNonZero)
        {
            if (dataset.Contains(tag) && dataset.GetValueCount(tag) < 1)
            {
                throw new DatasetValidationException(
                    FailureReasonCodes.MissingAttributeValue,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        DicomCoreResource.MissingRequiredValue,
                        tag));
            }

            if (StringVRs.Contains(tag.GetDefaultVR().Code)
                    && dataset.TryGetString(tag, out string newStringValue)
                    && string.IsNullOrWhiteSpace(newStringValue))
            {
                throw new DatasetValidationException(
                    FailureReasonCodes.ValidationFailure,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        DicomCoreResource.AttributeMustNotBeEmpty,
                        tag));
            }
        }
    }

    private static void ValidateEmptyValue(this DicomDataset dataset, DicomTag tag)
    {
        EnsureArg.IsNotNull(dataset, nameof(dataset));

        if (dataset.GetValueCount(tag) > 0)
        {
            throw new DatasetValidationException(
                FailureReasonCodes.ValidationFailure,
                string.Format(
                    CultureInfo.InvariantCulture,
                    DicomCoreResource.AttributeMustBeEmpty,
                    tag));
        }
    }

    private static void ValidateNotPresent(this DicomDataset dataset, DicomTag tag)
    {
        EnsureArg.IsNotNull(dataset, nameof(dataset));

        if (dataset.Contains(tag))
        {
            throw new DatasetValidationException(
                FailureReasonCodes.ValidationFailure,
                string.Format(
                    CultureInfo.InvariantCulture,
                    DicomCoreResource.AttributeNotAllowed,
                    tag));
        }
    }

    /// <summary>
    /// Try to update dicom dataset after updating existing dataset with tag value present in newDataset.
    /// </summary>
    /// <remarks>
    /// Update for a tag happens regardless of whether the tag already had value in the existing dataset or not.
    /// </remarks>
    /// <param name="existingDataset">Existing Dataset.</param>
    /// <param name="newDataset">New Dataset.</param>
    /// <param name="tag">Tag to be updated.</param>
    /// <param name="updatedDataset">Dataset after updating <paramref name="existingDataset"/> based on values in <paramref name="newDataset"/>.</param>
    public static void AddOrUpdate(this DicomDataset existingDataset, DicomDataset newDataset, DicomTag tag, out DicomDataset updatedDataset)
    {
        EnsureArg.IsNotNull(existingDataset, nameof(existingDataset));
        EnsureArg.IsNotNull(newDataset, nameof(newDataset));
        EnsureArg.IsNotNull(tag, nameof(tag));

        updatedDataset = existingDataset;

        switch (tag.GetDefaultVR().Code)
        {
            case var code when DecimalVRs.Contains(code):
                updatedDataset = existingDataset.AddOrUpdate<decimal>(tag, newDataset.GetFirstValueOrDefault<decimal>(tag));
                break;
            case var code when DoubleVRs.Contains(code):
                updatedDataset = existingDataset.AddOrUpdate<double>(tag, newDataset.GetFirstValueOrDefault<double>(tag));
                break;
            case var code when FloatVRs.Contains(code):
                updatedDataset = existingDataset.AddOrUpdate<float>(tag, newDataset.GetFirstValueOrDefault<float>(tag));
                break;
            case var code when IntVRs.Contains(code):
                updatedDataset = existingDataset.AddOrUpdate<int>(tag, newDataset.GetFirstValueOrDefault<int>(tag));
                break;
            case var code when LongVRs.Contains(code):
                updatedDataset = existingDataset.AddOrUpdate<long>(tag, newDataset.GetFirstValueOrDefault<long>(tag));
                break;
            case var code when ShortVRs.Contains(code):
                updatedDataset = existingDataset.AddOrUpdate<short>(tag, newDataset.GetFirstValueOrDefault<short>(tag));
                break;
            case var code when StringVRs.Contains(code):
                updatedDataset = existingDataset.AddOrUpdate<string>(tag, newDataset.GetString(tag));
                break;
            case var code when UIntVRs.Contains(code):
                updatedDataset = existingDataset.AddOrUpdate<uint>(tag, newDataset.GetFirstValueOrDefault<uint>(tag));
                break;
            case var code when ULongVRs.Contains(code):
                updatedDataset = existingDataset.AddOrUpdate<ulong>(tag, newDataset.GetFirstValueOrDefault<ulong>(tag));
                break;
            case var code when UShortVRs.Contains(code):
                updatedDataset = existingDataset.AddOrUpdate<ushort>(tag, newDataset.GetFirstValueOrDefault<ushort>(tag));
                break;
            case var code when ByteArrayVRs.Contains(code):
                updatedDataset = existingDataset.AddOrUpdate(tag, newDataset.GetValues<byte>(tag));
                break;
            case "SQ":
                newDataset.CopyTo(updatedDataset, tag);
                break;
            // Other VR Types
            case "OD":
                updatedDataset = existingDataset.AddOrUpdate(tag, newDataset.GetValues<double>(tag));
                break;
            case "OF":
                updatedDataset = existingDataset.AddOrUpdate(tag, newDataset.GetValues<float>(tag));
                break;
            case "OL":
                updatedDataset = existingDataset.AddOrUpdate(tag, newDataset.GetValues<uint>(tag));
                break;
            case "OW":
                updatedDataset = existingDataset.AddOrUpdate(tag, newDataset.GetValues<ushort>(tag));
                break;
            case "OV":
                updatedDataset = existingDataset.AddOrUpdate(tag, newDataset.GetValues<ulong>(tag));
                break;
        }
    }
}
