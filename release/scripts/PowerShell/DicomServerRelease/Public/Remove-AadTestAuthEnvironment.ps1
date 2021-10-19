function Remove-AadTestAuthEnvironment {
    <#
    .SYNOPSIS
    Removes the AAD components for the test environment in AAD.
    .DESCRIPTION
    .PARAMETER TestAuthEnvironmentPath
    Path for the testauthenvironment.json file
    .PARAMETER EnvironmentName
    Environment name used for the test environment. This is used throughout for making names unique.
    .PARAMETER TenantIdDomain
    TenantId domain ("*.onmicrosoft.com") used for creating service audience while creating AAD application.
    #>
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string]$TestAuthEnvironmentPath,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string]$EnvironmentName,
        
        [Parameter(Mandatory = $true )]
        [ValidateNotNullOrEmpty()]
        [String]$TenantIdDomain
    )

    Set-StrictMode -Version Latest
    
    # Get current AzureAd context
    try {
        $tenantInfo = Get-AzureADCurrentSessionInfo -ErrorAction Stop
    } 
    catch {
        throw "Please log in to Azure AD with Connect-AzureAD cmdlet before proceeding"
    }

    Write-Host "Tearing down test authorization environment for AAD"

    $testAuthEnvironment = Get-Content -Raw -Path $TestAuthEnvironmentPath | ConvertFrom-Json

    $dicomServiceAudience = Get-ServiceAudience -ServiceName $EnvironmentName -TenantIdDomain $TenantIdDomain

    $application = Get-AzureAdApplicationByIdentifierUri $dicomServiceAudience

    if ($application) {
        Write-Host "Removing API application $dicomServiceAudience"
        Remove-AzureAdApplication -ObjectId $application.ObjectId | Out-Null
    }

    foreach ($user in $testAuthEnvironment.Users) {
        $upn = Get-UserUpn -EnvironmentName $EnvironmentName -UserId $user.Id -TenantDomain $tenantInfo.TenantDomain
        $aadUser = Get-AzureAdUser -Filter "userPrincipalName eq '$upn'"

        if ($aadUser) {
            Write-Host "Removing user $upn"
            Remove-AzureAdUser -ObjectId $aadUser.Objectid | Out-Null
        }
    }

    foreach ($clientApp in $testAuthEnvironment.ClientApplications) {
        $displayName = Get-ApplicationDisplayName -EnvironmentName $EnvironmentName -AppId $clientApp.Id
        $aadClientApplication = Get-AzureAdApplicationByDisplayName $displayName
        
        if ($aadClientApplication) {
            Write-Host "Removing application $displayName"
            Remove-AzureAdApplication -ObjectId $aadClientApplication.ObjectId | Out-Null
        }
    }
}
