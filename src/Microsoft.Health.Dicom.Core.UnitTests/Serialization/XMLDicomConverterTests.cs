#pragma warning disable
// These tests are based on the fo-dicom's DicomXMLTests with the following license:
//
// Copyright (c) 2012-2019 fo-dicom contributors.
// Licensed under the Microsoft Public License (MS-PL).
// Source: fo-dicom:4.01/Tests/Desktop/Serialization/XmlDicomConverterTest.cs
#pragma warning enable

using System;
using System.IO;
using System.Linq;
using System.Text;
using Dicom;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Serialization
{
    /// <summary>
    /// The xml dicom converter test.
    /// </summary>
    public class XMLDicomConverterTests
    {
        [Fact]
        public void TestSimpleXmlSerialization()
        {
            var dataset = BuildSimpleDataset();
            var finalXml = BuildSimpleXml();
            string xml = DicomXML.ConvertDicomToXML(dataset);
            Assert.True(!string.IsNullOrEmpty(xml));
            Assert.Equal(finalXml.ToString().Trim(), xml.Trim());
        }

        /// <summary>
        /// Tests a "triple trip" test of serializing, de-serializing and re-serializing for a DICOM dataset containing a zoo of different types.
        /// </summary>
        [Fact]
        public void SerializeAndDeserializeZoo()
        {
            DicomDataset target = BuildZooDataset();
            VerifyXmlTripleTrip(target);
        }

        [Fact]
        public void SerializeAndDeserializeSimple()
        {
            DicomDataset target = BuildZooDataset();
            VerifyXmlTripleTrip(target);
        }

        [Fact]
        public void SerializeAndDeserializeAllTypes()
        {
            DicomDataset target = BuildAllTypesDataset_();
            VerifyXmlTripleTrip(target);
        }

        [Fact]
        public void SerializeAndDeserializeAllTypesNull()
        {
            DicomDataset target = BuildAllTypesNullDataset_();
            VerifyXmlTripleTrip(target);
        }

        private void VerifyXmlTripleTrip(DicomDataset dataset)
        {
            string xml = DicomXML.ConvertDicomToXML(dataset);
            DicomDataset dataset2 = DicomXML.ConvertXMLToDicom(xml);
            string xml2 = DicomXML.ConvertDicomToXML(dataset2);

            StreamWriter writer = new StreamWriter("xml.xml", false);
            writer.Write(xml.Trim());
            writer.Close();

            StreamWriter writerTwo = new StreamWriter("xml2.xml", false);
            writerTwo.Write(xml2.Trim());
            writerTwo.Close();

            Assert.Equal(xml.Trim(), xml2.Trim());
        }

        private string BuildSimpleXml()
        {
            var finalXml = new StringBuilder();
            finalXml.AppendLine(@"<?xml version=""1.0"" encoding=""utf-8""?>");
            finalXml.AppendLine(@"<NativeDicomModel>");
            finalXml.AppendLine(@"<DicomAttribute tag=""00100010"" vr=""PN"" keyword=""PatientName"">");
            finalXml.AppendLine(@"<PersonName number=""1"">");
            finalXml.AppendLine(@"<Alphabetic>");
            finalXml.AppendLine(@"<FamilyName>Test</FamilyName>");
            finalXml.AppendLine(@"<GivenName>Name</GivenName>");
            finalXml.AppendLine(@"</Alphabetic>");
            finalXml.AppendLine(@"</PersonName>");
            finalXml.AppendLine(@"</DicomAttribute>");
            finalXml.AppendLine(@"<DicomAttribute tag=""0020000D"" vr=""UI"" keyword=""StudyInstanceUID"">");
            finalXml.AppendLine(@"<Value number=""1"">1.2.345</Value>");
            finalXml.AppendLine(@"</DicomAttribute>");
            finalXml.AppendLine(@"</NativeDicomModel>");
            return finalXml.ToString();
        }

        private DicomDataset BuildSimpleDataset()
        {
            var dataset = new DicomDataset();
            dataset.AddOrUpdate(DicomTag.StudyInstanceUID, "1.2.345");
            dataset.AddOrUpdate(DicomTag.PatientName, "Test^Name");
            return dataset;
        }

        private static DicomDataset BuildAllTypesDataset_()
        {
            var privateCreator = DicomDictionary.Default.GetPrivateCreator("TEST");
            var privDict = DicomDictionary.Default[privateCreator];

            privDict.Add(new DicomDictionaryEntry(DicomMaskedTag.Parse("0003", "xx02"), "Private Tag 02", "PrivateTag02", DicomVM.VM_1, false, DicomVR.AE));
            privDict.Add(new DicomDictionaryEntry(DicomMaskedTag.Parse("0003", "xx03"), "Private Tag 03", "PrivateTag03", DicomVM.VM_1, false, DicomVR.AS));
            privDict.Add(new DicomDictionaryEntry(DicomMaskedTag.Parse("0003", "xx04"), "Private Tag 04", "PrivateTag04", DicomVM.VM_1, false, DicomVR.AT));
            privDict.Add(new DicomDictionaryEntry(DicomMaskedTag.Parse("0003", "xx05"), "Private Tag 05", "PrivateTag05", DicomVM.VM_1, false, DicomVR.CS));
            privDict.Add(new DicomDictionaryEntry(DicomMaskedTag.Parse("0003", "xx06"), "Private Tag 06", "PrivateTag06", DicomVM.VM_1, false, DicomVR.DA));
            privDict.Add(new DicomDictionaryEntry(DicomMaskedTag.Parse("0003", "xx07"), "Private Tag 07", "PrivateTag07", DicomVM.VM_1, false, DicomVR.DS));
            privDict.Add(new DicomDictionaryEntry(DicomMaskedTag.Parse("0003", "xx08"), "Private Tag 08", "PrivateTag08", DicomVM.VM_1, false, DicomVR.DT));
            privDict.Add(new DicomDictionaryEntry(DicomMaskedTag.Parse("0003", "xx09"), "Private Tag 09", "PrivateTag09", DicomVM.VM_1, false, DicomVR.FL));
            privDict.Add(new DicomDictionaryEntry(DicomMaskedTag.Parse("0003", "xx0a"), "Private Tag 0a", "PrivateTag0a", DicomVM.VM_1, false, DicomVR.FD));
            privDict.Add(new DicomDictionaryEntry(DicomMaskedTag.Parse("0003", "xx0b"), "Private Tag 0b", "PrivateTag0b", DicomVM.VM_1, false, DicomVR.IS));
            privDict.Add(new DicomDictionaryEntry(DicomMaskedTag.Parse("0003", "xx0c"), "Private Tag 0c", "PrivateTag0c", DicomVM.VM_1, false, DicomVR.LO));
            privDict.Add(new DicomDictionaryEntry(DicomMaskedTag.Parse("0003", "xx0d"), "Private Tag 0d", "PrivateTag0d", DicomVM.VM_1, false, DicomVR.LT));
            privDict.Add(new DicomDictionaryEntry(DicomMaskedTag.Parse("0003", "xx0e"), "Private Tag 0e", "PrivateTag0e", DicomVM.VM_1, false, DicomVR.OB));
            privDict.Add(new DicomDictionaryEntry(DicomMaskedTag.Parse("0003", "xx0f"), "Private Tag 0f", "PrivateTag0f", DicomVM.VM_1, false, DicomVR.OD));
            privDict.Add(new DicomDictionaryEntry(DicomMaskedTag.Parse("0003", "xx10"), "Private Tag 10", "PrivateTag10", DicomVM.VM_1, false, DicomVR.OF));
            privDict.Add(new DicomDictionaryEntry(DicomMaskedTag.Parse("0003", "xx11"), "Private Tag 11", "PrivateTag11", DicomVM.VM_1, false, DicomVR.OL));
            privDict.Add(new DicomDictionaryEntry(DicomMaskedTag.Parse("0003", "xx12"), "Private Tag 12", "PrivateTag12", DicomVM.VM_1, false, DicomVR.OW));
            privDict.Add(new DicomDictionaryEntry(DicomMaskedTag.Parse("0003", "xx14"), "Private Tag 14", "PrivateTag14", DicomVM.VM_1, false, DicomVR.PN));
            privDict.Add(new DicomDictionaryEntry(DicomMaskedTag.Parse("0003", "xx15"), "Private Tag 15", "PrivateTag15", DicomVM.VM_1, false, DicomVR.SH));
            privDict.Add(new DicomDictionaryEntry(DicomMaskedTag.Parse("0003", "xx16"), "Private Tag 16", "PrivateTag16", DicomVM.VM_1, false, DicomVR.SL));
            privDict.Add(new DicomDictionaryEntry(DicomMaskedTag.Parse("0003", "xx17"), "Private Tag 17", "PrivateTag17", DicomVM.VM_1, false, DicomVR.SQ));
            privDict.Add(new DicomDictionaryEntry(DicomMaskedTag.Parse("0003", "xx18"), "Private Tag 18", "PrivateTag18", DicomVM.VM_1, false, DicomVR.ST));
            privDict.Add(new DicomDictionaryEntry(DicomMaskedTag.Parse("0003", "xx19"), "Private Tag 19", "PrivateTag19", DicomVM.VM_1, false, DicomVR.SS));
            privDict.Add(new DicomDictionaryEntry(DicomMaskedTag.Parse("0003", "xx1a"), "Private Tag 1a", "PrivateTag1a", DicomVM.VM_1, false, DicomVR.ST));
            privDict.Add(new DicomDictionaryEntry(DicomMaskedTag.Parse("0003", "xx1c"), "Private Tag 1c", "PrivateTag1c", DicomVM.VM_1, false, DicomVR.TM));
            privDict.Add(new DicomDictionaryEntry(DicomMaskedTag.Parse("0003", "xx1d"), "Private Tag 1d", "PrivateTag1d", DicomVM.VM_1, false, DicomVR.UC));
            privDict.Add(new DicomDictionaryEntry(DicomMaskedTag.Parse("0003", "xx1e"), "Private Tag 1e", "PrivateTag1e", DicomVM.VM_1, false, DicomVR.UI));
            privDict.Add(new DicomDictionaryEntry(DicomMaskedTag.Parse("0003", "xx1f"), "Private Tag 1f", "PrivateTag1f", DicomVM.VM_1, false, DicomVR.UL));
            privDict.Add(new DicomDictionaryEntry(DicomMaskedTag.Parse("0003", "xx20"), "Private Tag 20", "PrivateTag20", DicomVM.VM_1, false, DicomVR.UN));
            privDict.Add(new DicomDictionaryEntry(DicomMaskedTag.Parse("0003", "xx21"), "Private Tag 21", "PrivateTag21", DicomVM.VM_1, false, DicomVR.UR));
            privDict.Add(new DicomDictionaryEntry(DicomMaskedTag.Parse("0003", "xx22"), "Private Tag 22", "PrivateTag22", DicomVM.VM_1, false, DicomVR.US));
            privDict.Add(new DicomDictionaryEntry(DicomMaskedTag.Parse("0003", "xx23"), "Private Tag 23", "PrivateTag23", DicomVM.VM_1, false, DicomVR.UT));

            var ds = new DicomDataset();

            ds.Add(new DicomApplicationEntity(ds.GetPrivateTag(new DicomTag(3, 0x0002, privateCreator)), "AETITLE"));
            ds.Add(new DicomAgeString(ds.GetPrivateTag(new DicomTag(3, 0x0003, privateCreator)), "034Y"));
            ds.Add(new DicomAttributeTag(ds.GetPrivateTag(new DicomTag(3, 0x0004, privateCreator)), new[] { DicomTag.SOPInstanceUID }));
            ds.Add(new DicomCodeString(ds.GetPrivateTag(new DicomTag(3, 0x0005, privateCreator)), "FOOBAR"));
            ds.Add(new DicomDate(ds.GetPrivateTag(new DicomTag(3, 0x0006, privateCreator)), "20000229"));
            ds.Add(new DicomDecimalString(ds.GetPrivateTag(new DicomTag(3, 0x0007, privateCreator)), new[] { "9876543210123457" }));
            ds.Add(new DicomDateTime(ds.GetPrivateTag(new DicomTag(3, 0x0008, privateCreator)), "20141231194212"));
            ds.Add(new DicomFloatingPointSingle(ds.GetPrivateTag(new DicomTag(3, 0x0009, privateCreator)), new[] { 0.25f }));
            ds.Add(new DicomFloatingPointDouble(ds.GetPrivateTag(new DicomTag(3, 0x000a, privateCreator)), new[] { Math.PI }));
            ds.Add(new DicomIntegerString(ds.GetPrivateTag(new DicomTag(3, 0x000b, privateCreator)), 2147483647));
            ds.Add(new DicomLongString(ds.GetPrivateTag(new DicomTag(3, 0x000c, privateCreator)), "(╯°□°）╯︵ ┻━┻"));
            ds.Add(new DicomLongText(ds.GetPrivateTag(new DicomTag(3, 0x000d, privateCreator)), "┬──┬ ノ( ゜-゜ノ)"));
            ds.Add(new DicomOtherByte(ds.GetPrivateTag(new DicomTag(3, 0x000e, privateCreator)), new byte[] { 1, 2, 3, 0, 255 }));
            ds.Add(new DicomOtherDouble(ds.GetPrivateTag(new DicomTag(3, 0x000f, privateCreator)), new double[] { 1.0, 2.5 }));
            ds.Add(new DicomOtherFloat(ds.GetPrivateTag(new DicomTag(3, 0x0010, privateCreator)), new float[] { 1.0f, 2.9f }));
            ds.Add(new DicomOtherLong(ds.GetPrivateTag(new DicomTag(3, 0x0011, privateCreator)), new uint[] { 0xffffffff, 0x00000000, 0x12345678 }));
            ds.Add(new DicomOtherWord(ds.GetPrivateTag(new DicomTag(3, 0x0012, privateCreator)), new ushort[] { 0xffff, 0x0000, 0x1234 }));
            ds.Add(new DicomPersonName(ds.GetPrivateTag(new DicomTag(3, 0x0014, privateCreator)), "Morrison-Jones^Susan^^^Ph.D."));
            ds.Add(new DicomShortString(ds.GetPrivateTag(new DicomTag(3, 0x0015, privateCreator)), "顔文字"));
            ds.Add(new DicomSignedLong(ds.GetPrivateTag(new DicomTag(3, 0x0016, privateCreator)), -65538));
            ds.Add(new DicomSequence(ds.GetPrivateTag(new DicomTag(3, 0x0017, privateCreator)), new[] { new DicomDataset { new DicomShortText(new DicomTag(3, 0x0018, privateCreator), "ಠ_ಠ") } }));
            ds.Add(new DicomSignedShort(ds.GetPrivateTag(new DicomTag(3, 0x0019, privateCreator)), -32768));
            ds.Add(new DicomShortText(ds.GetPrivateTag(new DicomTag(3, 0x001a, privateCreator)), "ಠ_ಠ"));
            ds.Add(new DicomTime(ds.GetPrivateTag(new DicomTag(3, 0x001c, privateCreator)), "123456"));
            ds.Add(new DicomUnlimitedCharacters(ds.GetPrivateTag(new DicomTag(3, 0x001d, privateCreator)), "Hmph."));
            ds.Add(new DicomUniqueIdentifier(ds.GetPrivateTag(new DicomTag(3, 0x001e, privateCreator)), DicomUID.CTImageStorage));
            ds.Add(new DicomUnsignedLong(ds.GetPrivateTag(new DicomTag(3, 0x001f, privateCreator)), 0xffffffff));
            ds.Add(new DicomUnknown(ds.GetPrivateTag(new DicomTag(3, 0x0020, privateCreator)), new byte[] { 1, 2, 3, 0, 255 }));
            ds.Add(new DicomUniversalResource(ds.GetPrivateTag(new DicomTag(3, 0x0021, privateCreator)), "http://example.com?q=1"));
            ds.Add(new DicomUnsignedShort(ds.GetPrivateTag(new DicomTag(3, 0x0022, privateCreator)), 0xffff));
            ds.Add(new DicomUnlimitedText(ds.GetPrivateTag(new DicomTag(3, 0x0023, privateCreator)), "unlimited!"));

            return ds;
        }

        private static DicomDataset BuildAllTypesNullDataset_()
        {
            var privateCreator = DicomDictionary.Default.GetPrivateCreator("TEST");
            return new DicomDataset
            {
                new DicomApplicationEntity(new DicomTag(3, 0x1002, privateCreator)),
                new DicomAgeString(new DicomTag(3, 0x1003, privateCreator)),
                new DicomAttributeTag(new DicomTag(3, 0x1004, privateCreator)),
                new DicomCodeString(new DicomTag(3, 0x1005, privateCreator)),
                new DicomDate(new DicomTag(3, 0x1006, privateCreator), new string[0]),
                new DicomDecimalString(new DicomTag(3, 0x1007, privateCreator), new string[0]),
                new DicomDateTime(new DicomTag(3, 0x1008, privateCreator), new string[0]),
                new DicomFloatingPointSingle(new DicomTag(3, 0x1009, privateCreator)),
                new DicomFloatingPointDouble(new DicomTag(3, 0x100a, privateCreator)),
                new DicomIntegerString(new DicomTag(3, 0x100b, privateCreator), new string[0]),
                new DicomLongString(new DicomTag(3, 0x100c, privateCreator)),
                new DicomLongText(new DicomTag(3, 0x100d, privateCreator), null),
                new DicomOtherByte(new DicomTag(3, 0x100e, privateCreator), new byte[0]),
                new DicomOtherDouble(new DicomTag(3, 0x100f, privateCreator), new double[0]),
                new DicomOtherFloat(new DicomTag(3, 0x1010, privateCreator), new float[0]),
                new DicomOtherLong(new DicomTag(3, 0x1014, privateCreator), new uint[0]),
                new DicomOtherWord(new DicomTag(3, 0x1011, privateCreator), new ushort[0]),
                new DicomPersonName(new DicomTag(3, 0x1012, privateCreator)),
                new DicomShortString(new DicomTag(3, 0x1013, privateCreator)),
                new DicomSignedLong(new DicomTag(3, 0x1001, privateCreator)),
                new DicomSequence(new DicomTag(3, 0x1015, privateCreator)),
                new DicomSignedShort(new DicomTag(3, 0x1017, privateCreator)),
                new DicomShortText(new DicomTag(3, 0x1018, privateCreator), null),
                new DicomTime(new DicomTag(3, 0x1019, privateCreator), new string[0]),
                new DicomUnlimitedCharacters(new DicomTag(3, 0x101a, privateCreator), (string)null),
                new DicomUniqueIdentifier(new DicomTag(3, 0x101b, privateCreator), new string[0]),
                new DicomUnsignedLong(new DicomTag(3, 0x101c, privateCreator)),
                new DicomUnknown(new DicomTag(3, 0x101d, privateCreator)),
                new DicomUniversalResource(new DicomTag(3, 0x101e, privateCreator), null),
                new DicomUnsignedShort(new DicomTag(3, 0x101f, privateCreator)),
                new DicomUnlimitedText(new DicomTag(3, 0x1020, privateCreator), null),
            };
        }

        private static DicomDataset BuildZooDataset()
        {
            var target = new DicomDataset
                {
                    new DicomPersonName(DicomTag.PatientName, new[] { "Anna^Pelle", null, "Olle^Jöns^Pyjamas" }),
                    { DicomTag.SOPClassUID, DicomUID.RTPlanStorage },
                    { DicomTag.SOPInstanceUID, DicomUIDGenerator.GenerateDerivedFromUUID().UID },
                    { DicomTag.SeriesInstanceUID, new DicomUID[] { } },
                    { DicomTag.DoseType, new[] { "HEJ" } },
                };

            target.Add(DicomTag.ControlPointSequence, (DicomSequence[])null);
            var beams = new[] { 1, 2, 3 }.Select(beamNumber =>
            {
                var beam = new DicomDataset
                {
                    { DicomTag.BeamNumber, beamNumber },
                    { DicomTag.BeamName, $"Beam #{beamNumber}" },
                };
                return beam;
            }).ToList();
            beams.Insert(1, null);
            target.Add(DicomTag.BeamSequence, beams.ToArray());
            return target;
        }
    }
}
