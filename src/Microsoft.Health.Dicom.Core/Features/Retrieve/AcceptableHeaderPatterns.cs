// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Microsoft.Health.Dicom.Core.Messages.Retrieve;

namespace Microsoft.Health.Dicom.Core.Features.Retrieve
{
    public class AcceptableHeaderPatterns
    {
        private AcceptableHeaderPattern[] _patterns;

        public AcceptableHeaderPatterns(params AcceptableHeaderPattern[] patterns)
        {
            _patterns = patterns;
        }

        public IEnumerable<AcceptableHeaderPattern> Patterns { get => _patterns; }

        public bool TryGetMatchedPattern(AcceptHeader header, out AcceptableHeaderPattern acceptableHeaderPattern, out string transferSyntax)
        {
            acceptableHeaderPattern = null;
            transferSyntax = string.Empty;

            foreach (AcceptableHeaderPattern pattern in _patterns)
            {
                if (pattern.IsAcceptable(header, out transferSyntax))
                {
                    acceptableHeaderPattern = pattern;
                    return true;
                }
            }

            return false;
        }
    }
}
