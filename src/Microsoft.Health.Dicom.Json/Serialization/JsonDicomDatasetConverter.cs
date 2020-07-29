// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Dicom;
using Dicom.IO.Buffer;
using Microsoft.Health.Dicom.Json.Serialization.Parsers;

namespace Microsoft.Health.Dicom.Json.Serialization
{
    /// <summary>
    /// Converts a DicomDataset object to and from JSON using the NewtonSoft Json.NET library
    /// </summary>
    public class JsonDicomDatasetConverter : JsonConverter<DicomDataset>
    {
        private static readonly Dictionary<string, IDicomItemParser> DicomItemParserLookup = new Dictionary<string, IDicomItemParser>()
        {
            { DicomVR.AE.Code, new StringMultiValueDicomItemParser<DicomApplicationEntity>() },
            { DicomVR.AS.Code, new StringMultiValueDicomItemParser<DicomAgeString>() },
            { DicomVR.AT.Code, new AttributeTagMultiValueDicomItemParser() },
            { DicomVR.CS.Code, new StringMultiValueDicomItemParser<DicomCodeString>() },
            { DicomVR.DA.Code, new StringMultiValueDicomItemParser<DicomDate>() },
            { DicomVR.DS.Code, new StringMultiValueDicomItemParser<DicomDecimalString>(supportBulkDataUri: true) },
            { DicomVR.DT.Code, new StringMultiValueDicomItemParser<DicomDateTime>() },
            { DicomVR.FD.Code, new NumberMultiValueDicomItemParser<DicomFloatingPointDouble, double>(element => element.GetDouble()) },
            { DicomVR.FL.Code, new NumberMultiValueDicomItemParser<DicomFloatingPointSingle, float>(element => element.GetSingle()) },
            { DicomVR.IS.Code, new NumberMultiValueDicomItemParser<DicomIntegerString, int>(element => element.GetInt32()) },
            { DicomVR.LO.Code, new StringMultiValueDicomItemParser<DicomLongString>() },
            { DicomVR.LT.Code, new StringSingleValueDicomItemParser<DicomLongText>() },
            { DicomVR.OB.Code, new OtherDicomItemParser<DicomOtherDouble>() },
            { DicomVR.OD.Code, new OtherDicomItemParser<DicomOtherDouble>() },
            { DicomVR.OF.Code, new OtherDicomItemParser<DicomOtherFloat>() },
            { DicomVR.OL.Code, new OtherDicomItemParser<DicomOtherLong>() },
            { DicomVR.OW.Code, new OtherDicomItemParser<DicomOtherWord>() },
            { DicomVR.OV.Code, new OtherDicomItemParser<DicomOtherVeryLong>() },
            { DicomVR.PN.Code, null },
            { DicomVR.SH.Code, new StringMultiValueDicomItemParser<DicomShortString>() },
            { DicomVR.SL.Code, new NumberMultiValueDicomItemParser<DicomSignedLong, int>(element => element.GetInt32()) },
            { DicomVR.SQ.Code, null },
            { DicomVR.SS.Code, new NumberMultiValueDicomItemParser<DicomSignedShort, short>(element => element.GetInt16()) },
            { DicomVR.ST.Code, new StringSingleValueDicomItemParser<DicomShortText>() }, // Why is this one FirstOrEmpty instead of SingleOrDefault?
            { DicomVR.SV.Code, new NumberMultiValueDicomItemParser<DicomSignedVeryLong, long>(element => element.GetInt64()) },
            { DicomVR.TM.Code, new StringMultiValueDicomItemParser<DicomTime>() },
            { DicomVR.UC.Code, new StringSingleValueDicomItemParser<DicomUnlimitedCharacters>() }, // Why is this one SingleOrDefault?
            { DicomVR.UI.Code, new StringMultiValueDicomItemParser<DicomUniqueIdentifier>() },
            { DicomVR.UL.Code, new NumberMultiValueDicomItemParser<DicomUnsignedLong, uint>(element => element.GetUInt32()) },
            { DicomVR.UN.Code, null },
            { DicomVR.UR.Code, new StringSingleValueDicomItemParser<DicomUniversalResource>() }, // Why SingleOrEmpty?
            { DicomVR.US.Code, new NumberMultiValueDicomItemParser<DicomUnsignedShort, ushort>(element => element.GetUInt16()) },
            { DicomVR.UT.Code, new StringSingleValueDicomItemParser<DicomUnlimitedText>() }, // SingleOrEmpty?
            { DicomVR.UV.Code, new NumberMultiValueDicomItemParser<DicomUnsignedVeryLong, ulong>(element => element.GetUInt64()) },
        };

