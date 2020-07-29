function Enable-BulkdImport {
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string]$StorageAccountName,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string]$DicomServerName
    )

    Set-StrictMode -Version Latest

    # Get the storage account exists.
    $storageAccount = Get-AzResource -ResourceName $StorageAccountName -ResourceType Microsoft.Storage/storageAccounts

    # Get the DICOM server exists.
    $dicomServer = Get-AzResource -ResourceName $DicomServerName -ResourceType Microsoft.Web/sites

    # Get the identity of the DICOM server.
    $managedIdentity = Get-AzADServicePrincipal -DisplayNameBeginsWith $DicomServerName

    # Allow the managed identity to read the storage account.
    New-AzRoleAssignment -ObjectId $managedIdentity.Id -ResourceName $storageAccount.Name -ResourceType $storageAccount.Type -ResourceGroupName $storageAccount.ResourceGroupName -RoleDefinitionName "Storage Blob Data Reader"

    # Create event grid
    Set-Variable -Name TopicName -Value "$DicomServerName-bi" -Option Constant
    Set-Variable -Name EventEndpoint -Value "https://$DicomServerName.azurewebsites.net/webhooks/bulkImport" -Option Constant

    $eventGrid = Get-AzEventGridTopic -ResourceGroupName $dicomServer.ResourceGroupName -Name $TopicName -ErrorAction SilentlyContinue

    if (!$eventGrid) {
        $eventGrid = New-AzEventGridTopic -ResourceGroupName $dicomServer.ResourceGroupName -Name $TopicName -Location $dicomServer.Location
    }

    New-AzEventGridSubscription -Endpoint $EventEndpoint -EventSubscriptionName "$StorageAccountName-bi" -IncludedEventType "Microsoft.Storage.BlobCreated"
}