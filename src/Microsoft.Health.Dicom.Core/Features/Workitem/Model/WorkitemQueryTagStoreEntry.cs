// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.ObjectModel;
using EnsureThat;
using FellowOakDicom;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;

namespace Microsoft.Health.Dicom.Core.Features.Workitem;

/// <summary>
/// Represent each workitem query tag entry has retrieved from the store.
/// </summary>
public class WorkitemQueryTagStoreEntry : QueryTagEntry
{
    public WorkitemQueryTagStoreEntry(int key, string path, string vr)
    {
        Key = key;
        Path = EnsureArg.IsNotNullOrWhiteSpace(path);
        VR = EnsureArg.IsNotNullOrWhiteSpace(vr);
    }

    /// <summary>
    /// Key of this extended query tag entry.
    /// </summary>
    public int Key { get; }

    /// <summary>
    /// Get the DicomTags that is the representation of the path for this tag
    /// This is populated while fetching query tag from the db
    /// </summary>
    public ReadOnlyCollection<DicomTag> PathTags { get; set; }
}
