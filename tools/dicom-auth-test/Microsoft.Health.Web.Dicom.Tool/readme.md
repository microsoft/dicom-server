## About this sample
This is an example that demonstrates how can a dicomservice be accessed from an azure VM that has system identity enabled.

## How to execute
To execute this example, run the following command from the console of your VM that has system identity enabled

     dotnet run --project .\Microsoft.Health.Web.Dicom.Tool.csproj execute --dicomServiceUrl <dicom service url>

Example:

     dotnet run --project.\Microsoft.Health.Web.Dicom.Tool.csproj execute --dicomServiceUrl "https://testdicomweb-testdicom.dicom.azurehealthcareapis.com"