        ////private static readonly Encoding _jsonTextEncoding = Encoding.UTF8;
        private readonly bool _writeTagsAsKeywords;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonDicomDatasetConverter"/> class.
        /// </summary>
        /// <param name="writeTagsAsKeywords">Whether to write the json keys as DICOM keywords instead of tags. This makes the json non-compliant to DICOM JSON.</param>
        public JsonDicomDatasetConverter(bool writeTagsAsKeywords = false)
        {
            _writeTagsAsKeywords = writeTagsAsKeywords;
        }

        private delegate void WriterDelegate<T>(T value);

        private delegate object ReadMultiValueDelegate(JsonElement element);

        /// <inheritdoc/>/>
        public override void Write(Utf8JsonWriter writer, DicomDataset value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartObject();

            foreach (DicomItem item in value)
            {
                if (((uint)item.Tag & 0xffff) == 0)
                {
                    // Group length (gggg,0000) attributes shall not be included in a DICOM JSON Model object.
                    continue;
                }

                // Unknown or masked tags cannot be written as keywords
                var unknown = item.Tag.DictionaryEntry == null
                              || string.IsNullOrWhiteSpace(item.Tag.DictionaryEntry.Keyword)
                              ||
                              (item.Tag.DictionaryEntry.MaskTag != null &&
                               item.Tag.DictionaryEntry.MaskTag.Mask != 0xffffffff);

                if (_writeTagsAsKeywords && !unknown)
                {
                    writer.WritePropertyName(item.Tag.DictionaryEntry.Keyword);
                }
                else
                {
                    writer.WritePropertyName(item.Tag.Group.ToString("X4") + item.Tag.Element.ToString("X4"));
                }

                WriteJsonDicomItem(writer, item, options);
            }

            writer.WriteEndObject();
        }

        /// <inheritdoc/>
        public override DicomDataset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (!JsonDocument.TryParseValue(ref reader, out JsonDocument document))
            {
                return null;
            }

            var dataset = new DicomDataset();

            JsonElement rootElement = document.RootElement;

            foreach (JsonProperty property in rootElement.EnumerateObject())
            {
                DicomTag tag = ParseTag(property.Name);
                DicomItem item = ReadJsonDicomItem(tag, property.Value);

                dataset.Add(item);
            }

            foreach (DicomItem item in dataset)
            {
                if (item.Tag.IsPrivate && ((item.Tag.Element & 0xff00) != 0))
                {
                    var privateCreatorTag = new DicomTag(item.Tag.Group, (ushort)(item.Tag.Element >> 8));

                    if (dataset.Contains(privateCreatorTag))
                    {
                        item.Tag.PrivateCreator = new DicomPrivateCreator(dataset.GetSingleValue<string>(privateCreatorTag));
                    }
                }
            }

            return dataset;
        }

        /// <summary>
        /// Determines whether this instance can convert the specified object type.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <returns>
        /// <c>true</c> if this instance can convert the specified object type; otherwise, <c>false</c>.
        /// </returns>
        public override bool CanConvert(Type objectType)
        {
            return typeof(DicomDataset).GetTypeInfo().IsAssignableFrom(objectType.GetTypeInfo());
        }

