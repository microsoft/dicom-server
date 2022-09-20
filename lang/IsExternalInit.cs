// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

// This file defines the IsExternalInit static class used to implement init-only properties.
//
// Documentation: https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/init
// Source: https://github.com/dotnet/runtime/blob/v6.0.0/src/libraries/Common/src/System/Runtime/CompilerServices/IsExternalInit.cs

#if NETSTANDARD2_0

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace System.Runtime.CompilerServices;

[ExcludeFromCodeCoverage]
[EditorBrowsable(EditorBrowsableState.Never)]
internal static class IsExternalInit
{ }

#endif
