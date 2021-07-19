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
        [string]$AppId,
        [Parameter(Mandatory = $true)]
        [ValidateNotNull()]
        [pscredential]$TenantAdminCredential
    )

    Set-StrictMode -Version Latest

    Write-Host "Granting admin consent for app ID $AppId"

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
            
            # AllPrincipals indicates authorization to impersonate all users (https://docs.microsoft.com/en-us/graph/api/resources/oauth2permissiongrant?view=graph-rest-1.0#properties)
            Grant-AzureAdOauth2Permission -ClientId $sp.ObjectId -ConsentType "AllPrincipals" -ResourceId $targetSp.ObjectId -Scope $scopeValue -TenantAdminCredential $TenantAdminCredential        
            Write-Host "Permission '$scopeValue' on '$($targetSp.appDisplayName)' to '$($sp.appDisplayName)' is granted!"   
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
        [string]$Scope,
        [Parameter(Mandatory = $true)]
        [ValidateNotNull()]
        [pscredential]$TenantAdminCredential
    ) 

    # check existense
    $existingEntry = Get-AzureADOAuth2PermissionGrant -All $true | ? {$_.ClientId -eq $sp.ObjectId -and $_.ResourceId -eq $targetSp.ObjectId -and $_.Scope -eq $Scope }

    if ($existingEntry)
    {
        # remove if exist
        Remove-AzureADOAuth2PermissionGrant -ObjectId $existingEntry.ObjectId
    }
    Add-AzureAdOauth2PermissionGrant -ClientId $ClientId -ConsentType $ConsentType -PrincipalId $PrincipalId -ResourceId $ResourceId -Scope $Scope -TenantAdminCredential $TenantAdminCredential
}


function Get-GraphApiAccessToken
{
     param(
        [Parameter(Mandatory = $true)]
        [ValidateNotNull()]
        [pscredential]$TenantAdminCredential

    ) 
    [string]$tenantId = ((Get-AzureADCurrentSessionInfo).Tenant.Id)
    $username = $TenantAdminCredential.GetNetworkCredential().UserName
    $password_raw = $TenantAdminCredential.GetNetworkCredential().Password

    $adTokenUrl = "https://login.microsoftonline.com/$tenantId/oauth2/token"
    $resource = "https://graph.microsoft.com/" # microsoft graph Api

    $body = @{
          grant_type = "password"
          username   = $username
          password   = $password_raw
          resource   = $resource 
          client_id  = "1950a258-227b-4e31-a9cf-717495945fc2" # Microsoft Azure PowerShell
    }
    $response = Invoke-RestMethod -Method 'Post' -Uri $adTokenUrl -ContentType "application/x-www-form-urlencoded" -Body $body
    return $response.access_token

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
        [string]$Scope,
        [Parameter(Mandatory = $true)]
        [ValidateNotNull()]
        [pscredential]$TenantAdminCredential

    ) 

    [string]$tenantId = ((Get-AzureADCurrentSessionInfo).Tenant.Id)

    # get access token for Graph API
    $accessToken = Get-GraphApiAccessToken -TenantAdminCredential $TenantAdminCredential
    $body = @{
          clientId     =   $ClientId
          consentType  =   $ConsentType
          resourceId   =   $ResourceId
          scope        =   $Scope 
    }
     
    if (-not [string]::IsNullOrEmpty($PrincipalId))
    {
        $body.Add("principalId",$PrincipalId)
    }
           
    $header = @{
        Authorization  = "Bearer $accessToken"                    
        'Content-Type' = 'application/json'
    }

    $response = Invoke-RestMethod "https://graph.microsoft.com/v1.0/oauth2PermissionGrants" -Method Post -Body ($body | ConvertTo-Json) -Headers $header 
    return $response
}