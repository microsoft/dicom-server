// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.CommandLine;
using System.CommandLine.Parsing;

namespace Microsoft.Health.Dicom.FileGenerator;

public class Program
{
    private const int MaxStudies = 50;
    private const int MaxSeries = 100;
    private const int MaxInstances = 1000;

    public void Main(string[] args)
    {
        ParseArgumentsAndExecute(args);
    }

    private void ParseArgumentsAndExecute(string[] args)
    {
        var path = new Option<string>(
            "--path",
            description: "Path to file(s), e.g.: \"C:\\dicomdir\"");

        var invalidSS = new Option<bool>(
            "--invalidSS",
            description: "Add an invalid Signed Short attribute");

        var invalidDS = new Option<bool>(
            "--invalidDS",
            description: "Add an invalid Decimal String attribute");

        var numberOfStudies = new Option<int>(
            "--studies",
            description: $"Number of studies to create. Must be between 1 (default) and {MaxStudies}",
            getDefaultValue: () => 1);

        numberOfStudies.AddValidator((OptionResult result) =>
        {
            if (result.GetValueForOption(numberOfStudies) < 1 || result.GetValueForOption(numberOfStudies) >= MaxStudies)
            {
                result.ErrorMessage = $"The value of --studies must not be less than 1 or more than {MaxStudies}.";
            }
        });

        var numberOfSeries = new Option<int>(
            "--series",
            description: $"Number of series to create per study. Must be between 1 (default) and {MaxSeries}",
            getDefaultValue: () => 1);

        numberOfSeries.AddValidator((OptionResult result) =>
        {
            if (result.GetValueForOption(numberOfSeries) < 1 || result.GetValueForOption(numberOfSeries) > MaxSeries)
            {
                result.ErrorMessage = $"The value of --series must not be less than 1 or more than {MaxSeries}.";
            }
        });

        var numberOfInstances = new Option<int>(
            "--instances",
            description: $"Number of instances to create per series. Must be between 1 (default) and {MaxInstances}",
            getDefaultValue: () => 1);

        numberOfInstances.AddValidator((OptionResult result) =>
        {
            if (result.GetValueForOption(numberOfInstances) < 1 || result.GetValueForOption(numberOfInstances) > MaxInstances)
            {
                result.ErrorMessage = $"The value of --instances must not be less than 1 or more than {MaxInstances}.";
            }
        });

        var fileSizeInMB = new Option<int>(
            "--fileSize",
            description: "The approximate size of each instance file, in MB");

        fileSizeInMB.AddValidator((OptionResult result) =>
        {
            if (result.GetValueForOption(fileSize) < 1)
            {
                result.ErrorMessage = $"The value of --fileSizeInMB must not be less than 0.";
            }
        });

        var rootCommand = new RootCommand("Generate one or optionally many DICOM file(s).");

        rootCommand.AddOption(path);
        rootCommand.AddOption(invalidSS);
        rootCommand.AddOption(invalidDS);
        rootCommand.AddOption(numberOfStudies);
        rootCommand.AddOption(numberOfSeries);
        rootCommand.AddOption(numberOfInstances);
        rootCommand.AddOption(fileSize);

        rootCommand.SetHandler(Execute, path, invalidSS, invalidDS, numberOfStudies, numberOfSeries, numberOfInstances, fileSizeInMB);
        rootCommand.Invoke(args);
    }

    private void Execute(string path, bool invalidSS, bool invalidDS, int numberOfStudies = 1, int numberOfSeries = 1, int numberOfInstances = 1, int fileSizeInMB = 1)
    {
        var fileGenerator = new FileGenerator(fileSizeInMB);
        fileGenerator.SaveFiles(path, invalidSS, invalidDS, numberOfStudies, numberOfSeries, numberOfInstances);
    }
}
