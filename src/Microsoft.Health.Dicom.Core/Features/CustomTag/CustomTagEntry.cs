// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.Dicom.Core.Features.CustomTag
{
    /// <summary>
    /// External representation of a custom tag entry.
    /// </summary>
    public class CustomTagEntry
    {
        public CustomTagEntry(string path, string vr, CustomTagLevel level)
        {
            Path = path;
            VR = vr;
            Level = level;
        }

        public CustomTagEntry(string path, string vr, CustomTagLevel level, CustomTagStatus status)
        {
            Path = path;
            VR = vr;
            Level = level;
            Status = status;
        }

        /// <summary>
        /// Path of this tag. Normally it's composed of groupid and elementid.
        /// E.g: 00100020 is path of patient id.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// VR of this tag.
        /// </summary>
        public string VR { get; set; }

        /// <summary>
        /// Level of this tag. Could be Study, Series or Instance.
        /// </summary>
        public CustomTagLevel Level { get; set; }

        /// <summary>
        /// Status of this tag. Represents the current state the tag is in.
        /// This value is null when the entry represents a tag to be created.
        /// </summary>
        public CustomTagStatus Status { get; set; }

        public override string ToString()
        {
            return $"Path: {Path}, VR:{VR}, Level:{Level}";
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Path, VR, Level.GetHashCode(), Status.GetHashCode());
        }

        public override bool Equals(object obj)
        {
            CustomTagEntry other = obj as CustomTagEntry;
            if (other == null)
            {
                return false;
            }

            return Path == other.Path && VR == other.VR && Level == other.Level && Status == other.Status;
        }
    }
}
