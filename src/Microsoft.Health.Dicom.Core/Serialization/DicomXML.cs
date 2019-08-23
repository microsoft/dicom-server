#pragma warning disable
// This is the combination of 2 file. The base file is from fo-dicom:
//
// Copyright (c) 2012-2019 fo-dicom contributors.
// Licensed under the Microsoft Public License (MS-PL).
// Note: This is a copy of fo-dicom:4.01/Serialization/DicomXML.cs
//
// Several methods for XML deserialization have been added from Zaid Safadi's DICOMcloud:
// Liscensed under the Apache License 2.0
// Source: https://github.com/Zaid-Safadi/DICOMcloud/blob/master/DICOMcloud/DICOMcloud/XmlDicomConverter.cs
#pragma warning enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Dicom;
using Dicom.IO.Buffer;

namespace Microsoft.Health.Dicom.Core
{
    /// <summary>
    /// Does the conversion of <see cref="Dicom.DicomDataset"/> to an XML string
    /// </summary>
    public static class DicomXML
    {
        public static XmlWriterSettings XmlSettings { get; set; }

        public static DicomTransferSyntax TransferSyntax { get; set; }

        #region Public methods

        /// <summary>
        /// Converts a <see cref="DicomDataset"/> to a XML-String
        /// </summary>
        /// <param name="dataset">The DicomDataset that is converted to XML-String</param>
        /// <param name="settings">Settings for the XMLWriter.</param>
        public static string ConvertDicomToXML(DicomDataset dataset, XmlWriterSettings settings = null)
        {
            if (settings == null)
            {
                settings = new XmlWriterSettings();
                settings.Encoding = new UTF8Encoding(false);
                settings.Indent = true;
            }

            XmlSettings = settings;
            string xmlString = DicomToXml(dataset);
            return xmlString;
        }

        /// <summary>
        /// Convert a XML-String to a <see cref="DicomDataset"/>/>
        /// </summary>
        /// <param name="xmlString">The XML-String to convert to a <see cref="DicomDataset"/></param>
        /// <returns>The <see cref="DicomDataset"/> created from the xml</returns>
        public static DicomDataset ConvertXMLToDicom(string xmlString)
        {
            DicomDataset dataset = new DicomDataset();
            XDocument document = XDocument.Parse(xmlString);

            ReadChildren(dataset, null, document.Root, 0);

            return dataset;
        }

        /// <summary>
        /// Converts the <see cref="DicomDataset"/> into an XML string.
        /// </summary>
        /// <param name="dataset">Dataset to serialize.</param>
        /// <returns>An XML string.</returns>
        public static string WriteToXml(this DicomDataset dataset)
        {
            return ConvertDicomToXML(dataset);
        }

        /// <summary>
        /// Converts the <see cref="DicomFile"/> into an XML string.
        /// </summary>
        /// <param name="file">The DicomFile to convert to XML</param>
        /// <returns>An XML string.</returns>
        public static string WriteToXml(this DicomFile file)
        {
            return ConvertDicomToXML(file.Dataset);
        }

        #endregion

        #region Private Methods

        private static string DicomToXml(DicomDataset dataset)
        {
            var xmlOutput = new StringBuilder();
            xmlOutput.AppendLine(@"<?xml version=""1.0"" encoding=""utf-8""?>");
            xmlOutput.AppendLine(@"<NativeDicomModel>");

            DicomDatasetToXml(xmlOutput, dataset);

            xmlOutput.AppendLine(@"</NativeDicomModel>");
            return xmlOutput.ToString();
        }

        private static void DicomDatasetToXml(StringBuilder xmlOutput, DicomDataset dataset)
        {
            foreach (var item in dataset)
            {
                if (item is DicomElement)
                {
                    DicomElementToXml(xmlOutput, (DicomElement)item);
                }
                else if (item is DicomSequence)
                {
                    var sq = item as DicomSequence;

                    WriteDicomAttribute(xmlOutput, sq);

                    int itemNum = 0;
                    foreach (DicomDataset d in sq.Items)
                    {
                        if (d != null)
                        {
                            xmlOutput.AppendLine($@"<Item number=""{itemNum + 1}"">");
                            DicomDatasetToXml(xmlOutput, d);
                            xmlOutput.AppendLine(@"</Item>");
                            itemNum++;
                        }
                    }

                    xmlOutput.AppendLine(@"</DicomAttribute>");
                }
            }
        }

