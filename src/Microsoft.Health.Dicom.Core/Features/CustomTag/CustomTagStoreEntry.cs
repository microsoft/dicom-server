// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Core.Features.CustomTag
{
    /// <summary>
    /// Represent each custom tag entry has retrieved from the store.
    /// </summary>
    public class CustomTagStoreEntry
    {
        public CustomTagStoreEntry(int key, string path, string vr, string privateCreator, CustomTagLevel level, CustomTagStatus status)
        {
            Key = key;
            Path = path;
            VR = vr;
            PrivateCreator = privateCreator;
            Level = level;
            Status = status;
        }

        /// <summary>
        /// Key of this custom tag entry.
        /// </summary>
        public int Key { get; set; }

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
        /// Identification code of private tag implementer of this Tag.
        /// </summary>
        /// <remarks>It's only apply to private tag. Please refer to http://dicom.nema.org/dicom/2013/output/chtml/part05/sect_7.8.html for more details.</remarks>
        public string PrivateCreator { get; set; }

        /// <summary>
        /// Level of this tag. Could be Study, Series or Instance.
        /// </summary>
        public CustomTagLevel Level { get; set; }

        /// <summary>
        /// Status of this tag.
        /// </summary>
        public CustomTagStatus Status { get; set; }

        /// <summary>
        /// Convert to  <see cref="CustomTagEntry"/>.
        /// </summary>
        /// <returns>The custom tag entry.</returns>
        public CustomTagEntry ToCustomTagEntry()
        {
            return new CustomTagEntry { Path = Path, VR = VR, PrivateCreator = PrivateCreator, Level = Level, Status = Status };
        }

        public override string ToString()
        {
            return $"Key: {Key}, Path: {Path}, VR:{VR}, PrivateCreator:{PrivateCreator}, Level:{Level}, Status:{Status}";
        }
    }
}
