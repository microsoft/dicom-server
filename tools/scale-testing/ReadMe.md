# How to Use the Scale Testing Tool

To use the scale testing tool, first use the [ARM template](templates/default-azuredeploy.json) and deploy your Azure Resource using Template Deployment.

After, you need to determine what level of permissions you have over your subscription. If you are able to grant yourself elevated permissions, you can follow the second set of powershell scripts which further automate the process. Elevated permissions means that in Service Bus you are a [Data Owner](https://docs.microsoft.com/en-us/azure/role-based-access-control/built-in-roles#azure-service-bus-data-owner) and in the App Service you are a [Website Contributor](https://docs.microsoft.com/en-us/azure/role-based-access-control/built-in-roles#website-contributor).

Also, while DownloadBlobNames.psm1 can be used to download the names of successfully stored instances, series and studies, at the [end](#download-successfully-stored-instances-series-and-studies-using-ssms) of this readme, another simpler and faster (but more manual) way is laid out.


## **Powershell scripts with regular permissions**
As a prerequisite, start a powershell console in the current folder and open the visual studio solution in the current folder using visual studio.

### STOW-RS
1. Open the app configuration resource in the resource group you created and in the configuration explorer, set Run-Type to 'stow-rs'
2. Execute STOW-RS.psm1:
    a) The resource group it asks for is the one you created when deploying the ARM template.  
    b) The instance count dictates the total number of instances that will be stored (as an approximation).  
    c) The number of threads to run simultaneously identifies how many instances of the generator app will run at once. 
3. Once the generator instances complete you will have a set of numbered text files in the current folder that contain a set of person instances and the stow-rs service bus topic should have the instances you created.
4. To publish the message handler:
    a) In Visual Studio, right click on 'Microsoft.Health.Dicom.Tools.ScaleTesting.MessageHandler' and select 'Publish'.  
    b) In the menu that pops up, select 'Select Existing'. Click 'Create Profile' to continue.   
    c) It should auto populate your subscriptions, select the one containing your resource group, pick your resource groups from the list below and select the available App Service.  
    d) After it creates a profile, click the edit icon next to 'WebJobType'.  
    e) In the pop-up menu, change 'WebJobType' to 'Continuous' and click 'Save'.  
    f) Double-check the values and then click 'Publish' when you are ready.  
6. After the service bus topic is empty and the run is completed, delete the web job by going to the app service's WebJobs menu.

### Download Blob Names
1. Execute DownloadBlobNames.psm1.
2. When it finishes running, there should be 3 new txt files in the current folder - instances.txt, series.txt and studies.txt.

### WADO-RS
1. Open the app configuration resource in the resource group you created and in the configuration explorer, set Run-Type to 'wado-rs'
2. Execute WADO-RS.psm1
    a) The run type it asks for is to identify whether the retrieve run you are executing is for instances, series or studies. The default value is instances.  
    b) The resource group it asks for is the one you created when deploying the ARM template.  
    c) The number of threads to run simultaneously identifies how many instances of the generator app will run at once. 
3. Once the uploader instances complete the 'wado-rs' service bus topic should have the a set of uploaded instances/series/studies uids.
4. If you have previously used the MessageHandler you should have a saved publish profile when you right click on it to Publish in Visual Studio. If not, refer to step 5 in STOW-RS. Publish the MessageHandler using the created publish profile.
5. After the service bus topic is empty and the run is completed, delete the web job by going to the app service's WebJobs menu.

### WADO-RS Metadata
This is the same as WADO-RS except that the run-type should be set to wado-rs-metadata, WADO-RSForMetadata.psm1 should be executed and the service bus topic to monitor is 'wado-rs-metadata'.

### QIDO-RS
This also behaves similarly to WADO-RS and WADO-RS Metadata except the following: run-type is 'qido', QIDO-RS.psm1 is executed, you will not be prompted for a run type and the service bus topic to monitor is 'qido'.


## **Powershell Scripts with Elevated Permissions**
As a prerequisite, start a powershell console in the current folder. These scripts behave very similarly as the regular ones so the below instructions just mention what is different. In essence, they use your ability to have more permissions to monitor the service bus and deploy the MessageHandler for you.

### STOW-RS
Execute STOW-RSElevated.psm1 instead. The script will also ask for the service bus namespace and app service name, both of which can be retrieved from the resource group. Steps 4 & 5 can be ignored and once the run is over, you can skip to 6.

### Download Blob Names
No difference.

### WADO-RS
Execute WADO-RSElevated.psm1 instead. The script will also ask for the service bus namespace and app service name, both of which can be retrieved from the resource group. Steps 4 & 5 can be ignored and once the run is over, you can skip to 6.

### WADO-RS Metadata
Execute WADO-RSForMetadataElevated.psm1 instead. The script will also ask for the service bus namespace and app service name, both of which can be retrieved from the resource group. Steps 4 & 5 can be ignored and once the run is over, you can skip to 6.

### QIDO-RS
Execute WIDO-RSElevated.psm1 instead. The script will also ask for the service bus namespace and app service name, both of which can be retrieved from the resource group. Steps 4 & 5 can be ignored and once the run is over, you can skip to 6.
  
  
## **Download successfully stored instances, series and studies using SSMS**
This is a faster way to download the successfully stored instances/series/studies when compared to using the powershell script DownloadBlobNames.ps1. It is, however, more manual.
1. Open SSMS and connect to the Azure SQL instance associated with your scale testing resource group.
2. Expand the databases and right-click on Dicom. In the menu, select 'Tasks' and in the ensuing sub-menu, select 'Export Data'. This should result in a SQL Server Import and Export Wizard popping up.
3.
    a) Press Next on the homepage.
    b) Select the Microsoft OLE DB Provider as the Data Source. The server name should be autopopulated. Use SQL authentication with the admin username and password that you used to connect to the instance. Press next. 
    c) Select Flat File Destination, press browse to specify the file name and path and press next.
    d) Select the option "Write a query to specify the data to transfer" and press next.
    e) Input one of the following queries (repeat for the other two as well!) and press Parse. Once validated, press next.  
    ```
        SELECT DISTINCT StudyInstanceUid
        FROM dbo.Instance
        WHERE Status = '1' 

        SELECT DISTINCT StudyInstanceUid, SeriesInstanceUid
        FROM dbo.Instance
        WHERE Status = '1'

        SELECT DISTINCT StudyInstanceUid, SeriesInstanceUid, SopInstanceUid
        FROM dbo.Instance
        WHERE Status = '1'
    ```
    f) In Configure Flat File Destination, keep the default values and press next. Repeat with Save and Run Package.
    g) Once complete, the Export tool will create a file with the results of that query - the successfully stored instances!
4. You can modify this file to be congruent with what RetrieveBlobNames produces by removing the first row containing the column names and replacing the commas separating values with a space.