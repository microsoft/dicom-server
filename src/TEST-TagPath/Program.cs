// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Dicom;

namespace TestTagPath
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            if (args is null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            // DicomItem is an abstract class that contains a tag.

            // DicomElement is a DicomItem with a tag and a value, with many concrete implementations matching data types.
            // e.g. DicomElement > DicomStringElement > DicomMultiStringElement > DicomPersonName
            var elements = new List<DicomElement>();
            elements.Add(new DicomPersonName(DicomTag.PatientName, "Person 1"));
            elements.Add(new DicomPersonName(DicomTag.PersonName, "Person 2"));

            // A DicomDataset can be created by passing in DicomItems, or the tag/value pair that will make a DicomElement
            var dataset = new DicomDataset();
            dataset.Add(DicomTag.PatientName, "Person 1");
            dataset.Add(DicomTag.PatientState, "Happy!");

            // A DicomSequence is a concrete DicomItem (not a DicomElement) that can contain many DicomDatasets
            var sequence = new DicomSequence(DicomTag.ReferencedPatientSequence, dataset);
            Console.WriteLine("Sequence items: " + sequence.Items.Count); // 1
            sequence = new DicomSequence(DicomTag.ReferencedPatientSequence, new DicomDataset[] { dataset, dataset });
            Console.WriteLine("Sequence items: " + sequence.Items.Count); // 2

            // We could define paths via DicomSequence - but that would require
            // knowing concrete types and will always instantiate with empty value 
            var searchSequence = new DicomSequence(DicomTag.ReferencedPatientSequence,
                new DicomDataset[]
                {
                    new DicomDataset().Add(new DicomPersonName(DicomTag.PatientName))
                });

            // A QueryTagPath is simply the tags that trace a path through a sequence, not their corresponding values.
            var tagPath = new QueryTagPath();
            tagPath.AddPath(DicomTag.ReferencedPatientSequence)
                .AddPath(DicomTag.PatientName);
            Console.WriteLine(tagPath.Tags[tagPath.Tags.Count - 1].DictionaryEntry.ValueRepresentations.FirstOrDefault()); // PN

            // Here we traverse a dataset (via extension method) to find the values specified by a tag path as DicomElements.
            var datasetWithSequence = new DicomDataset(new DicomItem[] { sequence });
            var pathElements = datasetWithSequence.GetLastPathElements(tagPath);

            foreach (var pathElement in pathElements)
            {
                var buffer = ((DicomPersonName)pathElement).Buffer;
                // Console.WriteLine(BitConverter.ToString(buffer.Data);
                Console.WriteLine(System.Text.Encoding.UTF8.GetString(buffer.Data));
            }
        }
    }
}

