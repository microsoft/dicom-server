// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using EnsureThat;

namespace Microsoft.Health.Dicom.Core.Features.Persistence
{
    public class DicomFrames : IDicomResource
    {
        public DicomFrames(DicomInstance instance, params int[] frames)
        {
            EnsureArg.IsNotNull(instance, nameof(instance));
            EnsureArg.IsNotNull(frames, nameof(frames));
            EnsureArg.IsGt(frames.Length, 0, nameof(frames));

            Instance = instance;
            Frames = frames;
        }

        public DicomInstance Instance { get; }

        public IEnumerable<int> Frames { get; }

        public string StudyInstanceUID => Instance.StudyInstanceUID;
    }
}
