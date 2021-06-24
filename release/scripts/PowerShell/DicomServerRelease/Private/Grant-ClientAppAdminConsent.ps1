function Grant-ClientAppAdminConsent {
    <#
    .SYNOPSIS
    Grants admin consent to a client app, so that users of the app are 
    not required to consent to the app calling the Dicom apli app on their behalf.
    .PARAMETER ClientAppServicePrincipalObjectId
    The client application service principal object ID.
    .PARAMETER TenantAdminCredential
    Credentials for a tenant admin user
    .PARAMETER ApiAppServicePrincipalObjectId
    Server Application service principal object ID
    .PARAMETER RoleId
    Role Id that needs to be consented
    #>
    param(
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string]$ClientAppServicePrincipalObjectId

        [Parameter(Mandatory = $true)]
        [ValidateNotNull()]
        [pscredential]$TenantAdminCredential

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string]$ApiAppServicePrincipalObjectId

        [Parameter(Mandatory = $true)]
        [ValidateNotNull()]
        [pscredential]$RoleId
    )

    Set-StrictMode -Version Latest

    Write-Host "Granting admin consent for app: $ClientAppServicePrincipalObjectId and role: $RoleId"

    # There currently is no documented or supported way of programatically
    # granting admin consent. So for now we resort to a hack. 
    # We call an API that is used from the portal. An admin *user* is required for this, a service principal will not work.
    # Also, the call can fail when the app has just been created, so we include a retry loop. 

    $body = @{
        grant_type = "password"
        username   = $TenantAdminCredential.GetNetworkCredential().UserName
        password   = $TenantAdminCredential.GetNetworkCredential().Password
        resource   = "74658136-14ec-4630-ad9b-26e160ff0fc6" 
        client_id  = "1950a258-227b-4e31-a9cf-717495945fc2" # Microsoft Azure PowerShell
    }
    
    $tokenResponse = Invoke-RestMethod (Get-AzureADTokenEndpoint) -Method POST -Body $body -ContentType 'application/x-www-form-urlencoded'
    
    $header = @{
        'Authorization'          = 'Bearer ' + $tokenResponse.access_token
        'x-ms-client-request-id' = [guid]::NewGuid()
    }

    $url = "https://graph.microsoft.com/v1.0/servicePrincipals/$ApiAppServicePrincipalObjectId/appRoleAssignedTo"
    
    $consentbody = @{
        principalId = $ClientAppServicePrincipalObjectId
        resourceId  = $ApiAppServicePrincipalObjectId
        appRoleId   = $RoleId
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
