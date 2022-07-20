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
[assembly: SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "<Pending>", Scope = "member", Target = "~M:Microsoft.Health.Dicom.Api.Web.FileBufferingReadStream.#ctor(System.IO.Stream,System.Int32,System.Nullable{System.Int64},System.Func{System.String},System.Buffers.ArrayPool{System.Byte})")]
[assembly: SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>", Scope = "member", Target = "~F:Microsoft.Health.Dicom.Api.Web.FileBufferingReadStream._maxRentedBufferSize")]
[assembly: SuppressMessage("Style", "IDE0005:Using directive is unnecessary.", Justification = "<Pending>")]
[assembly: SuppressMessage("Style", "IDE0032:Use auto property", Justification = "<Pending>", Scope = "member")]
[assembly: SuppressMessage("Style", "IDE0032:Use auto property", Justification = "<Pending>", Scope = "member", Target = "~F:Microsoft.Health.Dicom.Api.Web.FileBufferingReadStream._memoryThreshold")]
[assembly: SuppressMessage("Style", "IDE0032:Use auto property", Justification = "<Pending>", Scope = "member", Target = "~F:Microsoft.Health.Dicom.Api.Web.FileBufferingReadStream._tempFileName")]
[assembly: SuppressMessage("Style", "IDE0032:Use auto property", Justification = "<Pending>", Scope = "member", Target = "~F:Microsoft.Health.Dicom.Api.Web.FileBufferingReadStream._inMemory")]
[assembly: SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "<Pending>", Scope = "member", Target = "~M:Microsoft.Health.Dicom.Api.Web.FileBufferingReadStream.#ctor(System.IO.Stream,System.Int32,System.Nullable{System.Int64},System.String,System.Buffers.ArrayPool{System.Byte})")]
[assembly: SuppressMessage("Style", "IDE0032:Use auto property", Justification = "<Pending>", Scope = "member", Target = "~P:Microsoft.Health.Dicom.Api.Web.FileBufferingReadStream.MemoryThreshold")]
[assembly: SuppressMessage("Style", "IDE0032:Use auto property", Justification = "<Pending>", Scope = "member", Target = "~P:Microsoft.Health.Dicom.Api.Web.FileBufferingReadStream.InMemory")]
[assembly: SuppressMessage("Style", "IDE0032:Use auto property", Justification = "<Pending>", Scope = "member", Target = "~P:Microsoft.Health.Dicom.Api.Web.FileBufferingReadStream.TempFileName")]
[assembly: SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "<Pending>", Scope = "member", Target = "~M:Microsoft.Health.Dicom.Api.Web.FileBufferingReadStream.CopyToAsync(System.IO.Stream,System.Int32,System.Threading.CancellationToken)~System.Threading.Tasks.Task")]
[assembly: SuppressMessage("Usage", "CA2215:Dispose methods should call base class dispose", Justification = "<Pending>", Scope = "member", Target = "~M:Microsoft.Health.Dicom.Api.Web.FileBufferingReadStream.Dispose(System.Boolean)")]
[assembly: SuppressMessage("Usage", "CA2213:Disposable fields should be disposed", Justification = "<Pending>", Scope = "member", Target = "~F:Microsoft.Health.Dicom.Api.Web.FileBufferingReadStream._inner")]
[assembly: SuppressMessage("Usage", "CA2215:Dispose methods should call base class dispose", Justification = "<Pending>", Scope = "member", Target = "~M:Microsoft.Health.Dicom.Api.Web.FileBufferingReadStream.DisposeAsync~System.Threading.Tasks.ValueTask")]
