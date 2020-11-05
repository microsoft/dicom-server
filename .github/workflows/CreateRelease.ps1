# Function to help log out useful information.
function log ([parameter(mandatory)][String]$msg) { Add-Content log.txt $msg }

# Generate a log file.
New-Item ./log.txt -ItemType File -Force

$apiVersion = "api-version=6.0"

# Releases are only created every other week even though the action occurs every week.
# Identify if this is the second week of a sprint.
$currentDate = (get-date).ToUniversalTime().Date
$firstRun = (get-date -Date "10/23/2020 00:00:00Z").ToUniversalTime().date
$weeksSince = [math]::floor(($currentDate - $firstRun).TotalDays / 7)
$shouldRelease = ($weeksSince % 2) -eq 0

if($shouldRelease) {
    log "We're not releasing this week, one week left!"
    exit
}

# Set basis to call AzureDevOps' Rest API as necessary.
[Net.ServicePointManager]::SecurityProtocol =  [Net.SecurityProtocolType]::Tls12
$azureDevOpsAuthenicationHeader = @{Authorization = 'Basic ' + [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes(":$($env:AZUREDEVOPS_PAT)")) }
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
    $approvalsUri = $apiBase + "/fhirserver/_apis/release/approvals?statusFilter=pending&" + $apiVersion
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
        $pendingApprovals = "Pending releases for approval: "
        foreach ($pendingApproval in $approval) {
            $pendingApprovals += $pendingApproval.release.name + " "
        }

        $multipleApprovalsError = "Error: More than 1 approval at a time was unexpected $pendingApprovals"
        log $multipleApprovalsError
        throw $multipleApprovalsError
    }
    elseif($approval.Count -eq 0) {
        # If there are no pending approvals, exit as there are no new changes to release.
        log "No pending approvals, nothing to do."
        exit
    }

    # If there is exactly one release, identify relevant properties to base further queries on.
    $currentRelease = $approval[0].release.id
    $currentReleaseUrl = $approval[0].release.url + "?" + $apiVersion

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
        $patchError = "Error: Patch request to update $($($($approval[0]).release).name) as $($updateStatusObj.Status) failed with: $_"
        log $patchError
        throw $patchError
    }
}