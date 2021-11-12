// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Common;
using Common.KeyVault;
using Common.ServiceBus;
using EnsureThat;
using FellowOakDicom;
using Microsoft.Azure.ServiceBus;

namespace PersonInstanceGenerator
{
    public static class Program
    {
        private static readonly List<(string, string)> Modalities = new List<(string, string)>
        {
            ("CR", "Computed Radiography"),
            ("CT", "Computed Tomography"),
            ("MR", "Magnetic Resonance"),
            ("NM", "Nuclear Medicine"),
            ("US", "Ultrasound"),
            ("OT", "Other"),
            ("BI", "Biomagnetic imaging"),
            ("DG", "Diaphanography"),
            ("ES", "Endoscopy"),
            ("LS", "Laser surface scan"),
            ("PT", "Positron emission tomography (PET)"),
            ("RG", "Radiographic imaging (conventional film/screen)"),
            ("TG", "Thermography"),
            ("XA", "X-Ray Angiography"),
            ("RF", "Radio Fluoroscopy"),
            ("RTIMAGE", "Radiotherapy Image "),
            ("RTDOSE", "Radiotherapy Dose"),
            ("RTSTRUCT", "Radiotherapy Structure Set "),
            ("RTPLAN", "Radiotherapy Plan"),
            ("RTRECORD", "RT Treatment Record"),
            ("HC", "Hard Copy"),
            ("DX", "Digital Radiography"),
            ("MG", "Mammography"),
            ("IO", "Intra-oral Radiography"),
            ("PX", "Panoramic X-Ray"),
            ("GM", "General Microscopy"),
            ("SM", "Slide Microscopy"),
            ("XC", "External-camera Photography"),
            ("PR", "Presentation State"),
            ("AU", "Audio"),
            ("ECG", "Electrocardiography"),
            ("EPS", "Cardiac Electrophysiology"),
            ("HD", "Hemodynamic Waveform"),
            ("SR", "SR Document"),
            ("IVUS", "Intravascular Ultrasound"),
            ("OP", "Ophthalmic Photography"),
            ("SMR", "Stereometric Relationship"),
            ("AR", "Autorefraction"),
            ("KER", "Keratometry"),
            ("VA", "Visual Acuity"),
            ("SRF", "Subjective Refraction"),
            ("OCT", "Optical Coherence Tomography (non-Ophthalmic)"),
            ("LEN", "Lensometry"),
            ("OPV", "Ophthalmic Visual Field"),
            ("OPM", "Ophthalmic Mapping"),
            ("OAM", "Ophthalmic Axial Measurements "),
            ("RESP", "Respiratory Waveform"),
            ("KO", "Key Object Selection"),
            ("SEG", "Segmentation"),
            ("REG", "Registration"),
            ("OPT", "Ophthalmic Tomography"),
            ("BDUS", "Bone Densitometry (ultrasound)"),
            ("BMD", "Bone Densitometry (X-Ray)"),
            ("DOC", "Document"),
            ("FID", "Fiducials"),
            ("PLAN", "Plan"),
            ("IOL", "Intraocular Lens Data"),
            ("IVOCT", "Intravascular Optical Coherence Tomography"),
        };

        private static readonly List<string> Occupation = new List<string>
        {
            "Teacher",
            "Doctor",
            "Surgeon",
            "Therapist",
            "Salesperson",
            "Firefighter",
            "Software Engineer",
            "Interpreter",
            "Clerk",
            "Reporter",
            "Plumber",
            "Lawyer",
            "Technician",
            "Pharmacist",
            "Student",
            "Tailor",
            "Chef",
            "Accountant",
            "Carpenter",
            "Author",
            "Analyst",
            "Editor",
            "Maintenance Worker",
            "Fitness Instructor",
            "Factory Worker",
            "Truck Driver",
        };

        private static readonly List<string> Sex = new List<string>
        {
            "O",
            "F",
            "M",
        };

        private static Random s_rand;

        private static string s_serviceBusConnectionString;
        private static ITopicClient s_topicClient;

