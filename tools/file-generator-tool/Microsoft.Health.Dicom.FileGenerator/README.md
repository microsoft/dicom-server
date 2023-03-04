# Overview
This command-line tool can be used to generate DICOM file(s) that share common study and series metadata. The defaults will generate a small number of 2MB files with random pixel data,
but the options allow a much larger range of uses. All files will be created in the same path - no folder structure will be created.

# Arguments

## --path
Path to file(s), e.g.: `C:\dicomdir`

## --fileSizeInMB
The approximate size of each instance file, in MB. Defaults to 2.

## --studies
Number of studies to create. Must be between 1 (default) and 50.

## --series
Number of series to create per study. Must be between 1 (default) and 100.

## --instances
Number of instances to create per series. Must be between 1 (default) and 1000.

## --invalidSS
Add an invalid Signed Short attribute.

## --invalidDS
Add an invalid Decimal String attribute.

# Example
To create 10 2MB files in the same folder as the binary, run:
```
dotnet run --instances 10
```

To create 5 studies with 1 50 MB instance each in a specific path, run:
```
dotnet run --path C:\dicomdir --fileSizeInMB 50 --studies 5
```
