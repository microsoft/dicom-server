// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Exceptions;
using Microsoft.Health.Dicom.Core.Features.Validation;
using Microsoft.Health.Dicom.Core.Messages;

namespace Microsoft.Health.Dicom.Core.Features.Model;

public class DicomIdentifier : IEquatable<DicomIdentifier>
{
    public ResourceType Type
    {
        get
        {
            // TODO: Frame?
            if (SopInstanceUid != null)
                return ResourceType.Instance;
            else if (SeriesInstanceUid != null)
                return ResourceType.Series;
            else
                return ResourceType.Study;
        }
    }

    public string StudyInstanceUid { get; }

    public string SeriesInstanceUid { get; }

    public string SopInstanceUid { get; }

    private DicomIdentifier(string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid)
    {
        StudyInstanceUid = EnsureArg.IsNotNull(studyInstanceUid, nameof(studyInstanceUid));
        SeriesInstanceUid = seriesInstanceUid;
        SopInstanceUid = sopInstanceUid;
    }

    public override bool Equals(object obj)
        => obj is DicomIdentifier other && Equals(other);

    public bool Equals(DicomIdentifier other)
        => other != null
        && string.Equals(StudyInstanceUid, other.StudyInstanceUid, StringComparison.Ordinal)
        && string.Equals(SeriesInstanceUid, other.SeriesInstanceUid, StringComparison.Ordinal)
        && string.Equals(SopInstanceUid, other.SopInstanceUid, StringComparison.Ordinal);

    public override int GetHashCode()
        => HashCode.Combine(StudyInstanceUid, SeriesInstanceUid, SopInstanceUid);

    public static DicomIdentifier ForStudy(string uid)
        => new DicomIdentifier(uid, null, null);

    public static DicomIdentifier ForSeries(string studyInstanceUid, string seriesInstanceUid)
        => new DicomIdentifier(studyInstanceUid, EnsureArg.IsNotNull(seriesInstanceUid, nameof(seriesInstanceUid)), null);

    public static DicomIdentifier ForInstance(string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid)
        => new DicomIdentifier(
            studyInstanceUid,
            EnsureArg.IsNotNull(seriesInstanceUid, nameof(seriesInstanceUid)),
            EnsureArg.IsNotNull(sopInstanceUid, nameof(sopInstanceUid)));

    public static DicomIdentifier Parse(string value)
    {
        string[] parts = EnsureArg.IsNotNull(value, nameof(value)).Split('/', StringSplitOptions.TrimEntries);
        if (parts.Length == 0 || parts.Length > 3)
            throw new FormatException();

        string studyInstanceUid, seriesInstanceUid = null, sopInstanceUid = null;

        // Parse and Validate
        try
        {
            studyInstanceUid = parts[0];
            UidValidation.Validate(studyInstanceUid, nameof(StudyInstanceUid));

            if (parts.Length > 1)
            {
                seriesInstanceUid = parts[1];
                UidValidation.Validate(seriesInstanceUid, nameof(SeriesInstanceUid));

                if (parts.Length == 3)
                {
                    sopInstanceUid = parts[2];
                    UidValidation.Validate(sopInstanceUid, nameof(SopInstanceUid));
                }
            }
        }
        catch (InvalidIdentifierException iie)
        {
            throw new FormatException(iie.Message);
        }

        return new DicomIdentifier(studyInstanceUid, seriesInstanceUid, sopInstanceUid);
    }

    public override string ToString()
    {
        if (SopInstanceUid != null)
            return StudyInstanceUid + '/' + SeriesInstanceUid + '/' + SopInstanceUid;
        else if (SeriesInstanceUid != null)
            return StudyInstanceUid + '/' + SeriesInstanceUid;
        else
            return StudyInstanceUid;
    }
}
