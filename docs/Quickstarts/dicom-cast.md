# Deploy  DICOMcast

DICOMcast can be deployed as an Azure Container Instance using the provided [ARM Template](/converter/dicom-cast/samples/templates/default-azuredeploy.json).

## Prerequisites

* A deployed Azure API for FHIR endpoint or FHIR Server 
* A deployed Medical Imaging Server for Azure

## Deployment

The ARM template will deploy the following resources to the specified resource group:

* Azure Container Instance
    * Used to run the DICOMcast executable
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
