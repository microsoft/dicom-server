// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using DicomSchemaManager.Core.Properties;

namespace DicomSchemaManager.Core;

//smart enum pattern as described here: https://ardalis.com/enum-alternatives-in-c/

public sealed class ApplyCommandResult
{
    public static readonly ApplyCommandResult Successful = new ApplyCommandResult(0, Resources.SuccessfulApplyFormatString);
    public static readonly ApplyCommandResult Unsuccessful = new ApplyCommandResult(1, Resources.UnsuccessfulApplyFormatString);
    public static readonly ApplyCommandResult Unnecessary = new ApplyCommandResult(2, Resources.UnnecessaryApplyFormatString);
    public static readonly ApplyCommandResult Incompatible = new ApplyCommandResult(3, Resources.IncompatibleVersionFormatString);

    public string MessageFormatString { get; } = string.Empty;
    public int ExitCode { get; }

    private ApplyCommandResult() { }

    private ApplyCommandResult(int exitCode, string message)
    {
        MessageFormatString = message;
        ExitCode = exitCode;
    }

}
