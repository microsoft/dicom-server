function New-DicomServerApiApplicationRegistration {
    <#
    .SYNOPSIS
    Create an AAD Application registration for a Dicom server instance.
    .DESCRIPTION
    Create a new AAD Application registration for a Dicom server instance. 
    A DicomServiceName or DicomServiceAudience must be supplied.
    .EXAMPLE
    New-DicomServerApiApplicationRegistration -DicomServiceName "mydicomservice" -AppRoles admin,nurse
    .EXAMPLE
    New-DicomServerApiApplicationRegistration -DicomServiceAudience "https://mydicomservice.resoluteopensource.onmicrosoft.com" -AppRoles admin,nurse
    .PARAMETER DicomServiceName
    Name of the Dicom service instance. 
    .PARAMETER DicomServiceAudience
    Full URL of the Dicom service.
    .PARAMETER TenantIdDomain
    TenantId domain ("*.onmicrosoft.com") used for creating service audience while creating AAD application.
    .PARAMETER AppRoles
    Names of AppRoles to be defined in the AAD Application registration
    #>
    [CmdletBinding(DefaultParameterSetName='ByDicomServiceName')]
    param(
        [Parameter(Mandatory = $true, ParameterSetName = 'ByDicomServiceName' )]
        [ValidateNotNullOrEmpty()]
        [string]$DicomServiceName,

        [Parameter(Mandatory = $true, ParameterSetName = 'ByDicomServiceAudience' )]
        [ValidateNotNullOrEmpty()]
        [string]$DicomServiceAudience,

        [Parameter(Mandatory = $true, ParameterSetName = 'ByDicomServiceName' )]
        [ValidateNotNullOrEmpty()]
        [String]$TenantIdDomain,

        [Parameter(Mandatory = $false)]
        [String[]]$AppRoles = "admin"
    )

    Set-StrictMode -Version Latest
    
    # Get current AzureAd context
    try {
        Get-AzureADCurrentSessionInfo -ErrorAction Stop | Out-Null
    } 
    catch {
        throw "Please log in to Azure AD with Connect-AzureAD cmdlet before proceeding"
    }

    if ([string]::IsNullOrEmpty($DicomServiceAudience)) {
        $DicomServiceAudience = Get-ServiceAudience -ServiceName $DicomServiceName -TenantIdDomain $TenantIdDomain
    }

    $desiredAppRoles = @()
    foreach ($role in $AppRoles) {
        $id = New-Guid

        $desiredAppRoles += @{
            AllowedMemberTypes = @("User", "Application")
            Description        = $role
            DisplayName        = $role
            Id                 = $id
            IsEnabled          = "true"
            Value              = $role
        }
    }

    # Create the App Registration
    $apiAppReg = New-AzureADApplication -DisplayName $DicomServiceAudience -IdentifierUris $DicomServiceAudience -AppRoles $desiredAppRoles
    New-AzureAdServicePrincipal -AppId $apiAppReg.AppId | Out-Null

    $aadEndpoint = (Get-AzureADCurrentSessionInfo).Environment.Endpoints["ActiveDirectory"]
    $aadTenantId = (Get-AzureADCurrentSessionInfo).Tenant.Id.ToString()

    #Return Object
    @{
        AppId     = $apiAppReg.AppId;
        TenantId  = $aadTenantId;
        Authority = "$aadEndpoint$aadTenantId";
        Audience  = $DicomServiceAudience;
    }
}