        /////// <summary>
        /////// Create an instance of a IBulkDataUriByteBuffer. Override this method to use a different IBulkDataUriByteBuffer implementation in applications.
        /////// </summary>
        /////// <param name="bulkDataUri">The URI of a bulk data element as defined in <see cref="!:http://dicom.nema.org/medical/dicom/current/output/chtml/part19/chapter_A.html#table_A.1.5-2">Table A.1.5-2 in PS3.19</see>.</param>
        /////// <returns>An instance of a Bulk URI Byte buffer.</returns>
        ////protected virtual IBulkDataUriByteBuffer CreateBulkDataUriByteBuffer(string bulkDataUri)
        ////{
        ////    return new BulkDataUriByteBuffer(bulkDataUri);
        ////}

        internal static DicomTag ParseTag(string tagstr)
        {
            if (Regex.IsMatch(tagstr, @"\A\b[0-9a-fA-F]+\b\Z"))
            {
                var group = Convert.ToUInt16(tagstr.Substring(0, 4), 16);
                var element = Convert.ToUInt16(tagstr.Substring(4), 16);
                var tag = new DicomTag(group, element);
                return tag;
            }

            return DicomDictionary.Default[tagstr];
        }

        private static DicomItem ReadJsonDicomItem(DicomTag tag, JsonElement element)
        {
            if (!element.TryGetProperty("vr", out JsonElement valueRepresentationElement))
            {
                throw new Exception("Malformed.");
            }

            string vr = valueRepresentationElement.GetString();

            DicomItem item;

            switch (vr)
            {
                case "AE":
                    item = new DicomApplicationEntity(tag, (string[])data);
                    break;
                case "AS":
                    item = new DicomAgeString(tag, (string[])data);
                    break;
                case "AT":
                    item = new DicomAttributeTag(tag, ((string[])data).Select(ParseTag).ToArray());
                    break;
                case "CS":
                    item = new DicomCodeString(tag, (string[])data);
                    break;
                case "DA":
                    item = new DicomDate(tag, (string[])data);
                    break;
                case "DS":
                    if (data is IByteBuffer dataBufferDS)
                    {
                        item = new DicomDecimalString(tag, dataBufferDS);
                    }
                    else
                    {
                        item = new DicomDecimalString(tag, (string[])data);
                    }

                    break;
                case "DT":
                    item = new DicomDateTime(tag, (string[])data);
                    break;
                case "FD":
                    if (data is IByteBuffer dataBufferFD)
                    {
                        item = new DicomFloatingPointDouble(tag, dataBufferFD);
                    }
                    else
                    {
                        item = new DicomFloatingPointDouble(tag, (double[])data);
                    }

                    break;
                case "FL":
                    if (data is IByteBuffer dataBufferFL)
                    {
                        item = new DicomFloatingPointSingle(tag, dataBufferFL);
                    }
                    else
                    {
                        item = new DicomFloatingPointSingle(tag, (float[])data);
                    }

                    break;
                case "IS":
                    if (data is IByteBuffer dataBufferIS)
                    {
                        item = new DicomIntegerString(tag, dataBufferIS);
                    }
                    else
                    {
                        item = new DicomIntegerString(tag, (int[])data);
                    }

                    break;
                case "LO":
                    item = new DicomLongString(tag, (string[])data);
                    break;
                case "LT":
                    if (data is IByteBuffer dataBufferLT)
                    {
                        item = new DicomLongText(tag, _jsonTextEncoding, dataBufferLT);
                    }
                    else
                    {
                        item = new DicomLongText(tag, _jsonTextEncoding, data.AsStringArray().SingleOrEmpty());
                    }

                    break;
                case "OB":
                    item = new DicomOtherByte(tag, (IByteBuffer)data);
                    break;
                case "OD":
                    item = new DicomOtherDouble(tag, (IByteBuffer)data);
                    break;
                case "OF":
                    item = new DicomOtherFloat(tag, (IByteBuffer)data);
                    break;
                case "OL":
                    item = new DicomOtherLong(tag, (IByteBuffer)data);
                    break;
                case "OW":
                    item = new DicomOtherWord(tag, (IByteBuffer)data);
                    break;
                case "OV":
                    item = new DicomOtherVeryLong(tag, (IByteBuffer)data);
                    break;
                case "PN":
                    item = new DicomPersonName(tag, (string[])data);
                    break;
                case "SH":
                    item = new DicomShortString(tag, (string[])data);
                    break;
                case "SL":
                    if (data is IByteBuffer dataBufferSL)
                    {
                        item = new DicomSignedLong(tag, dataBufferSL);
                    }
                    else
                    {
                        item = new DicomSignedLong(tag, (int[])data);
                    }

                    break;
                case "SQ":
                    item = new DicomSequence(tag, (DicomDataset[])data);
                    break;
                case "SS":
                    if (data is IByteBuffer dataBufferSS)
                    {
                        item = new DicomSignedShort(tag, dataBufferSS);
                    }
                    else
                    {
                        item = new DicomSignedShort(tag, (short[])data);
                    }

                    break;
                case "ST":
                    if (data is IByteBuffer dataBufferST)
                    {
                        item = new DicomShortText(tag, _jsonTextEncoding, dataBufferST);
                    }
                    else
                    {
                        item = new DicomShortText(tag, _jsonTextEncoding, data.AsStringArray().FirstOrEmpty());
                    }

                    break;
                case "SV":
                    if (data is IByteBuffer dataBufferSV)
                    {
                        item = new DicomSignedVeryLong(tag, dataBufferSV);
                    }
                    else
                    {
                        item = new DicomSignedVeryLong(tag, (long[])data);
                    }

                    break;
                case "TM":
                    item = new DicomTime(tag, (string[])data);
                    break;
                case "UC":
                    if (data is IByteBuffer dataBufferUC)
                    {
                        item = new DicomUnlimitedCharacters(tag, _jsonTextEncoding, dataBufferUC);
                    }
                    else
                    {
                        item = new DicomUnlimitedCharacters(tag, _jsonTextEncoding, data.AsStringArray().SingleOrDefault());
                    }

                    break;
                case "UI":
                    item = new DicomUniqueIdentifier(tag, (string[])data);
                    break;
                case "UL":
                    if (data is IByteBuffer dataBufferUL)
                    {
                        item = new DicomUnsignedLong(tag, dataBufferUL);
                    }
                    else
                    {
                        item = new DicomUnsignedLong(tag, (uint[])data);
                    }

                    break;
                case "UN":
                    item = new DicomUnknown(tag, (IByteBuffer)data);
                    break;
                case "UR":
                    item = new DicomUniversalResource(tag, data.AsStringArray().SingleOrEmpty());
                    break;
                case "US":
                    if (data is IByteBuffer dataBufferUS)
                    {
                        item = new DicomUnsignedShort(tag, dataBufferUS);
                    }
                    else
                    {
                        item = new DicomUnsignedShort(tag, (ushort[])data);
                    }

                    break;
                case "UT":
                    if (data is IByteBuffer dataBufferUT)
                    {
                        item = new DicomUnlimitedText(tag, _jsonTextEncoding, dataBufferUT);
                    }
                    else
                    {
                        item = new DicomUnlimitedText(tag, _jsonTextEncoding, data.AsStringArray().SingleOrEmpty());
                    }

                    break;
                case "UV":
                    if (data is IByteBuffer dataBufferUV)
                    {
                        item = new DicomUnsignedVeryLong(tag, dataBufferUV);
                    }
                    else
                    {
                        item = new DicomUnsignedVeryLong(tag, (ulong[])data);
                    }

                    break;
                default:
                    throw new NotSupportedException("Unsupported value representation");
            }

            return item;
        }

