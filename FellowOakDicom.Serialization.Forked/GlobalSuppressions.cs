// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Style", "IDE0161:Convert to file-scoped namespace", Justification = "<Pending>", Scope = "namespace", Target = "~N:FellowOakDicom.Serialization.Forked")]
[assembly: SuppressMessage("Style", "IDE0073:The file header is missing or not located at the top of the file", Justification = "<Pending>")]
[assembly: SuppressMessage("Style", "IDE0005:Using directive is unnecessary.", Justification = "<Pending>")]
[assembly: SuppressMessage("Compiler", "CS1573:Using directive is unnecessary.", Justification = "<Pending>")]
[assembly: SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "<Pending>", Scope = "member", Target = "~M:FellowOakDicom.Serialization.Forked.DicomXML.WriteToXml(FellowOakDicom.DicomFile)~System.String")]
[assembly: SuppressMessage("Globalization", "CA1305:Specify IFormatProvider", Justification = "<Pending>", Scope = "member", Target = "~M:FellowOakDicom.Serialization.Forked.DicomXML.DicomDatasetToXml(System.Text.StringBuilder,FellowOakDicom.DicomDataset)")]
[assembly: SuppressMessage("Globalization", "CA1305:Specify IFormatProvider", Justification = "<Pending>", Scope = "member", Target = "~M:FellowOakDicom.Serialization.Forked.DicomXML.DicomElementToXml(System.Text.StringBuilder,FellowOakDicom.DicomElement)")]
[assembly: SuppressMessage("Globalization", "CA1307:Specify StringComparison for clarity", Justification = "<Pending>", Scope = "member", Target = "~M:FellowOakDicom.Serialization.Forked.DicomXML.EscapeXml(System.String)~System.String")]
[assembly: SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "<Pending>", Scope = "member", Target = "~M:FellowOakDicom.Serialization.Forked.DicomArrayJsonConverter.Write(System.Text.Json.Utf8JsonWriter,FellowOakDicom.DicomDataset[],System.Text.Json.JsonSerializerOptions)")]
[assembly: SuppressMessage("Style", "IDE0008:Use explicit type", Justification = "<Pending>", Scope = "member", Target = "~M:FellowOakDicom.Serialization.Forked.DicomArrayJsonConverter.Write(System.Text.Json.Utf8JsonWriter,FellowOakDicom.DicomDataset[],System.Text.Json.JsonSerializerOptions)")]
[assembly: SuppressMessage("Style", "IDE0036:Order modifiers", Justification = "<Pending>", Scope = "type", Target = "~T:FellowOakDicom.Serialization.Forked.DicomJsonConverter")]
[assembly: SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>", Scope = "member", Target = "~F:FellowOakDicom.Serialization.Forked.DicomJsonConverter._jsonTextEncodings")]
[assembly: SuppressMessage("Performance", "CA1802:Use literals where appropriate", Justification = "<Pending>", Scope = "member", Target = "~F:FellowOakDicom.Serialization.Forked.DicomJsonConverter._personNameComponentGroupDelimiter")]
[assembly: SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>", Scope = "member", Target = "~F:FellowOakDicom.Serialization.Forked.DicomJsonConverter._personNameComponentGroupDelimiter")]
[assembly: SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>", Scope = "member", Target = "~F:FellowOakDicom.Serialization.Forked.DicomJsonConverter._personNameComponentGroupNames")]
[assembly: SuppressMessage("Documentation", "CA1200:Avoid using cref tags with a prefix", Justification = "<Pending>", Scope = "member", Target = "~M:FellowOakDicom.Serialization.Forked.DicomJsonConverter.CreateBulkDataUriByteBuffer(System.String)~FellowOakDicom.IO.Buffer.IBulkDataUriByteBuffer")]
[assembly: SuppressMessage("Design", "CA1054:URI-like parameters should not be strings", Justification = "<Pending>", Scope = "member", Target = "~M:FellowOakDicom.Serialization.Forked.DicomJsonConverter.CreateBulkDataUriByteBuffer(System.String)~FellowOakDicom.IO.Buffer.IBulkDataUriByteBuffer")]
[assembly: SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "<Pending>", Scope = "member", Target = "~M:FellowOakDicom.Serialization.Forked.DicomJsonConverter.FindValue(System.Text.Json.Utf8JsonReader,System.String,System.String)~System.String")]
[assembly: SuppressMessage("Documentation", "CA1200:Avoid using cref tags with a prefix", Justification = "<Pending>", Scope = "member", Target = "~M:FellowOakDicom.Serialization.Forked.DicomJsonConverter.Write(System.Text.Json.Utf8JsonWriter,FellowOakDicom.DicomDataset,System.Text.Json.JsonSerializerOptions)")]
[assembly: SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "<Pending>", Scope = "member", Target = "~M:FellowOakDicom.Serialization.Forked.DicomJsonConverter.Write(System.Text.Json.Utf8JsonWriter,FellowOakDicom.DicomDataset,System.Text.Json.JsonSerializerOptions)")]
[assembly: SuppressMessage("Documentation", "CA1200:Avoid using cref tags with a prefix", Justification = "<Pending>", Scope = "member", Target = "~M:FellowOakDicom.Serialization.Forked.DicomJsonConverter.Read(System.Text.Json.Utf8JsonReader@,System.Type,System.Text.Json.JsonSerializerOptions)~FellowOakDicom.DicomDataset")]
[assembly: SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "<Pending>", Scope = "member", Target = "~M:FellowOakDicom.Serialization.Forked.DicomJsonConverter.ReadJsonPersonName(System.Text.Json.Utf8JsonReader@)~System.String[]")]
[assembly: SuppressMessage("Style", "IDE0008:Use explicit type", Justification = "<Pending>", Scope = "member", Target = "~M:FellowOakDicom.Serialization.Forked.DicomJsonConverter.ReadJsonBulkDataUri(System.Text.Json.Utf8JsonReader@)~FellowOakDicom.IO.Buffer.IBulkDataUriByteBuffer")]
