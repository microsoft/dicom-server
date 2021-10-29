// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using FellowOakDicom;
using Dicom.IO.Buffer;
using EnsureThat;

namespace Microsoft.Health.Dicom.Tests.Common.Comparers
{
    /// <summary>
    /// Compare if 2 DicomItem equals or not.
    /// Most DicomItem doesn't have method Equals implented, so has to make it ourselves.
    /// </summary>
    public class DicomItemEqualityComparer : IEqualityComparer<DicomItem>
    {
        public static DicomItemEqualityComparer Default { get; } = new DicomItemEqualityComparer();

        public bool Equals([AllowNull] DicomItem x, [AllowNull] DicomItem y)
        {
            if (x == null || y == null)
            {
                return object.ReferenceEquals(x, y);
            }

            if (x.GetType() != y.GetType())
            {
                return false;
            }

            if (typeof(DicomElement).IsAssignableFrom(x.GetType()))
            {
                return DicomElementEquals((DicomElement)x, (DicomElement)y);
            }

            // It's not perfect, but enough for our test
            return x.Equals(y);
        }

        private bool DicomItemEquals(DicomItem x, DicomItem y)
        {
            return x.Tag == y.Tag && x.ValueRepresentation == y.ValueRepresentation;
        }

        private bool DicomElementEquals(DicomElement x, DicomElement y)
        {
            return DicomItemEquals(x, y) && x.Count == y.Count && ByteBufferEquals(x.Buffer, y.Buffer);
        }

        private bool ByteBufferEquals(IByteBuffer x, IByteBuffer y)
        {
            if (x == null || y == null)
            {
                return object.ReferenceEquals(x, y);
            }

            if (x.Size != y.Size)
            {
                return false;
            }

            for (int i = 0; i < x.Size; i++)
            {
                if (x.Data[i] != y.Data[i])
                {
                    return false;
                }
            }

            return true;
        }

        public int GetHashCode([DisallowNull] DicomItem obj)
        {
            EnsureArg.IsNotNull(obj, nameof(obj));
            return obj.GetHashCode();
        }
    }
}
