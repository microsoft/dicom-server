# About this sample
This is an example that demonstrates how can a dicomservice be accessed from an azure VM that has system identity enabled.

## How to execute
To execute this example, run the following command from the console of your VM that has system identity enabled

     dotnet run --project .\Microsoft.Health.Dicom.Tool.ManagedIdentitySample.csproj --dicomServiceUrl <dicom service url>

Example:

     dotnet run --project.\Microsoft.Health.Dicom.Tool.ManagedIdentitySample.csproj --dicomServiceUrl "https://testdicomweb-testdicom.dicom.azurehealthcareapis.com"