        private void WriteJsonDicomItem(Utf8JsonWriter writer, DicomItem item, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("vr");
            writer.WriteStringValue(item.ValueRepresentation.Code);

            switch (item.ValueRepresentation.Code)
            {
                case "PN":
                    WriteJsonPersonName(writer, (DicomPersonName)item);
                    break;
                case "SQ":
                    WriteJsonSequence(writer, (DicomSequence)item, options);
                    break;
                case "OB":
                case "OD":
                case "OF":
                case "OL":
                case "OV":
                case "OW":
                case "UN":
                    WriteJsonOther(writer, (DicomElement)item);
                    break;
                case "FL":
                    WriteJsonElement<float>(writer, (DicomElement)item, writer.WriteNumberValue);
                    break;
                case "FD":
                    WriteJsonElement<double>(writer, (DicomElement)item, writer.WriteNumberValue);
                    break;
                case "IS":
                case "SL":
                    WriteJsonElement<int>(writer, (DicomElement)item, writer.WriteNumberValue);
                    break;
                case "SS":
                    WriteJsonElement<short>(writer, (DicomElement)item, (_, value) => writer.WriteNumberValue(value));
                    break;
                case "SV":
                    WriteJsonElement<long>(writer, (DicomElement)item, writer.WriteNumberValue);
                    break;
                case "UL":
                    WriteJsonElement<uint>(writer, (DicomElement)item, writer.WriteNumberValue);
                    break;
                case "US":
                    WriteJsonElement<ushort>(writer, (DicomElement)item, (_, value) => writer.WriteNumberValue(value));
                    break;
                case "UV":
                    WriteJsonElement<ulong>(writer, (DicomElement)item, writer.WriteNumberValue);
                    break;
                case "DS":
                    WriteJsonDecimalString(writer, (DicomElement)item);
                    break;
                case "AT":
                    WriteJsonAttributeTag(writer, (DicomElement)item);
                    break;
                default:
                    WriteJsonElement<string>(writer, (DicomElement)item, writer.WriteStringValue);
                    break;
            }

            writer.WriteEndObject();
        }

