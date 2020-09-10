# Deploy DICOM Cast

DICOM Cast can be deployed as an Azure Container Instance using the provided [ARM Template](/converter/dicom-cast/samples/templates/default-azuredeploy.json).

## Prerequisites

* A deployed Azure API for FHIR endpoint or FHIR Server 
* A deployed Medical Imaging Server for Azure

## Deployment

If you have an Azure subscription, click the link below to deploy to Azure:

<a href="https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fdcmcistorage.blob.core.windows.net%2Fcibuild%2Fdicom-cast%2Fdefault-azuredeploy.json" target="_blank">
    <img src="https://azuredeploy.net/deploybutton.png"/>
</a>

This will deploy the following resources to the specified resource group:

* Azure Container Instance
    * Used to run the DICOM Cast executable
    * The image used is specified via the `image` parameter and defaults to the latest CI build
    * A managed identity is also configured
* Application Insights
    * If `deployApplicationInsights` is specified, an Application Insights instance is deployed for logging
* Storage Account
    * Used to keep track of the state of the service
* KeyVault
    * Used to store the storage connection string
    * Is accessed via the managed identity specified on ACI

Instructions for how to deploy an ARM template can be found in the following docs
* [Deploy via Portal](https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/deploy-portal)
* [Deploy via CLI](https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/deploy-cli)
* [Deploy via PowerShell](https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/deploy-powershell)

## Authentication

The authentication used will depend on setup of your Medical Imaging Server for Azure and your FHIR Server. For additional information regarding setting up authentication see [this documentation](/converter/dicom-cast/docs/authentication.md).

Below steps needs to be taken
- Add secrets related to Authentication in KeyValut for Medical Imaging Server for Azure.

    Example: If Medical Imaging Server for Azure was configured with `OAuth2ClientCredential`, below is the list of secrets that need to added to the KeyValut.

    - DicomWeb--Authentication--Enabled : True
    - DicomWeb--Authentication--AuthenticationType : OAuth2ClientCredential
    - DicomWeb--Authentication--OAuth2ClientCredential--TokenUri : ```<AAD tenant token uri>```
    - DicomWeb--Authentication--OAuth2ClientCredential--Resource : ```Application ID URI of the resource app```
    - DicomWeb--Authentication--OAuth2ClientCredential--Scope : ```Application ID URI of the resource app```
    - DicomWeb--Authentication--OAuth2ClientCredential--ClientId : ```Client Id of the client app```
    - DicomWeb--Authentication--OAuth2ClientCredential--ClientSecret : ```Client app secret```

- Add similar secrets to KeyVault for FHIR server.

- Stop and Start the Container, to pickup the new configurations.



