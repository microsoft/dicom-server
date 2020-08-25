// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Dicom;
using Dicom.IO.Buffer;

namespace Microsoft.Health.Dicom.Tests.Integration.Features
{
    public class DicomItemComparer : IEqualityComparer<DicomItem>
    {
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
            return obj.GetHashCode();
        }
    }
}