        public static async Task Main(string[] args)
        {
            EnsureArg.IsNotNull(args, nameof(args));

            var options = new SecretClientOptions()
            {
                Retry =
                {
                    Delay = TimeSpan.FromSeconds(2),
                    MaxDelay = TimeSpan.FromSeconds(16),
                    MaxRetries = 5,
                    Mode = RetryMode.Exponential,
                },
            };
            var client = new SecretClient(new Uri(KnownApplicationUrls.KeyVaultUrl), new DefaultAzureCredential(), options);

            KeyVaultSecret secret = client.GetSecret(KnownSecretNames.ServiceBusConnectionString);

            s_serviceBusConnectionString = secret.Value;

            s_rand = new Random();
            var patientNames = File.ReadAllLines(args[0]);
            var physiciansNames = File.ReadAllLines(args[1]);
            string path = args[2];
            s_topicClient = new TopicClient(s_serviceBusConnectionString, KnownTopics.StowRs);
            int tracker = 0;
            int totalCount = int.Parse(args[3]);

            using StreamWriter sw = File.Exists(path) ? File.AppendText(path) : File.CreateText(path);
            while (tracker < totalCount)
            {
                var patientName = patientNames.RandomElement();
                var patientId = PatientId();
                DateTime patientBirthDate = RandomDateTimeBefore1995();
                var patientSex = Sex.RandomElement();
                var patientOccupation = Occupation.RandomElement();
                int studies = s_rand.Next(1, 5);
                for (int i = 0; i < studies; i++)
                {
                    var physicianName = physiciansNames.RandomElement();
                    DateTime studyDate = RandomDateTimeAfter1995();
                    var accession = AccessionNumber();
                    List<(string, (int, string), (int, string))> instances = InstanceGenerator();
                    (string, string) modality = Modalities.RandomElement();
                    var patientAge = Math.Round((decimal)(studyDate - patientBirthDate).Days / 365);
                    var patientWeight = s_rand.Next(50, 90);

                    foreach ((string, (int, string), (int, string)) inst in instances)
                    {
                        var pI = new PatientInstance
                        {
                            Name = patientName,
                            PatientId = patientId.ToString(),
                            PatientSex = patientSex,
                            PatientBirthDate = patientBirthDate.Date.Year.ToString() + patientBirthDate.Date.Month.ToString() + patientBirthDate.Date.Day.ToString(),
                            PatientAge = patientAge.ToString(),
                            PatientWeight = patientWeight.ToString(),
                            PatientOccupation = patientOccupation,
                            PhysicianName = physicianName,
                            StudyUid = inst.Item1,
                            SeriesUid = inst.Item2.Item2,
                            SeriesIndex = inst.Item2.Item1.ToString(),
                            InstanceUid = inst.Item3.Item2,
                            InstanceIndex = inst.Item3.Item1.ToString(),
                            Modality = modality.Item1,
                            AccessionNumber = accession.ToString(),
                            StudyDate = studyDate.Date.Year.ToString() + studyDate.Date.Month.ToString() + studyDate.Date.Day.ToString(),
                            StudyDescription = modality.Item2,
                            PerformedProcedureStepStartDate = studyDate.AddMinutes(s_rand.Next(1, 10)).ToString(),
                        };

                        var patient = JsonSerializer.Serialize(pI);

                        try
                        {
                            // Create a new message to send to the topic
                            var message = new Message(Encoding.UTF8.GetBytes(patient));

                            Console.WriteLine($" tracker = {tracker}");

                            // Send the message to the topic
                            await s_topicClient.SendAsync(message);

                            sw.WriteLine(patient);
                        }
                        catch (Exception exception)
                        {
                            Console.WriteLine($"{DateTime.Now} :: Exception: {exception.Message}");
                        }

                        tracker++;
                    }
                }
            }
        }

        private static List<(string, (int, string), (int, string))> InstanceGenerator()
        {
            List<(string studyUid, (int seriesIndex, string seriesUid), (int instanceIndex, string instanceUid))> ret = new List<(string, (int, string), (int, string))>();
            string studyUid = DicomUID.Generate().UID;
            int series = s_rand.Next(1, 5);
            for (int i = 0; i < series; i++)
            {
                string seriesUid = DicomUID.Generate().UID;
                int instances = s_rand.Next(1, 7);
                for (int j = 0; j < instances; j++)
                {
                    string instanceUid = DicomUID.Generate().UID;
                    ret.Add((studyUid, (i, seriesUid), (j, instanceUid)));
                }
            }

            return ret;
        }

        private static DateTime RandomDateTimeBefore1995()
        {
            var start = new DateTime(1970, 1, 1);
            var end = new DateTime(1990, 1, 1);
            int range = (end - start).Days;
            DateTime ret = start.AddDays(s_rand.Next(range));
            ret = ret.AddHours(s_rand.Next(24));
            ret = ret.AddMinutes(s_rand.Next(60));
            ret = ret.AddSeconds(s_rand.Next(60));
            return ret;
        }

        private static DateTime RandomDateTimeAfter1995()
        {
            var start = new DateTime(1995, 1, 1);
            int range = (DateTime.Today - start).Days;
            DateTime ret = start.AddDays(s_rand.Next(range));
            ret = ret.AddHours(s_rand.Next(24));
            ret = ret.AddMinutes(s_rand.Next(60));
            ret = ret.AddSeconds(s_rand.Next(60));
            return ret;
        }

        private static int AccessionNumber()
        {
            return s_rand.Next(11111111, 19999999);
        }

        private static int PatientId()
        {
            return s_rand.Next(1000000, 1000000000);
        }
    }
}