        private static void WriteJsonDecimalString(Utf8JsonWriter writer, DicomElement elem)
        {
            if (elem.Count != 0)
            {
                writer.WritePropertyName("Value");
                writer.WriteStartArray();
                foreach (var val in elem.Get<string[]>())
                {
                    if (string.IsNullOrEmpty(val))
                    {
                        writer.WriteNullValue();
                    }
                    else
                    {
                        var fix = FixDecimalString(val);
                        if (ulong.TryParse(fix, NumberStyles.Integer, CultureInfo.InvariantCulture, out ulong xulong))
                        {
                            writer.WriteNumberValue(xulong);
                        }
                        else if (long.TryParse(fix, NumberStyles.Integer, CultureInfo.InvariantCulture, out long xlong))
                        {
                            writer.WriteNumberValue(xlong);
                        }
                        else if (decimal.TryParse(fix, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal xdecimal))
                        {
                            writer.WriteNumberValue(xdecimal);
                        }
                        else if (double.TryParse(fix, NumberStyles.Float, CultureInfo.InvariantCulture, out double xdouble))
                        {
                            writer.WriteNumberValue(xdouble);
                        }
                        else
                        {
                            throw new FormatException($"Cannot write dicom number {val} to json");
                        }
                    }
                }

                writer.WriteEndArray();
            }
        }

        ////private static bool IsValidJsonNumber(string val)
        ////{
        ////    try
        ////    {
        ////        return true;
        ////    }
        ////    catch (Exception)
        ////    {
        ////        return false;
        ////    }
        ////}

        /// <summary>
        /// Fix-up a Dicom DS number for use with json.
        /// Rationale: There is a requirement that DS numbers shall be written as json numbers in part 18.F json, but the
        /// requirements on DS allows values that are not json numbers. This method "fixes" them to conform to json numbers.
        /// </summary>
        /// <param name="val">A valid DS value</param>
        /// <returns>A json number equivalent to the supplied DS value</returns>
        private static string FixDecimalString(string val)
        {
            return val;

            ////if (IsValidJsonNumber(val))
            ////{
            ////    return val;
            ////}

            ////if (string.IsNullOrWhiteSpace(val))
            ////{
            ////    return null;
            ////}

            ////val = val.Trim();

            ////var negative = false;

            ////// Strip leading superfluous plus signs
            ////if (val[0] == '+')
            ////{
            ////    val = val.Substring(1);
            ////}
            ////else if (val[0] == '-')
            ////{
            ////    // Temporarily remove negation sign for zero-stripping later
            ////    negative = true;
            ////    val = val.Substring(1);
            ////}

            ////// Strip leading superfluous zeros
            ////if (val.Length > 1 && val[0] == '0' && val[1] != '.')
            ////{
            ////    int i = 0;
            ////    while (i < val.Length - 1 && val[i] == '0' && val[i + 1] != '.')
            ////    {
            ////        i++;
            ////    }

            ////    val = val.Substring(i);
            ////}

            ////// Re-add negation sign
            ////if (negative)
            ////{
            ////    val = "-" + val;
            ////}

            ////return val;

            ////if (IsValidJsonNumber(val))
            ////{
            ////    return val;
            ////}

            ////throw new ArgumentException("Failed converting DS value to json");
        }

