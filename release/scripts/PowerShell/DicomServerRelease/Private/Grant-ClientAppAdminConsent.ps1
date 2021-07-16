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

    # get applicatioin and service principle
    Write-Host "DEBUG: TenantId - $((Get-AzureADCurrentSessionInfo).Tenant.Id) "
    
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
            
            Grant-AzureAdOauth2Permission -ClientId $sp.ObjectId ` -ConsentType "AllPrincipals" -ResourceId $targetSp.ObjectId -Scope $scopeValue            
        }
    
    }
}

function Grant-AzureAdOauth2Permission
{
    param(
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string]$ClientId, 
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string]$ConsentType,
        [string]$PrincipalId,
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string]$ResourceId,
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string]$Scope
    ) 
    # check existense
    $existingEntry = Get-AzureADOAuth2PermissionGrant -All $true | ? {$_.ClientId -eq $sp.ObjectId -and $_.ResourceId -eq $targetSp.ObjectId -and $_.Scope -eq $Scope }

    if ($existingEntry)
    {
        Write-Verbose "Permission '$scopeValue' on '$($targetSp.appDisplayName)' to '$($sp.appDisplayName)' has been granted! Update it."
        Remove-AzureADOAuth2PermissionGrant -ObjectId $existingEntry.ObjectId
    }
    Add-AzureAdOauth2PermissionGrant -ClientId $ClientId -ConsentType $ConsentType -PrincipalId $PrincipalId -ResourceId $ResourceId -Scope $Scope
}

function Add-AzureAdOauth2PermissionGrant
{
    param(
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string]$ClientId, 
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string]$ConsentType,
        [string]$PrincipalId,
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string]$ResourceId,
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string]$Scope

    ) 
    [string]$tenantId = ((Get-AzureADCurrentSessionInfo).Tenant.Id)
    # get access token for Graph API
    Write-Host "Get access token to access Graph API"
    $accessToken = Get-AzAccessToken -ResourceUrl https://graph.microsoft.com/ -TenantId $tenantId  
    $body = 
    @{
          clientId     =   $ClientId
          consentType  =   $ConsentType
          resourceId   =   $ResourceId
          scope        =   $Scope 
     }
     
     if (-not [string]::IsNullOrEmpty($PrincipalId))
     {
        $body.Add("principalId",$PrincipalId)
     }

           
    $header =
    @{
        Authorization  = "Bearer $($accessToken.Token)"                    
        'Content-Type' = 'application/json'
    }

    $response = Invoke-RestMethod "https://graph.microsoft.com/v1.0/oauth2PermissionGrants" -Method Post -Body ($body | ConvertTo-Json) -Headers $header 
    Write-Host "Permission '$scopeValue' on '$($targetSp.appDisplayName)' to '$($sp.appDisplayName)' is granted!"
    return $response
}