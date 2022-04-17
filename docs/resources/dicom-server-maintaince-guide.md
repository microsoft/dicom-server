# Managing deployment of Medical Imaging Server for DICOM in production

Below is a list of things to consider if you want to mantain your own deployment. We recommend you to be moderately familiar with the code. 

| Area | Description |
| --- | --- |
| ARM Deployment template | The [sample deployment](../quickstarts/deploy-via-azure.md) deploys all the dependencies and configurations, but is not production ready. Consider it a template. |
| Web Application | This is our hosting layer. Consider creating your own with the right composition of Authentication and Authorization services. The [Web application](../../src/Microsoft.Health.Dicom.Web/) we have uses development Identity server which must be replaced for production use.  |
| Authentication and Authorization | Sample Azure deployment will deploy with no security, meaning the server is accessible to everyone on the internet. Review our Authentication and Authorization documentation and set it up correctly.|
| Upgrade | This is a active project. We are constantly fixing issues, adding features and re-architecting to make the service better. This means both [SQL schema](https://github.com/microsoft/fhir-server/blob/main/docs/SchemaMigrationGuide.md) and binaries must to be upgraded regularly. Our SQL schema versions go out support quickly. A weekly upgrade cadence, with a close monitoring of commit history, is recommended. |
| Monitoring | Logs from the DICOM server optionally go to Application Insights. Consider adding active monitoring and alerting. |
| Capacity and Scale | Plan for expected production workloads and performance needs. Azure SQL Database and Azure App Service scale independently. Consider both horizontal and vertical scaling. |
| Network Security | Consider network security like Private endpoints and Virtual network access to the dependent services. This can provide additional security features for defense in depth. |
| Pricing | A basic App Service plan and SQL Database is deployed with the sample deployment. There are basic charges for these resources even if they are not used. |
| Data redundancy | Ensure the correct SQL Database and Storage Account SKU to provide for desired data redudancy and availability.  |
| Disaster recovery | Consider multi-region failover, back-ups and other techniques to support mission critical deployments. |
| Privacy | Ensure up-to-date policies, tools, and resources to be compliant with Privacy. |
| Compliance | Application Audit logs, Security scan, Data access management, Secure development life cycle, Secret management, access review... etc are some of the tools and process you will have to consider to store PHI data in HIPPA, HITRUST and ISO certified way. |

<br>

> For getting started quickly and to run production workloads at scale with 99.9 SLA and all the mentioned things managed for you, we recommend the [Azure Health Data Services](https://docs.microsoft.com/en-us/azure/healthcare-apis/dicom/dicom-services-overview).
