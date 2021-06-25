function Grant-ClientAppAdminConsent {
    <#
    .SYNOPSIS
    Grants admin consent to a client app, so that users of the app are 
    not required to consent to the app calling the Dicom apli app on their behalf.
    .PARAMETER AppId
    The client application ID.
    .PARAMETER TenantAdminCredential
    Credentials for a tenant admin user
    .PARAMETER ApiAppId
    Server Application service ID
    #>
    param(
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string]$AppId,

        [Parameter(Mandatory = $true)]
        [ValidateNotNull()]
        [pscredential]$TenantAdminCredential,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string]$ApiAppId
    )
 
    Set-StrictMode -Version Latest

    Write-Host "Granting admin consent for app: $AppId"
    
    # Get App SP objectIds 
    $apiAppServicePrincipal = Get-AzureAdServicePrincipal -Filter "appId eq '$ApiAppId'"
    $appServicePrincipal = Get-AzureAdServicePrincipal -Filter "appId eq '$AppId'"
    $appServicePrincipalObjectId = $appServicePrincipal.ObjectId
    $apiAppServicePrincipalObjectId = $apiAppServicePrincipal.ObjectId

    $body = @{
        grant_type = "password"
        username   = $TenantAdminCredential.GetNetworkCredential().UserName
        password   = $TenantAdminCredential.GetNetworkCredential().Password
        resource   = "a3efc889-f1b7-4532-9e01-91e32d1039f4" # MS graph service principle
        client_id  = "1950a258-227b-4e31-a9cf-717495945fc2" # Microsoft Azure PowerShell
    }
    
    $tokenResponse = Invoke-RestMethod (Get-
    ) -Method POST -Body $body -ContentType 'application/x-www-form-urlencoded'
    
    $header = @{
        'Authorization'          = 'Bearer ' + $tokenResponse.access_token
        'x-ms-client-request-id' = [guid]::NewGuid()
    }

    $url = "https://graph.microsoft.com/v1.0/servicePrincipals/$apiAppServicePrincipalObjectId/appRoleAssignedTo"
    
    $consentbody = @{
        principalId = $appServicePrincipalObjectId
        resourceId  = $apiAppServicePrincipalObjectId
        appRoleId   = $apiAppServicePrincipal.Oauth2Permissions[0].Id
    }

    $retryCount = 0

    while ($true) {
        try {
            Invoke-RestMethod -Uri $url -Headers $header -Method POST -Body $consentbody | Out-Null
            return
        }
        catch {
            if ($retryCount -lt 6) {
                $retryCount++
                Write-Warning "Received failure when posting to $url. Will retry in 10 seconds."
                Start-Sleep -Seconds 10
            }
            else {
                throw
            }    
        }
    }
}