        private static void WriteJsonElement<T>(Utf8JsonWriter writer, DicomElement elem, WriterDelegate<T> valueWriter)
        {
            if (elem.Count != 0)
            {
                writer.WritePropertyName("Value");
                writer.WriteStartArray();

                foreach (var val in elem.Get<T[]>())
                {
                    if (val == null || (typeof(T) == typeof(string) && val.Equals(string.Empty)))
                    {
                        writer.WriteNullValue();
                    }
                    else
                    {
                        valueWriter(val);
                    }
                }

                writer.WriteEndArray();
            }
        }

        private static void WriteJsonElement<T>(Utf8JsonWriter writer, DicomElement elem, Action<Utf8JsonWriter, T> valueWriter)
        {
            if (elem.Count != 0)
            {
                writer.WritePropertyName("Value");
                writer.WriteStartArray();

                foreach (var val in elem.Get<T[]>())
                {
                    valueWriter(writer, val);
                }

                writer.WriteEndArray();
            }
        }

        private static void WriteJsonAttributeTag(Utf8JsonWriter writer, DicomElement elem)
        {
            if (elem.Count != 0)
            {
                writer.WritePropertyName("Value");
                writer.WriteStartArray();

                foreach (var val in elem.Get<DicomTag[]>())
                {
                    if (val == null)
                    {
                        writer.WriteNullValue();
                    }
                    else
                    {
                        writer.WriteStringValue(((uint)val).ToString("X8"));
                    }
                }

                writer.WriteEndArray();
            }
        }

        private static void WriteJsonOther(Utf8JsonWriter writer, DicomElement elem)
        {
            if (elem.Buffer is IBulkDataUriByteBuffer buffer)
            {
                writer.WritePropertyName("BulkDataURI");
                writer.WriteStringValue(buffer.BulkDataUri);
            }
            else if (elem.Count != 0)
            {
                writer.WritePropertyName("InlineBinary");
                writer.WriteStringValue(Convert.ToBase64String(elem.Buffer.Data));
            }
        }

        private void WriteJsonSequence(Utf8JsonWriter writer, DicomSequence seq, JsonSerializerOptions options)
        {
            if (seq.Items.Count != 0)
            {
                writer.WritePropertyName("Value");
                writer.WriteStartArray();

                foreach (var child in seq.Items)
                {
                    Write(writer, child, options);
                }

                writer.WriteEndArray();
            }
        }

        private static void WriteJsonPersonName(Utf8JsonWriter writer, DicomPersonName pn)
        {
            if (pn.Count != 0)
            {
                writer.WritePropertyName("Value");
                writer.WriteStartArray();

                foreach (var val in pn.Get<string[]>())
                {
                    if (string.IsNullOrEmpty(val))
                    {
                        writer.WriteNullValue();
                    }
                    else
                    {
                        writer.WriteStartObject();
                        writer.WritePropertyName("Alphabetic");
                        writer.WriteStringValue(val);
                        writer.WriteEndObject();
                    }
                }

                writer.WriteEndArray();
            }
        }

        ////private DicomItem ReadJsonDicomItem(DicomTag tag, JsonElement element)
        ////{
        ////    if (!element.TryGetProperty("vr", out JsonElement valueRepresentationElement))
        ////    {
        ////        throw new Exception("Malformed.");
        ////    }

        ////    string vr = valueRepresentationElement.GetString();

