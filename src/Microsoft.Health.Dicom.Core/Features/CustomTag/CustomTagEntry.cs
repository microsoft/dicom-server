// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Core.Features.CustomTag
{
    /// <summary>
    /// Represent each custom tag entry has retrieved from the store.
    /// </summary>
    public class CustomTagEntry
    {
        public CustomTagEntry(long key, string path, string vr, CustomTagLevel level, CustomTagStatus status)
        {
            Key = key;
            Path = path;
            VR = vr;
            Level = level;
            Status = status;
        }

        /// <summary>
        /// Key of this custom tag entry.
        /// </summary>
        public long Key { get; set; }

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
        /// Status of this tag.
        /// </summary>
        public CustomTagStatus Status { get; set; }

        public override string ToString()
        {
            return $"Key: {Key}, Path: {Path}, VR:{VR}, Level:{Level}, Status:{Status}";
        }
    }
}
