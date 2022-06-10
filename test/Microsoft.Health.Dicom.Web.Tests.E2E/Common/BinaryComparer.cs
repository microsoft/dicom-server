// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Common;

internal sealed class BinaryComparer : IEqualityComparer<Stream>
{
    public static BinaryComparer Instance { get; } = new BinaryComparer();

    public bool Equals(Stream x, Stream y)
    {
        if (ReferenceEquals(x, y))
            return true;

        // Above check will find both null
        if (x is null || x is null)
            return false;

        // TODO: Write this more efficiently by reading one word at a time
        int xByte, yByte;
        do
        {
            xByte = x.ReadByte();
            yByte = y.ReadByte();
        } while (xByte == yByte && xByte != -1 && yByte != -1);

        return xByte == yByte;
    }

    public int GetHashCode(Stream obj)
    {
        throw new NotImplementedException();
    }
}
