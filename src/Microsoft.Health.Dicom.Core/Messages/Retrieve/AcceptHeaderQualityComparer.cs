// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Health.Dicom.Core.Messages.Retrieve
{
    public class AcceptHeaderQualityComparer : IComparer<AcceptHeader>
    {
        private const double DefaultQuality = 1.0;

        public int Compare([AllowNull] AcceptHeader x, [AllowNull] AcceptHeader y)
        {
            if (x == y)
            {
                return 0;
            }

            if (x == null || y == null)
            {
                return x != null ? 1 : -1;
            }

            double xQuality = x.Quality.GetValueOrDefault(DefaultQuality);
            double yQuality = y.Quality.GetValueOrDefault(DefaultQuality);
            if (xQuality == yQuality)
            {
                return 0;
            }

            return xQuality > yQuality ? 1 : -1;
        }
    }
}
