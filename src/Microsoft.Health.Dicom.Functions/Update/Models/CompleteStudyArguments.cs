// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Dicom.Functions.Update.Models;

/// <summary>
/// Represents input to <see cref="UpdateDurableFunction.CompleteUpdateStudyAsync"/>
/// </summary>
public sealed class CompleteStudyArguments
{
    public string InputIdentifier { get; }

    public int StudyInstanceIndex { get; }

    public CompleteStudyArguments(string inputIdentifier, int studyInstanceIndex)
    {
        InputIdentifier = inputIdentifier;
        StudyInstanceIndex = studyInstanceIndex;
    }
}
