// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Health.Dicom.Core.Messages.Retrieve
{
    public class AcceptHeaderQuantityComparer : IComparer<AcceptHeader>
    {
        private const double DefaultQuantity = 1.0;

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

            double xQuanity = x.Quality.GetValueOrDefault(DefaultQuantity);
            double yQuanity = y.Quality.GetValueOrDefault(DefaultQuantity);
            if (xQuanity == yQuanity)
            {
                return 0;
            }

            return xQuanity > yQuanity ? 1 : -1;
        }
    }
}
