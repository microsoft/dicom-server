// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Design", "CA1019:Define accessors for attribute arguments", Justification = "ASP.NET Core attributes leverage arguments that they do not necessarily wish to expose.")]

// [assembly: SuppressMessage("Performance", "CA1813:Avoid unsealed attributes", Justification = "<Pending>")]