        private static void DicomElementToXml(StringBuilder xmlOutput, DicomElement item)
        {
            WriteDicomAttribute(xmlOutput, item);

            var vr = item.ValueRepresentation.Code;

            if (vr == DicomVRCode.OB || vr == DicomVRCode.OD || vr == DicomVRCode.OF || vr == DicomVRCode.OW ||
                vr == DicomVRCode.OL || vr == DicomVRCode.UN)
            {
                var binaryString = GetBinaryBase64(item);
                xmlOutput.AppendLine($@"<InlineBinary>{binaryString}</InlineBinary>");
            }
            else if (vr == DicomVRCode.PN)
            {
                for (int i = 0; i < item.Count; i++)
                {
                    xmlOutput.AppendLine($@"<PersonName number=""{i + 1}"">");
                    xmlOutput.AppendLine(@"<Alphabetic>");

                    DicomPersonName person = new DicomPersonName(item.Tag, item.Get<string>(i));

                    string lastName = person.Last;
                    if (!string.IsNullOrEmpty(lastName))
                    {
                        xmlOutput.AppendLine($@"<FamilyName>{EscapeXml(lastName)}</FamilyName>");
                    }

                    string givenName = person.First;
                    if (!string.IsNullOrEmpty(givenName))
                    {
                        xmlOutput.AppendLine($@"<GivenName>{EscapeXml(givenName)}</GivenName>");
                    }

                    string middleName = person.Middle;
                    if (!string.IsNullOrEmpty(middleName))
                    {
                        xmlOutput.AppendLine($@"<MiddleName>{EscapeXml(middleName)}</MiddleName>");
                    }

                    string prefixName = person.Prefix;
                    if (!string.IsNullOrEmpty(prefixName))
                    {
                        xmlOutput.AppendLine($@"<NamePrefix>{EscapeXml(prefixName)}</NamePrefix>");
                    }

                    string suffixName = person.Suffix;
                    if (!string.IsNullOrEmpty(suffixName))
                    {
                        xmlOutput.AppendLine($@"<NameSuffix>{EscapeXml(suffixName)}</NameSuffix>");
                    }

                    xmlOutput.AppendLine(@"</Alphabetic>");
                    xmlOutput.AppendLine(@"</PersonName>");
                }
            }
            else
            {
                for (int i = 0; i < item.Count; i++)
                {
                    var valueString = EscapeXml(item.Get<string>(i));
                    xmlOutput.AppendLine($@"<Value number=""{i + 1}"">{valueString}</Value>");
                }
            }

            xmlOutput.AppendLine(@"</DicomAttribute>");
        }

        private static void WriteDicomAttribute(StringBuilder xmlOutput, DicomItem item)
        {
            if (item.Tag.IsPrivate && item.Tag.PrivateCreator != null)
            {
                xmlOutput.AppendLine($@"<DicomAttribute tag=""{item.Tag.Group:X4}{item.Tag.Element:X4}"" vr=""{item.ValueRepresentation.Code}"" keyword=""{item.Tag.DictionaryEntry.Keyword}"" privateCreator=""{item.Tag.PrivateCreator.Creator}"">");
            }
            else
            {
                xmlOutput.AppendLine($@"<DicomAttribute tag=""{item.Tag.Group:X4}{item.Tag.Element:X4}"" vr=""{item.ValueRepresentation.Code}"" keyword=""{item.Tag.DictionaryEntry.Keyword}"">");
            }
        }

        private static string GetBinaryBase64(DicomElement item)
        {
            IByteBuffer buffer = item.Buffer;
            if (buffer == null)
            {
                return string.Empty;
            }

            return Convert.ToBase64String(buffer.Data);
        }

        private static string EscapeXml(string text)
        {
            return text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;").Replace("'", "&apos;");
        }

        private static void ReadChildren(DicomDataset dataset, DicomTransferSyntax ts, XContainer document, int level = 0)
        {
            foreach (XElement element in document.Elements("DicomAttribute"))
            {
                ReadDicomAttribute(dataset, ts, element, level);
            }
        }

        private static void ReadDicomAttribute(DicomDataset dataset, DicomTransferSyntax ts, XElement element, int level)
        {
            XAttribute vrAttribute = element.Attribute("vr");
            DicomTag tag = DicomTag.Parse(element.Attribute("tag").Value);
            DicomVR valueRep = null;
            if (vrAttribute != null && !string.IsNullOrEmpty(vrAttribute.Value))
            {
                valueRep = DicomVR.Parse(vrAttribute.Value);
            }

            if (tag.IsPrivate)
            {
                if (tag.DictionaryEntry.Keyword == "unknown")
                {
                    if (element.Attribute("keyword") != null)
                    {
                        tag = new DicomTag(tag.Group, tag.Element, element.Attribute("keyword").Value);
                    }
                }

                tag = dataset.GetPrivateTag(tag);

                if (vrAttribute != null)
                {
                    valueRep = DicomVR.Parse(vrAttribute.Value);
                }

                if (element.Attribute("privateCreator") != null)
                {
                    tag.PrivateCreator = new DicomPrivateCreator(element.Attribute("privateCreator").Value);
                }
            }

            if (valueRep == null)
            {
                DicomDictionaryEntry dictionaryEntry = DicomDictionary.Default[tag];
                valueRep = dictionaryEntry.ValueRepresentations.FirstOrDefault();
            }

            if (valueRep == DicomVR.SQ)
            {
                ReadSequence(dataset, ts, element, tag, level);
            }
            else
            {
                ReadElement(dataset, ts, element, tag, valueRep, level);
            }
        }

