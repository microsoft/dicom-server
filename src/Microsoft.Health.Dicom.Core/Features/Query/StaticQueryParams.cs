// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using EnsureThat;

namespace Microsoft.Health.Dicom.Core.Features.Query
{
    public class StaticQueryParams
    {
        private static readonly ISet<string> KnownParams = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "offset","limit","fuzzymatching","includefield"
        };

        [Range(0, int.MaxValue)]
        public int? Offset { get; set; }

        [Range(0, int.MaxValue)]
        public int? Limit { get; set; }

        public bool? FuzzyMatching { get; set; }

        public IReadOnlyList<string> IncludeField { get; set; }

        public static bool IsStaticQueryKey(string key)
        {
            return KnownParams.Contains(EnsureArg.IsNotNull(key, nameof(key)).Trim());
        }
    }
}
