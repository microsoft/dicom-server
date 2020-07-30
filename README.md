# DICOM Server for Azure

A .NET Core implementation of the DICOM Web standard. Details of the Dicom web standard implemented can be found [here](docs/users/Conformance.md).

## Deploy the Dicom Server
The source code is available to be deployed in any manner you would like. The Dicom server can be run on-prem or in the cloud. To assist with easy deployment we have included two options below, one through Azure and one which will deploy locally. 

### Deploy to Azure
To deploy in Azure, you will need to have a subscription in Azure. If you do not have an Azure subscription, you can start [here](https://azure.microsoft.com/free).

Once you have your subscription, click the link below:

<a href="https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fdcmcistorage.blob.core.windows.net%2Fcibuild%2Fdefault-azuredeploy.json" target="_blank">
    <img src="https://azuredeploy.net/deploybutton.png"/>
</a>

Note that the Service Name will be included in the URL you will use to access the application. 

Once deployment is complete, you can go to the newly created App Service to see the details. Here you will get the URL to access your Dicom server (https://<SERVICE NAME>.azurewebsites.net).

### Deploy locally
Follow the steps [here](docs/Development.md) to deploy a local copy of the Dicom Server

## Users
- [Conformance](docs/users/Conformance.md)
- [ChangeFeed](docs/users/ChangeFeed.md)
- [Health Check API](docs/users/HealthCheckAPI.md)

## Development
- [Setup](docs/Development.md)
- [Code Organization](docs/CodeOrganization.md)
- [Naming Guidelines](docs/NamingGuidelines.md)
- [Exception handling](docs/ExceptionHandling.md)
- [Tests](docs/Tests.md])
- [Authentication](docs/Authentication.md)

## Contributing
This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.microsoft.com.

When you submit a pull request, a CLA-bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., label, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

There are many other ways to contribute to DICOM Server for Azure.
* [Submit bugs](https://github.com/Microsoft/dicom-server/issues) and help us verify fixes as they are checked in.
* Review the [source code changes](https://github.com/Microsoft/dicom-server/pulls).
* Engage with DICOM Server for Azure users and developers on [StackOverflow](https://stackoverflow.com/questions/tagged/dicom-server-for-azure).
* Join the [#dicomforazure](https://twitter.com/hashtag/dicomserverforazure?f=tweets&vertical=default) discussion on Twitter.
* [Contribute bug fixes](CONTRIBUTING.md).

See [Contributing to Dicom Server for Azure](CONTRIBUTING.md) for more information.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