        private static void ReadSequence(DicomDataset dataset, DicomTransferSyntax ts, XElement element, DicomTag tag, int level)
        {
            DicomSequence sequence = new DicomSequence(tag, Array.Empty<DicomDataset>());

            foreach (XElement item in element.Elements("Item"))
            {
                DicomDataset childDataset = new DicomDataset();
                level++;
                ReadChildren(childDataset, ts, item, level);
                level--;
                sequence.Items.Add(childDataset);
            }

            dataset.AddOrUpdate(sequence);
        }

        private static void ReadElement(DicomDataset dataset, DicomTransferSyntax ts, XElement element, DicomTag tag, DicomVR valueRep, int level)
        {
            switch (valueRep.Code)
            {
                case DicomVRCode.PN:
                    StringBuilder personNameBuilder = new StringBuilder();
                    StringBuilder pendingChars = new StringBuilder();
                    foreach (XElement personNameElementValue in element.Elements())
                    {
                        foreach (XElement personNameComponent in personNameElementValue.Elements().OrderBy(n =>
                        {
                            if (n.Attribute("number") == null)
                            {
                                return new XAttribute("number", 0);
                            }

                            return n.Attribute("number");
                        }))
                        {
                            if (personNameComponent.Name == "Alphabetic" ||
                                personNameComponent.Name == "Ideographic" ||
                                personNameComponent.Name == "Phonetic")
                            {
                                UpdatePersonName(personNameBuilder, pendingChars, personNameComponent, "FamilyName");
                                UpdatePersonName(personNameBuilder, pendingChars, personNameComponent, "GivenName");
                                UpdatePersonName(personNameBuilder, pendingChars, personNameComponent, "MiddleName");
                                UpdatePersonName(personNameBuilder, pendingChars, personNameComponent, "NamePrefix");
                                UpdatePersonName(personNameBuilder, pendingChars, personNameComponent, "NameSuffix", true);
                                pendingChars.Append('=');
                            }
                        }

                        pendingChars.Append('\\');
                    }

                    dataset.AddOrUpdate<string>(valueRep, tag, personNameBuilder.ToString());
                    break;
                case DicomVRCode.OB:
                case DicomVRCode.OD:
                case DicomVRCode.OF:
                case DicomVRCode.OW:
                case DicomVRCode.OL:
                case DicomVRCode.UN:
                    var dataElement = element.Elements().OfType<XElement>().FirstOrDefault();
                    if (dataElement != null)
                    {
                        IByteBuffer data;
                        if (dataElement.Name == "BulkData")
                        {
                            string uri = dataElement.Attribute("uri").Value;
                            data = new BulkDataUriByteBuffer(uri);
                        }
                        else
                        {
                            var base64 = Convert.FromBase64String(dataElement.Value);
                            data = new MemoryByteBuffer(base64);
                        }

                        if (tag == DicomTag.PixelData && level == 0)
                        {
                            dataset.AddOrUpdatePixelData(valueRep, data, TransferSyntax);
                        }
                        else
                        {
                            dataset.AddOrUpdate<IByteBuffer>(valueRep, tag, data);
                        }
                    }

                    break;
                default:
                    var values = ReadValue(element);
                    if (tag == DicomTag.TransferSyntaxUID)
                    {
                        ts = DicomTransferSyntax.Parse(values.FirstOrDefault());
                    }

                    dataset.AddOrUpdate<string>(valueRep, tag, values.ToArray());
                    break;
            }
        }

        private static IList<string> ReadValue(XElement element)
        {
            SortedList<int, string> values = new SortedList<int, string>();

            foreach (var valueElement in element.Elements("Value"))
            {
                values.Add(
                    int.Parse(valueElement.Attribute("number").Value),
                    valueElement.Value);
            }

            return values.Values;
        }

        private static void UpdatePersonName(StringBuilder personNameBuilder, StringBuilder pendingChars, XElement personNameComponent, string partName, bool isLastPart = false)
        {
            XElement partElement = personNameComponent.Element(partName);

            if (partElement != null)
            {
                personNameBuilder.PatientNameAdd(pendingChars, partElement.Value);
            }

            if (!isLastPart)
            {
                pendingChars.Append('^');
            }
        }
        
        private static void PatientNameAdd(this StringBuilder patientNameBuilder, StringBuilder pendingChars, string text)
        {
            if (!string.IsNullOrEmpty(text))
            {
                patientNameBuilder.Append(pendingChars.ToString());
                patientNameBuilder.Append(text);
                pendingChars.Clear();
            }
        }
        #endregion
    }
}
