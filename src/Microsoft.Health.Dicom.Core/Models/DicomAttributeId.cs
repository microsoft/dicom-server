// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Dicom;
using EnsureThat;

namespace Microsoft.Health.Dicom.Core.Models
{
    public class DicomAttributeId
    {
        public DicomAttributeId(params DicomTag[] path)
            : this(new List<DicomTag>(path))
        {
        }

        public DicomAttributeId(IReadOnlyList<DicomTag> path)
        {
            EnsureArg.IsNot(path.Count, 0, nameof(path));

            Path = path;
            IsPrivate = path.Any(x => x.IsPrivate);
        }

        public bool IsPrivate { get; }

        public IReadOnlyList<DicomTag> Path { get; }

        public DicomTag Tag { get => Path[Path.Count - 1]; }

        public static bool TryParse(string text, out DicomAttributeId attributeId)
        {
            attributeId = null;
            List<DicomTag> path = new List<DicomTag>();
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            string[] sections = text.Split('.');
            foreach (string section in sections)
            {
                if (TryParse(section, out DicomTag tag))
                {
                    path.Add(tag);
                }
                else
                {
                    return false;
                }
            }

            attributeId = new DicomAttributeId(path);
            return true;
        }

        private static bool TryParse(string text, out DicomTag tag)
        {
            tag = null;
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            // if text is name
            tag = DicomDictionary.Default[text];
            if (tag != null)
            {
                return true;
            }

            // parse text as group number + element number
            return TryParseDicomTagNumber(text, out tag);
        }

        private static bool TryParseDicomTagNumber(string s, out DicomTag tag)
        {
            tag = null;
            if (s.Length < 8)
            {
                return false;
            }

            if (!ushort.TryParse(s.Substring(0, 4), NumberStyles.HexNumber, null, out ushort group))
            {
                return false;
            }

            if (!ushort.TryParse(s.Substring(4, 4), NumberStyles.HexNumber, null, out ushort element))
            {
                return false;
            }

            tag = new DicomTag(group, element);
            return true;
        }

        public string GetFullPath()
        {
            return string.Join(".", Path.Select(item => GetPathForTag(item)));
        }

        private static string GetPathForTag(DicomTag tag)
        {
            return To4Hex(tag.Group) + To4Hex(tag.Element);
        }

        private static string To4Hex(int num)
        {
            return string.Format("{0:X4}", num);
        }

        public override bool Equals(object obj)
        {
            if (object.ReferenceEquals(this, obj))
            {
                return true;
            }

            if (!(obj is DicomAttributeId))
            {
                return false;
            }

            DicomAttributeId objId = (DicomAttributeId)obj;

            // compare path
            if (objId.Path.Count != Path.Count)
            {
                return false;
            }

            return Enumerable.SequenceEqual(Path, objId.Path);
        }

        public override int GetHashCode()
        {
            return GetHashCodeOfArray(Path);
        }

        private static int GetHashCodeOfArray<T>(IEnumerable<T> ary)
        {
            // refer to http://hg.openjdk.java.net/jdk8u/jdk8u/jdk/file/be44bff34df4/src/share/classes/java/util/Arrays.java
            if (ary == null)
            {
                return 0;
            }

            int result = 1;

            foreach (T element in ary)
            {
                result = unchecked((31 * result) + element.GetHashCode());
            }

            return result;
        }
    }
}
