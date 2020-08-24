// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Common;

namespace QidoQueryGenerator
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            string path = args[0];
            string line;
            StreamReader file = new StreamReader(path);
            HashSet<string> studyGeneric = new HashSet<string>();
            HashSet<string> studySpecific = new HashSet<string>();
            HashSet<string> seriesGeneric = new HashSet<string>();
            HashSet<string> seriesSpecific = new HashSet<string>();

            while ((line = file.ReadLine()) != null)
            {
                PatientInstance pI;
                pI = JsonSerializer.Deserialize<PatientInstance>(line);

                string namePrefix = pI.Name.Split('^')[0].Substring(0, 3);
                string startDate = DateTime.Parse(pI.PerformedProcedureStepStartDate).AddMonths(-1).ToString("yyyyMMdd");
                string endDate = DateTime.Parse(pI.PerformedProcedureStepStartDate).AddMonths(1).ToString("yyyyMMdd");
                string studyGenericVal = $"/Studies?PatientName={namePrefix}&StudyDate={startDate}-{endDate}&fuzzyMatching=true";
                string studySpecificVal = $"/Studies?PatientId={pI.PatientId}";
                string seriesGenericVal = $"/Series?Modality={pI.Modality}";
                string seriesSpecificVal = $"/Series?Modality={pI.Modality}&PatientId={pI.PatientId}";

                studyGeneric.Add(studyGenericVal);
                studySpecific.Add(studySpecificVal);
                seriesGeneric.Add(seriesGenericVal);
                seriesSpecific.Add(seriesSpecificVal);
            }

            file.Close();

            string outputPath = args[1];
            List<string> queries = new List<string>();
            queries.AddRange(studyGeneric);
            queries.AddRange(studySpecific);
            queries.AddRange(seriesGeneric);
            queries.AddRange(seriesSpecific);
            queries.Shuffle();
            File.WriteAllLines(outputPath, queries);
            Console.WriteLine(queries.Count);
        }
    }
}
