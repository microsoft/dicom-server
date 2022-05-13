// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Globalization;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Features.Model;
using Microsoft.Health.Dicom.Core.Features.Validation;
using Microsoft.Health.Dicom.Core.Messages;

namespace Microsoft.Health.Dicom.Core.Models.Common;

/// <summary>
/// Represents an identifier for a DICOM study, series, or SOP instance.
/// </summary>
public readonly struct DicomIdentifier : IEquatable<DicomIdentifier>
{
    /// <summary>
    /// Gets a value indicating whether the identifier is the default empty identifier.
    /// </summary>
    /// <remarks>
    /// This value is considered to represent a blank study.
    /// </remarks>
    /// <value><see langword="true"/> if the value is blank; otherwise, <see langword="false"/>.</value>
    public bool IsEmpty => StudyInstanceUid == null;

    /// <summary>
    /// Gets the corresponding <see cref="ResourceType"/> for this identifier.
    /// </summary>
    /// <value>The <see cref="ResourceType"/> value.</value>
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

    /// <summary>
    /// Gets the unique identifier for the study.
    /// </summary>
    /// <value>The value of the study instance UID attribute (0020,000D).</value>
    public string StudyInstanceUid { get; }

    /// <summary>
    /// Gets the unique identifier for the series.
    /// </summary>
    /// <remarks>
    /// May be <see langword="null"/> if the value of the <see cref="Type"/> property is <see cref="ResourceType.Study"/>.
    /// </remarks>
    /// <value>The value of the series instance UID attribute (0020,000E).</value>
    public string SeriesInstanceUid { get; }

    /// <summary>
    /// Gets the unique identifier for the SOP instance.
    /// </summary>
    /// <remarks>
    /// May be <see langword="null"/> if the value of the <see cref="Type"/> property is
    /// <see cref="ResourceType.Study"/> or <see cref="ResourceType.Series"/>.
    /// </remarks>
    /// <value>The value of the SOP instance UID attribute (0008,0018).</value>
    public string SopInstanceUid { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DicomIdentifier"/> structure based on the given identifiers.
    /// </summary>
    /// <param name="studyInstanceUid">The unique identifier for the study.</param>
    /// <param name="seriesInstanceUid">The optional unique identifier for the series.</param>
    /// <param name="sopInstanceUid">The optional unique identifier for the SOP instance.</param>
    /// <exception cref="ArgumentNullException">
    /// <para><paramref name="studyInstanceUid"/> is <see langword="null"/> or white space</para>
    /// <para>-or-</para>
    /// <para>
    /// <paramref name="seriesInstanceUid"/> is <see langword="null"/> or white space, but
    /// <paramref name="sopInstanceUid"/> has a value.
    /// </para>
    /// </exception>
    public DicomIdentifier(string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid)
    {
        StudyInstanceUid = EnsureArg.IsNotNullOrWhiteSpace(studyInstanceUid, nameof(studyInstanceUid));
        if (string.IsNullOrWhiteSpace(seriesInstanceUid) && !string.IsNullOrWhiteSpace(sopInstanceUid))
            throw new ArgumentNullException(nameof(seriesInstanceUid));

        SeriesInstanceUid = seriesInstanceUid;
        SopInstanceUid = sopInstanceUid;
    }

    /// <summary>
    /// Returns a value indicating whether this instance is equal to a specified object.
    /// </summary>
    /// <param name="obj">The object to compare to this instance.</param>
    /// <returns>
    /// <see langword="true"/> if <paramref name="obj"/> is an instance of <see cref="DicomIdentifier"/> and
    /// equals the value of this instance; otherwise, <see langword="false"/>.
    /// </returns>
    public override bool Equals(object obj)
        => obj is DicomIdentifier other && Equals(other);

    /// <summary>
    /// Returns a value indicating whether the value of this instance is equal to the value of the
    /// specified <see cref="DicomIdentifier"/> instance.
    /// </summary>
    /// <param name="other">The object to compare to this instance.</param>
    /// <returns>
    /// <see langword="true"/> if the <paramref name="other"/> parameter equals the
    /// value of this instance; otherwise, <see langword="false"/>.
    /// </returns>
    public bool Equals(DicomIdentifier other)
        => string.Equals(StudyInstanceUid, other.StudyInstanceUid, StringComparison.Ordinal)
            && string.Equals(SeriesInstanceUid, other.SeriesInstanceUid, StringComparison.Ordinal)
            && string.Equals(SopInstanceUid, other.SopInstanceUid, StringComparison.Ordinal);

    /// <summary>
    /// Returns the hash code for this instance.
    /// </summary>
    /// <returns>A 32-bit signed integer hash code.</returns>
    public override int GetHashCode()
        => HashCode.Combine(StudyInstanceUid, SeriesInstanceUid, SopInstanceUid);

    /// <summary>
    /// Converts the value of the current <see cref="DicomIdentifier"/> structure to its equivalent string representation.
    /// </summary>
    /// <returns>A string representation of the value of the current <see cref="DicomIdentifier"/> structure.</returns>
    public override string ToString()
    {
        if (SopInstanceUid != null)
            return StudyInstanceUid + '/' + SeriesInstanceUid + '/' + SopInstanceUid;
        else if (SeriesInstanceUid != null)
            return StudyInstanceUid + '/' + SeriesInstanceUid;
        else
            return StudyInstanceUid;
    }

    /// <summary>
    /// Creates a new <see cref="DicomIdentifier"/> structure for the given study.
    /// </summary>
    /// <param name="uid">The unique identifier for the study.</param>
    /// <returns>A <see cref="DicomIdentifier"/> structure for the given study.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="uid"/> is <see langword="null"/>.</exception>
    public static DicomIdentifier ForStudy(string uid)
        => new DicomIdentifier(uid, null, null);

    /// <summary>
    /// Creates a new <see cref="DicomIdentifier"/> structure for the given series.
    /// </summary>
    /// <param name="studyInstanceUid">The unique identifier for the study.</param>
    /// <param name="seriesInstanceUid">The unique identifier for the series.</param>
    /// <returns>A <see cref="DicomIdentifier"/> structure for the given series.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="studyInstanceUid"/> or <paramref name="seriesInstanceUid"/> is <see langword="null"/>.
    /// </exception>
    public static DicomIdentifier ForSeries(string studyInstanceUid, string seriesInstanceUid)
        => new DicomIdentifier(studyInstanceUid, EnsureArg.IsNotNullOrWhiteSpace(seriesInstanceUid, nameof(seriesInstanceUid)), null);

    /// <summary>
    /// Creates a new <see cref="DicomIdentifier"/> structure for the given SOP instance.
    /// </summary>
    /// <param name="studyInstanceUid">The unique identifier for the study.</param>
    /// <param name="seriesInstanceUid">The unique identifier for the series.</param>
    /// <param name="sopInstanceUid">The unique identifier for the SOP instance.</param>
    /// <returns>A <see cref="DicomIdentifier"/> structure for the given SOP instance.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="studyInstanceUid"/>, <paramref name="seriesInstanceUid"/>, <paramref name="sopInstanceUid"/>
    /// is <see langword="null"/>.
    /// </exception>
    public static DicomIdentifier ForInstance(string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid)
        => new DicomIdentifier(
            studyInstanceUid,
            EnsureArg.IsNotNullOrWhiteSpace(seriesInstanceUid, nameof(seriesInstanceUid)),
            EnsureArg.IsNotNullOrWhiteSpace(sopInstanceUid, nameof(sopInstanceUid)));

    /// <summary>
    /// Creates a new <see cref="DicomIdentifier"/> structure equivalent to the given <see cref="VersionedInstanceIdentifier"/>.
    /// </summary>
    /// <param name="identifier">A versioned instance identifier.</param>
    /// <returns>A <see cref="DicomIdentifier"/> structure for the given SOP instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="identifier"/> is <see langword="null"/>.</exception>
    public static DicomIdentifier ForInstance(VersionedInstanceIdentifier identifier)
        => new DicomIdentifier(
            EnsureArg.IsNotNull(identifier, nameof(identifier)).StudyInstanceUid,
            identifier.SeriesInstanceUid,
            identifier.SopInstanceUid);

    /// <summary>
    /// Converts the string representation of a <see cref="DicomIdentifier"/> to its equivalent structure representation.
    /// </summary>
    /// <param name="value">A string that contains a <see cref="DicomIdentifier"/> to convert.</param>
    /// <returns>
    /// An object that is equivalent to the <see cref="DicomIdentifier"/> contained in <paramref name="value"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    /// <exception cref="FormatException">
    /// <paramref name="value"/> does not contain a valid string representation of a <see cref="DicomIdentifier"/>.
    /// </exception>
    public static DicomIdentifier Parse(string value)
        => TryParse(EnsureArg.IsNotNull(value, nameof(value)), out DicomIdentifier result)
            ? result
            : throw new FormatException(string.Format(CultureInfo.CurrentCulture, DicomCoreResource.InvalidDicomIdentifier, value));

    /// <summary>
    /// Converts the string representation of a <see cref="DicomIdentifier"/> to its equivalent structure representation
    /// and returns a value that indicates whether the conversion succeeded.
    /// </summary>
    /// <param name="value">A string that contains a <see cref="DicomIdentifier"/> to convert.</param>
    /// <param name="result">
    /// When this method returns, contains the <see cref="DicomIdentifier"/> value equivalent identifier
    /// contained in <paramref name="value"/>, if the conversion succeeded, or the default value if the conversion
    /// failed. The conversion fails if the <paramref name="value"/> parameter is <see langword="null"/>,
    /// is an empty string (""), or does not contain a valid string representation of an identifier.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the <paramref name="value"/> parameter was converted successfully;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public static bool TryParse(string value, out DicomIdentifier result)
    {
        string[] parts = EnsureArg.IsNotNull(value, nameof(value)).Split('/', StringSplitOptions.TrimEntries);
        if (parts.Length == 0 || parts.Length > 3)
        {
            result = default;
            return false;
        }

        string studyInstanceUid, seriesInstanceUid = null, sopInstanceUid = null;

        // Parse and Validate
        studyInstanceUid = parts[0];
        if (!UidValidation.IsValid(studyInstanceUid))
        {
            result = default;
            return false;
        }

        if (parts.Length > 1)
        {
            seriesInstanceUid = parts[1];
            if (!UidValidation.IsValid(seriesInstanceUid))
            {
                result = default;
                return false;
            }

            if (parts.Length == 3)
            {
                sopInstanceUid = parts[2];
                if (!UidValidation.IsValid(sopInstanceUid))
                {
                    result = default;
                    return false;
                }
            }
        }

        result = new DicomIdentifier(studyInstanceUid, seriesInstanceUid, sopInstanceUid);
        return true;
    }

    /// <summary>
    /// Determines whether two specified instances of <see cref="DicomIdentifier"/> are equal.
    /// </summary>
    /// <param name="left">The first object to compare.</param>
    /// <param name="right">The second object to compare.</param>
    /// <returns>
    /// <see langword="true"/> if <paramref name="left"/> and <paramref name="right"/>
    /// represent the same <see cref="DicomIdentifier"/>; otherwise, <see langword="false"/>.
    /// </returns>
    public static bool operator ==(DicomIdentifier left, DicomIdentifier right)
        => left.Equals(right);

    /// <summary>
    /// Determines whether two specified instances of <see cref="DicomIdentifier"/> are not equal.
    /// </summary>
    /// <param name="left">The first object to compare.</param>
    /// <param name="right">The second object to compare.</param>
    /// <returns>
    /// <see langword="true"/> if <paramref name="left"/> and <paramref name="right"/>
    /// do not represent the same <see cref="DicomIdentifier"/>; otherwise, <see langword="false"/>.
    /// </returns>
    public static bool operator !=(DicomIdentifier left, DicomIdentifier right)
        => !left.Equals(right);
}