        ////    object data;

        ////    switch (vr)
        ////    {
        ////        case "OB":
        ////        case "OD":
        ////        case "OF":
        ////        case "OL":
        ////        case "OW":
        ////        case "OV":
        ////        case "UN":
        ////            data = ReadJsonOX(element);
        ////            break;
        ////        case "SQ":
        ////            data = ReadJsonSequence(element);
        ////            break;
        ////        case "PN":
        ////            data = ReadJsonPersonName(element);
        ////            break;
        ////        case "FL":
        ////            data = ReadJsonMultiValue(element, ReadJsonMultiNumberValue<float>);
        ////            break;
        ////        case "FD":
        ////            data = ReadJsonMultiNumber<double>(element);
        ////            break;
        ////        case "IS":
        ////            data = ReadJsonMultiNumber<int>(element);
        ////            break;
        ////        case "SL":
        ////            data = ReadJsonMultiNumber<int>(element);
        ////            break;
        ////        case "SS":
        ////            data = ReadJsonMultiNumber<short>(element);
        ////            break;
        ////        case "SV":
        ////            data = ReadJsonMultiNumber<long>(element);
        ////            break;
        ////        case "UL":
        ////            data = ReadJsonMultiNumber<uint>(element);
        ////            break;
        ////        case "US":
        ////            data = ReadJsonMultiNumber<ushort>(element);
        ////            break;
        ////        case "UV":
        ////            data = ReadJsonMultiNumber<ulong>(element);
        ////            break;
        ////        case "DS":
        ////            data = ReadJsonMultiValue(element, ReadJsonMultiStringValue);
        ////            break;
        ////        default:
        ////            data = ReadJsonMultiValue(element, ReadJsonMultiStringValue);
        ////            break;
        ////    }

        ////    DicomItem item = CreateDicomItem(tag, vr, data);
        ////    return item;
        ////}

        ////private object ReadJsonMultiValue(JsonElement element, ReadMultiValueDelegate readDelegate)
        ////{
        ////    if (element.TryGetProperty("Value", out JsonElement valueElement) &&
        ////        valueElement.ValueKind == JsonValueKind.Array)
        ////    {
        ////        return readDelegate(valueElement);
        ////    }
        ////    else if (element.TryGetProperty("BulkDataURI", out JsonElement bulkDataUriElement))
        ////    {
        ////        return ReadJsonBulkDataUri(bulkDataUriElement);
        ////    }
        ////    else
        ////    {
        ////        return Array.Empty<string>();
        ////    }
        ////}

        ////////private object ReadJsonMultiString(JsonElement element)
        ////////{
        ////////    if (element.TryGetProperty("Value", out JsonElement valueElement) &&
        ////////        valueElement.ValueKind == JsonValueKind.Array)
        ////////    {
        ////////        return ReadJsonMultiStringValue(valueElement);
        ////////    }
        ////////    else if (element.TryGetProperty("BulkDataURI", out JsonElement bulkDataUriElement))
        ////////    {
        ////////        return ReadJsonBulkDataUri(bulkDataUriElement);
        ////////    }
        ////////    else
        ////////    {
        ////////        return Array.Empty<string>();
        ////////    }
        ////////}

        ////private static string[] ReadJsonMultiStringValue(JsonElement element)
        ////{
        ////    var results = new string[element.GetArrayLength()];

        ////    int index = 0;

        ////    foreach (JsonElement arrayItem in element.EnumerateArray())
        ////    {
        ////        if (arrayItem.ValueKind == JsonValueKind.Null)
        ////        {
        ////            results[index] = null;
        ////        }
        ////        else
        ////        {
        ////            results[index] = arrayItem.GetString();
        ////        }

        ////        index++;
        ////    }

        ////    return results;
        ////}

