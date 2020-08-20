// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Messages.Retrieve;

namespace Microsoft.Health.Dicom.Core.Features.Retrieve
{
    public class AcceptHeaderDescriptors
    {
        private AcceptHeaderDescriptor[] _descriptors;

        public AcceptHeaderDescriptors(params AcceptHeaderDescriptor[] descriptors)
        {
            EnsureArg.IsNotNull(descriptors, nameof(descriptors));
            _descriptors = descriptors;
        }

        public IEnumerable<AcceptHeaderDescriptor> Descriptors { get => _descriptors; }

        public bool TryGetMatchedPattern(AcceptHeader header, out AcceptHeaderDescriptor acceptableHeaderPattern, out string transferSyntax)
        {
            acceptableHeaderPattern = null;
            transferSyntax = string.Empty;

            foreach (AcceptHeaderDescriptor pattern in _descriptors)
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
