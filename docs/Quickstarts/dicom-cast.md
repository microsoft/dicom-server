# Deploy  Dicom-cast

DICOMCast can be deployed as an Azure Container Instance using the provided [ARM Template](/converter/dicom-cast/samples/templates/default-azuredeploy.json).

## Prerequisites

* A FHIR Server deployed 
* A DICOM Server deployed

## Deployment

The ARM template will deploy the following resources to the specified resource group

* Azure Container Instance
    * Used to run the DICOMCast executable
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

The authentication used will depend on setup of your DICOM Server and your FHIR Server. For additional information regarding setting up authentication see [this documentation](/converter/dicom-cast/docs/authentication.md).

Below is an example of the settings need to be added to the KeyValut for OAuth2ClientCredential

- DicomWeb--Authentication--Enabled : true
- DicomWeb--Authentication--AuthenticationType : OAuth2ClientCredential
- DicomWeb--Authentication--OAuth2ClientCredential--TokenUri : ```<AAD tenant token uri>```
- DicomWeb--Authentication--OAuth2ClientCredential--Resource : ```Application ID URI of the resource app```
- DicomWeb--Authentication--OAuth2ClientCredential--Scope : ```Application ID URI of the resource app```
- DicomWeb--Authentication--OAuth2ClientCredential--ClientId : ```Client Id of the client app```
- DicomWeb--Authentication--OAuth2ClientCredential--ClientSecret : ```Client app secret```



