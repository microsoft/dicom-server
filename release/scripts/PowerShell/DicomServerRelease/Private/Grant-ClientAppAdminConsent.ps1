function Grant-ClientAppAdminConsent {
    <#
    .SYNOPSIS
    Grants admin consent to a client app, so that users of the app are 
    not required to consent to the app calling the Dicom apli app on their behalf.
    .PARAMETER AppId
    The client application app ID.
    .PARAMETER TenantAdminCredential
    Credentials for a tenant admin user
    #>
    param(
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string]$AppId
    )

    Set-StrictMode -Version Latest

    Write-Host "Granting admin consent for app ID $AppId"
    [string]$tenantId = ((Get-AzureADCurrentSessionInfo).Tenant.Id)

    # get access token for Graph API
    $accessToken = Get-AzAccessToken -ResourceUrl https://graph.microsoft.com/ -TenantId $tenantId   

    # get applicatioin and service principle
    $app = Get-AzureADApplication -Filter "AppId eq '$AppId'"
    $sp = Get-AzureADServicePrincipal -Filter "AppId eq '$AppId'"
    
    foreach($access in $app.RequiredResourceAccess)
    {
        # grant permission for each required access
        $targetAppId = $access.ResourceAppId
        foreach($resourceAccess in $access.ResourceAccess)
        {
            # There are 2 types: Scope or Role
            # Role refers to AppRole, can be granted via Graph API appRoleAssignments (https://docs.microsoft.com/en-us/graph/api/serviceprincipal-list-approleassignments?view=graph-rest-1.0&tabs=http)
            # Scope refers to OAuth2Permission, also known as Delegated permission,  can be granted via Graph API oauth2PermissionGrants (https://docs.microsoft.com/en-us/graph/api/oauth2permissiongrant-list?view=graph-rest-1.0&tabs=http)
            # We currently don't have requirement for Role, so only handle Scope.
            if($resourceAccess.Type -ne "Scope")
            {
                Write-Warning "Granting admin content on $($resourceAccess.Type) is not supported."
                continue
            }
            $targetAppResourceId = $resourceAccess.Id
            
            # get target app service principle
            $targetSp =  Get-AzureADServicePrincipal -Filter "AppId eq '$targetAppId'"

            # get scope value
            $oauth2Permission =  $targetSp.Oauth2Permissions | ? {$_.Id -eq $targetAppResourceId}
            $scopeValue = $oauth2Permission.Value
            
            $body = 
            @{
                clientId     =   $sp.ObjectId
                consentType  =   "AllPrincipals" # admin consent -- consent for users
                resourceId   =   $targetSp.ObjectId
                scope        =   $scopeValue 
            }

           
            $header = 
            @{
                Authorization  = "Bearer $($accessToken.Token)"                    
                'Content-Type' = 'application/json'
            }
            Invoke-RestMethod "https://graph.microsoft.com/v1.0/oauth2PermissionGrants" -Method Post -Body ($body | ConvertTo-Json) -Headers $header 
            Write-Verbose "Permission '$scopeValue' on '$($targetSp.appDisplayName)' to '$($sp.appDisplayName)' is granted!"
            
        }
    
    }
}