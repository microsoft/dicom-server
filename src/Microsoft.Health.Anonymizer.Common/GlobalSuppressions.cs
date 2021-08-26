// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Performance", "CA1820:Test for empty strings using string length", Justification = "Hackathon", Scope = "member", Target = "~M:Microsoft.Health.Anonymizer.Common.EncryptFunction.Decrypt(System.String,System.Text.Encoding)~System.Byte[]")]
[assembly: SuppressMessage("Performance", "CA1825:Avoid zero-length array allocations", Justification = "Hackathon", Scope = "member", Target = "~M:Microsoft.Health.Anonymizer.Common.EncryptFunction.Decrypt(System.String,System.Text.Encoding)~System.Byte[]")]
[assembly: SuppressMessage("Security", "CA5401:Do not use CreateEncryptor with non-default IV", Justification = "Hackathon", Scope = "member", Target = "~M:Microsoft.Health.Anonymizer.Common.EncryptFunction.Encrypt(System.Byte[])~System.Byte[]")]
[assembly: SuppressMessage("Performance", "CA1825:Avoid zero-length array allocations", Justification = "Hackathon", Scope = "member", Target = "~M:Microsoft.Health.Anonymizer.Common.EncryptFunction.Encrypt(System.String,System.Text.Encoding)~System.Byte[]")]
[assembly: SuppressMessage("Performance", "CA1820:Test for empty strings using string length", Justification = "Hackathon", Scope = "member", Target = "~M:Microsoft.Health.Anonymizer.Common.EncryptFunction.Encrypt(System.String,System.Text.Encoding)~System.Byte[]")]
[assembly: SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Hackathon", Scope = "member", Target = "~M:Microsoft.Health.Anonymizer.Common.EncryptFunction.StreamToByte(System.IO.Stream)~System.Byte[]")]
[assembly: SuppressMessage("Globalization", "CA1310:Specify StringComparison for correctness", Justification = "Hackathon", Scope = "member", Target = "~M:Microsoft.Health.Anonymizer.Common.RedactFunction.RedactPostalCode(System.String)~System.String")]
[assembly: SuppressMessage("Performance", "CA1805:Do not initialize unnecessarily", Justification = "Hackathon", Scope = "member", Target = "~P:Microsoft.Health.Anonymizer.Common.Models.DateTimeObject.HasDay")]
[assembly: SuppressMessage("Performance", "CA1805:Do not initialize unnecessarily", Justification = "Hackathon", Scope = "member", Target = "~P:Microsoft.Health.Anonymizer.Common.Models.DateTimeObject.HasHour")]
[assembly: SuppressMessage("Performance", "CA1805:Do not initialize unnecessarily", Justification = "Hackathon", Scope = "member", Target = "~P:Microsoft.Health.Anonymizer.Common.Models.DateTimeObject.HasMilliSecond")]
[assembly: SuppressMessage("Performance", "CA1805:Do not initialize unnecessarily", Justification = "Hackathon", Scope = "member", Target = "~P:Microsoft.Health.Anonymizer.Common.Models.DateTimeObject.HasMonth")]
[assembly: SuppressMessage("Performance", "CA1805:Do not initialize unnecessarily", Justification = "Hackathon", Scope = "member", Target = "~P:Microsoft.Health.Anonymizer.Common.Models.DateTimeObject.HasSecond")]
[assembly: SuppressMessage("Performance", "CA1805:Do not initialize unnecessarily", Justification = "Hackathon", Scope = "member", Target = "~P:Microsoft.Health.Anonymizer.Common.Models.DateTimeObject.HasTimeZone")]
[assembly: SuppressMessage("Performance", "CA1805:Do not initialize unnecessarily", Justification = "Hackathon", Scope = "member", Target = "~P:Microsoft.Health.Anonymizer.Common.Models.DateTimeObject.HasYear")]
[assembly: SuppressMessage("Security", "CA5350:Do Not Use Weak Cryptographic Algorithms", Justification = "Hackathon", Scope = "member", Target = "~M:Microsoft.Health.Anonymizer.Common.CryptoHashFunction.#ctor(Microsoft.Health.Anonymizer.Common.Settings.CryptoHashSetting)")]
[assembly: SuppressMessage("Style", "IDE0073:The file header does not match the required text", Justification = "Hackathon")]
[assembly: SuppressMessage("Design", "CA1002:Do not expose generic lists", Justification = "Hackathon", Scope = "member", Target = "~P:Microsoft.Health.Anonymizer.Common.Settings.RedactSetting.RestrictedZipCodeTabulationAreas")]
[assembly: SuppressMessage("Security", "CA5351:Do Not Use Broken Cryptographic Algorithms", Justification = "Hackathon", Scope = "member", Target = "~M:Microsoft.Health.Anonymizer.Common.CryptoHashFunction.#ctor(Microsoft.Health.Anonymizer.Common.Settings.CryptoHashSetting)")]
[assembly: SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Hackathon", Scope = "member", Target = "~M:Microsoft.Health.Anonymizer.Common.Settings.EncryptSetting.GetEncryptByteKey~System.Byte[]")]
[assembly: SuppressMessage("Design", "CA1044:Properties should not be write only", Justification = "Hackathon", Scope = "member", Target = "~P:Microsoft.Health.Anonymizer.Common.Settings.EncryptSetting.EncryptKey")]
[assembly: SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "Hackathon", Scope = "member", Target = "~P:Microsoft.Health.Anonymizer.Common.Settings.RedactSetting.RestrictedZipCodeTabulationAreas")]
[assembly: SuppressMessage("Design", "CA1044:Properties should not be write only", Justification = "Hackathon", Scope = "member", Target = "~P:Microsoft.Health.Anonymizer.Common.Settings.CryptoHashSetting.CryptoHashKey")]
[assembly: SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Hackathon", Scope = "member", Target = "~M:Microsoft.Health.Anonymizer.Common.Settings.CryptoHashSetting.GetCryptoHashByteKey~System.Byte[]")]
[assembly: SuppressMessage("Design", "CA1052:Static holder types should be Static or NotInheritable", Justification = "Hackathon", Scope = "type", Target = "~T:Microsoft.Health.Anonymizer.Common.Utilities.DateTimeUtility")]
