// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Common;
using EnsureThat;

namespace QidoQueryGenerator
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            EnsureArg.IsNotNull(args, nameof(args));

            string path = args[0];
            string line;
            var file = new StreamReader(path);
            var studyGeneric = new HashSet<string>();
            var studySpecific = new HashSet<string>();
            var seriesGeneric = new HashSet<string>();
            var seriesSpecific = new HashSet<string>();

            while ((line = file.ReadLine()) != null)
            {
                PatientInstance pI;
                pI = JsonSerializer.Deserialize<PatientInstance>(line);

                string namePrefix = pI.Name.Split('^')[0].Substring(0, 3);
                string startDate = DateTime.Parse(pI.PerformedProcedureStepStartDate).AddMonths(-1).ToString("yyyyMMdd");
                string endDate = DateTime.Parse(pI.PerformedProcedureStepStartDate).AddMonths(1).ToString("yyyyMMdd");
                string studyGenericVal = $"/Studies?PatientName={namePrefix}&StudyDate={startDate}-{endDate}&fuzzyMatching=true";
                string studySpecificVal = $"/Studies?PatientID={pI.PatientId}";
                string seriesGenericVal = $"/Series?Modality={pI.Modality}";
                string seriesSpecificVal = $"/Series?Modality={pI.Modality}&PatientID={pI.PatientId}";

                studyGeneric.Add(studyGenericVal);
                studySpecific.Add(studySpecificVal);
                seriesGeneric.Add(seriesGenericVal);
                seriesSpecific.Add(seriesSpecificVal);
            }

            file.Close();

            string outputPath = args[1];
            var queries = new List<string>();
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
