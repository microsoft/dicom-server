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
    Write-Host "Enabling access to storage account through Managed Identity."
    New-AzRoleAssignment -ObjectId $managedIdentity.Id -ResourceName $storageAccount.Name -ResourceType $storageAccount.Type -ResourceGroupName $storageAccount.ResourceGroupName -RoleDefinitionName "Storage Blob Data Reader"

    # Create event grid
    Set-Variable -Name EventEndpoint -Value "https://$DicomServerName.azurewebsites.net/webhooks/bulkImport" -Option Constant

    Write-Host "Subscribing to storage account events."
    
    New-AzEventGridSubscription -Endpoint $EventEndpoint `
        -EventSubscriptionName "$StorageAccountName-bi" `
        -ResourceId $storageAccount.Id `
        -IncludedEventType "Microsoft.Storage.BlobCreated"

    # Call to enable the bulk import.
    Write-Host "Enabling Bulk Import from storage account."
    Invoke-WebRequest -Uri "https://$DicomServerName.azurewebsites.net/bulkImport/$StorageAccountName" -Method POST
}   
