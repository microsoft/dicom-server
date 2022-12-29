// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Messages.Retrieve;

namespace Microsoft.Health.Dicom.Core.Features.Retrieve;

public class AcceptHeaderDescriptors
{
    private readonly AcceptHeaderDescriptor[] _descriptors;

    public AcceptHeaderDescriptors(params AcceptHeaderDescriptor[] descriptors)
    {
        EnsureArg.IsNotNull(descriptors, nameof(descriptors));
        _descriptors = descriptors;
    }

    public IEnumerable<AcceptHeaderDescriptor> Descriptors { get => _descriptors; }

    public bool IsValidAcceptHeader(AcceptHeader header)
    {
        foreach (AcceptHeaderDescriptor descriptor in _descriptors)
        {
            if (descriptor.IsAcceptable(header))
            {
                descriptor.SetTransferSyntax(header);
                return true;
            }
        }
        return false;
    }
}
