param
(
    [Parameter(Mandatory=$True)]
    [string]
    $AppName,

    [Parameter(Mandatory=$True)]
    [string]
    $AppUrl,

    [Parameter(Mandatory=$True)]
    [string]
    $UserObjectId
)

$app = Get-AzureADApplication -Filter "DisplayName eq '$AppName'"
if (-not $app)
{
    # Create the app if it doesn't exist already
    Write-Output "Creating application '$AppName' for URL '$AppUrl'"
    $app = New-AzureADApplication -DisplayName $AppUrl -IdentifierUris $AppUrl
}

Set-AzureADServicePrincipal -ObjectId $app.ObjectId -AppRoleAssignmentRequired $True

$user = Get-AzureADUserAppRoleAssignment -ObjectId $UserObjectId
if (-not $user)
{
    # Add user with default role
    Write-Output "Adding user '$UserObjectId' to application '$AppName'"
    $user = New-AzureADUserAppRoleAssignment -ObjectId $UserObjectId -PrincipalId $UserObjectId -ResourceId $app.ObjectId -Id ([Guid]::Empty)
}

Write-Output "Successfully set up application '$AppName' with user '$UserObjectId'"
$DeploymentScriptOutputs = @{}
$DeploymentScriptOutputs['clientId'] = $app.AppId
$DeploymentScriptOutputs['clientSecret'] = Get-AzureADServicePrincipalKeyCredential -ObjectId $ServicePrincipalId
