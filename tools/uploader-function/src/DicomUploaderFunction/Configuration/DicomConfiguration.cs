using System;

namespace DicomUploaderFunction.Configuration;

public class DicomConfiguration
{
    public const string SectionName = "DicomWeb";

    public Uri Endpoint { get; set; }
    public AuthenticationConfiguration Authentication { get; set; }
}