        ////////private object ReadJsonMultiNumber<T>(JsonElement element)
        ////////{
        ////////    if (element.TryGetProperty("Value", out JsonElement valueElement) &&
        ////////        valueElement.ValueKind == JsonValueKind.Array)
        ////////    {
        ////////        return ReadJsonMultiNumberValue<T>(valueElement);
        ////////    }
        ////////    else if (element.TryGetProperty("BulkDataURI", out JsonElement bulkDataUriElement))
        ////////    {
        ////////        return ReadJsonBulkDataUri(bulkDataUriElement);
        ////////    }
        ////////    else
        ////////    {
        ////////        return Array.Empty<T>();
        ////////    }
        ////////}

        ////private static T[] ReadJsonMultiNumberValue<T>(JsonElement element)
        ////{
        ////    if (element.ValueKind != JsonValueKind.Array)
        ////    {
        ////        return Array.Empty<T>();
        ////    }

        ////    var childValues = new List<T>();
        ////    foreach (var item in tokens)
        ////    {
        ////        if (!(item.Type == JTokenType.Float || item.Type == JTokenType.Integer))
        ////        { throw new JsonReaderException("Malformed DICOM json"); }
        ////        childValues.Add((T)Convert.ChangeType(item.Value<object>(), typeof(T)));
        ////    }
        ////    var data = childValues.ToArray();
        ////    return data;
        ////}

        ////private string[] ReadJsonPersonName(JToken itemObject)
        ////{
        ////    if (itemObject["Value"] is JArray tokens)
        ////    {
        ////        var childStrings = new List<string>();
        ////        foreach (var item in tokens)
        ////        {
        ////            if (item.Type == JTokenType.Null)
        ////            {
        ////                childStrings.Add(null);
        ////            }
        ////            else
        ////            {
        ////                if (item["Alphabetic"] is JToken alphabetic)
        ////                {
        ////                    if (alphabetic.Type != JTokenType.String)
        ////                    { throw new JsonReaderException("Malformed DICOM json"); }
        ////                    childStrings.Add(alphabetic.Value<string>());
        ////                }
        ////            }
        ////        }
        ////        var data = childStrings.ToArray();
        ////        return data;
        ////    }
        ////    else
        ////    {
        ////        return new string[0];
        ////    }
        ////}

        ////private DicomDataset[] ReadJsonSequence(JToken itemObject)
        ////{
        ////    if (itemObject["Value"] is JArray items)
        ////    {
        ////        var childItems = new List<DicomDataset>();
        ////        foreach (var item in items)
        ////        {
        ////            childItems.Add(ReadJsonDataset(item));
        ////        }
        ////        var data = childItems.ToArray();
        ////        return data;
        ////    }
        ////    else
        ////    {
        ////        return new DicomDataset[0];
        ////    }
        ////}

        ////private IByteBuffer ReadJsonOX(JToken itemObject)
        ////{
        ////    if (itemObject["InlineBinary"] is JToken inline)
        ////    {
        ////        return ReadJsonInlineBinary(inline);
        ////    }
        ////    else if (itemObject["BulkDataURI"] is JToken bulk)
        ////    {
        ////        return ReadJsonBulkDataUri(bulk);
        ////    }
        ////    return EmptyBuffer.Value;
        ////}

        ////private static IByteBuffer ReadJsonInlineBinary(JToken token)
        ////{
        ////    if (token.Type != JTokenType.String)
        ////    { throw new JsonReaderException("Malformed DICOM json"); }
        ////    var data = new MemoryByteBuffer(Convert.FromBase64String(token.Value<string>()));
        ////    return data;
        ////}

        ////private IBulkDataUriByteBuffer ReadJsonBulkDataUri(JToken token)
        ////{
        ////    if (token.Type != JTokenType.String)
        ////    { throw new JsonReaderException("Malformed DICOM json"); }
        ////    var data = CreateBulkDataUriByteBuffer(token.Value<string>());
        ////    return data;
        ////}
    }

    ////internal static class JsonDicomConverterExtensions
    ////{
    ////    public static string[] AsStringArray(this object data) => (string[])data;

    ////    public static string FirstOrEmpty(this string[] array) => array.Length > 0 ? array[0] : string.Empty;

    ////    public static string SingleOrEmpty(this string[] array) => array.Length > 0 ? array.Single() : string.Empty;
    ////}
}
