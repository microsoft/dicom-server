# Required parameter specifying the secret access token to view releases and manage approvals.
param([Parameter(Mandatory=$true)][string]$accessToken)

# Function to help log out useful information.
function log ([parameter(mandatory)][String]$msg) { Add-Content log.txt $msg }

# Generate a log file.
New-Item ./log.txt -ItemType File -Force

# Releases are only created every other week even though the action occurs every week.
# Identify if this is the second week of a sprint.
$currentDate = Get-Date
$currentDate = $currentDate.ToUniversalTime().Date
$firstRun = ([datetime]::ParseExact('10-23-2020', 'MM-dd-yyyy', $null)).Date
$weeksSince = [math]::floor(($currentDate - $firstRun).TotalDays / 7)
$shouldRelease = ($weeksSince % 2) -eq 0

if($shouldRelease) {
    log "We're not releasing this week, one week left!"
    exit
}

# Set basis to call AzureDevOps' Rest API as necessary.
[Net.ServicePointManager]::SecurityProtocol =  [Net.SecurityProtocolType]::Tls12
$azureDevOpsAuthenicationHeader = @{Authorization = 'Basic ' + [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes(":$($accessToken)")) }
$apiBase = "https://vsrm.dev.azure.com/microsofthealthoss"

# Hardcoded to point to Release Defenition 23 (DICOM Server Nuget and Tag Release) under the FhirServer Project
$definitionUri = $apiBase + "/7621b231-1a7d-4364-935b-2f72b911c43d/_apis/Release/definitions/23"

# Identify the latest release
$definition = Invoke-RestMethod -Uri $definitionUri -Method get -Headers $azureDevOpsAuthenicationHeader
$lastRelease = $definition.lastRelease.Id
$currentRelease = $null

while(-Not($lastRelease -eq $currentRelease)) {
    # Fetch releases currently pending for approval
    log "Fetching approvals"
    $approvalsUri = $apiBase + "/fhirserver/_apis/release/approvals?api-version=6.0"
    $approvals = Invoke-RestMethod -Uri $approvalsUri -Method get -Headers $azureDevOpsAuthenicationHeader
    $approval = @()
    foreach($instance in $approvals.value) {
        # Isolate the one associated with dicom-server
        if ($instance.releaseDefinition.id -eq "23") { 
            $approval += $instance
        }
    }

    # Validate that only one release is pending approval - any others should be approved, rejected or queued.
    if($approval.Count -gt 1) {
        throw "Error: More than 1 approval at a time was unexpected"
    }
    elseif($approval.Count -eq 0) {
        # If there are no pending approvals, exit as there are no new changes to release.
        log "No pending approvals, nothing to do."
        exit
    }

    # If there is exactly one release, identify relevant properties to base further queries on.
    $currentRelease = $approval[0].release.id
    $currentReleaseUrl = $approval[0].release.url + "?api-version=6.0"

    log "Approval found for $($($($approval[0]).release).name)"

    $updateStatusObj = $null;

    # All releases that are not the latest release are rejected.
    if(-Not($currentRelease -eq $lastRelease)) {
        $updateStatusObj = @{
            status="rejected"
            comment="Rejected by automation (reason: not the latest)"
        }

        log "Rejecting $($($($approval[0]).release).name) as it is not the latest."
    }
    else {
        # Only the latest release will be approved.
        $updateStatusObj = @{
            status="approved"
            comment="Approved by automation"
        }

        log "Accepting $($($($approval[0]).release).name)"
    }
    
    try {
        # Make a patch request to the AzureDevOps' API to approve the release.
        $patch = Invoke-WebRequest -Uri $currentReleaseUrl -Method patch -Headers $azureDevOpsAuthenicationHeader -ContentType "application/json" -Body $updateStatusObj -ErrorAction Stop
        Start-Sleep -Milliseconds 10000
    }
    catch {
        # Any errors are thrown and will be handled by the github action.
        throw "Error: Patch request to update $($($($approval[0]).release).name) as $($updateStatusObj.Status) failed with: $_.Exception.patch.StatusCode.value_"
    }
}