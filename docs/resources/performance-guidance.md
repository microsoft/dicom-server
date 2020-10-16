# Medical Imaging Server for DICOM Performance Guidance

When you deploy an instance of the Medical Imaging Server for DICOM, the following resources are important for performance in a production workload:

- **App Service Plan**: Hosts the Medical Imaging Service for DICOM.
- **Azure SQL**: Indexes a subset of the Medical Imaging Server for DICOM metadata to support queries and to maintain a queryable log of changes.
- **Storage Account**: Blob Storage which persists all Medical Imaging Server for DICOM data and metadata.

This resource provides guidance on Azure App Service, SQL Database and Storage Account settings for the Medical Imaging Server for DICOM. Note, these are recommendations but may not fit the exact needs of your workload.

## Azure App Service Plan

The S1 tier is the default App Service Plan SKU enabled upon deployment. You can customize your App Service Plan SKU during deployment via the Medical Imaging Server for DICOM [Quickstart Deploy to Azure](../quickstarts/deploy-via-azure.md). You can also update your App Service Plan after deployment. You can find instructions to that at [Configure Medical Imaging Server for DICOM Settings](../how-to-guides/configure-dicom-server-settings.md).

Azure offers a variety of plans to meet your workload requirements. To learn more about the various plans, view the [App Service pricing](https://azure.microsoft.com/pricing/details/app-service/windows/). To learn how to update your Azure App Service Plan, refer to [Configure Medical Imaging Server for DICOM Settings](../how-to-guides/configure-dicom-server-settings.md).

## Azure SQL Database Tier

The Standard tier of the DTU-based SQL performance tiers is enabled by default upon deployment. We recommend the DTU Purchase Model over the vCore model for the Medical Imaging Server for DICOM. In DTU-based SQL purchase models, a fixed set of resources is assigned to the database via performance tiers: Basic, Standard and Premium.

To review the various SQL Database Tiers from Azure, refer to [Azure SQL Database Pricing](https://azure.microsoft.com/pricing/details/sql-database/single/). To learn how to update your Azure SQL Database tier, refer to [Configure Medical Imaging Server for DICOM Settings](../how-to-guides/configure-dicom-server-settings.md).

## Geo-Redundancy

For a production workload, we highly recommend configuring your Medical Imaging Server for DICOM to support geo-redundancy.

### Geo-Redundant Azure Storage

Azure Storage offers geo-redundant storage to ensure high availability even in the event of a regional outage. We highly recommend choosing an Azure Storage Account Sku that supports geo-redundancy if you are running a production workload. Azure storage offers two options for geo-redundant replication, Geo-zone-redundant-storage (GZRS) and Geo-redundant-storage (GRS). Refer to this article to decide which geo-redundant Azure Storage option is right for you: [Use geo-redundancy to design highly available applications](https://docs.microsoft.com/en-us/azure/storage/common/geo-redundant-design).

You can customize your Azure Storage Account SKU during deployment via the Medical Imaging Server for DICOM [Quickstart Deploy to Azure](../quickstarts/deploy-via-azure.md). By default, Standard LRS is selected, which is Standard Locally Redundant Storage.

### Geo-replication for SQL Database

In addition to configuring geo-redudunant Azure Storage, we recommend configuring active geo-replication for your Azure SQL Database. This allows you to create readable secondary databases of individual databases on a server in the same or different data center region. For a tutorial on how to configure this, see [Creating and using active geo-replication - Azure SQL Database](https://docs.microsoft.com/azure/azure-sql/database/active-geo-replication-overview).

## Workload Scenarios for Azure SQL Database & Azure App Service

### Scenario 1: Testing out DICOM Server

If you are testing out the Medical Imaging Server for DICOM and not running production workloads, we recommend using the S1 Azure App Service Tier alongside a Standard Azure SQL Database (S1, S2, S3). For a small system that does not require redundancy, with these tiers, you can spend as little as ~$70/month on Azure App Service & Azure SQL Database.

If you had ~100,000 DICOM instances, you should expect a range of 2000-9000 requests/minute and a response time under 1 second for STOW, WADO & QIDO.

### Scenario 2: Production workload for a small ecosystem

For a smaller production workload, we recommend scaling up your Azure SQL Database to S12. For your Azure App Service, any Standard tier should be sufficient. If you are going into production, you also need to ensure your Medical Imaging Server for DICOM supports geo-redundancy. Refer to our [geo-redundancy guidelines](##Geo-Redundancy).

We recommend the S1 Standard Azure App Service Tier along side an Azure SQL Tier of S12. At these tiers, you can expect up a range of 1000-20,000 requests/minute with a response time under 400 ms for WADO, QIDO & STOW.

### Scenario 3: Large Hospital System with high numbers of DICOM requests per day in a production workload

If you need to upload a large number of DICOM instances a day (800,000+), we recommend scaling up your Azure App Service Plan and your Azure SQL Database to Premium Tiers. If you are going into production, you also need to ensure your Medical Imaging Server for DICOM supports geo-redundancy. Refer to our [geo-redundancy guidelines](##Geo-Redundancy).

To support a large number of DICOM transactions per day, we recommend a P1v2 tier for Azure App Service alongside a P11 tier for Azure SQL Database. For STOW, WADO and QIDO, you should expect a range of 40,000 - 100,000 requests/minute with response times under 200 ms.

## Summary

In this resource, we reviewed suggested guidance for Azure App Service tiers, Azure SQL tiers and Storage Account settings so that your Medical Imaging Server for DICOM can meet your  workload requirements:

- To get started with the Medical Imaging Server for DICOM, [Deploy to Azure](../quickstarts/deploy-via-azure.md).
- If you already have configured an instance of the Medical Imaging Server for DICOM, [Configure your DICOM Server Settings](../how-to-guides/configure-dicom-server-settings.md).
