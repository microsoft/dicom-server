#
# Module manifest for module 'DicomServerRelease'
#
@{
    RootModule        = 'DicomServerRelease.psm1'
    ModuleVersion     = '0.0.1'
    GUID              = '4e840205-d0bd-4b83-9834-f799b4625355'
    Author            = 'Microsoft Healthcare NExT'
    CompanyName       = 'https://microsoft.com'
    Description       = 'PowerShell Module for managing Azure Active Directory registrations and users for Microsoft Dicom Server for a Test Environment. This module relies on the DicomServer module, and it must be imported before use of this module'
    PowerShellVersion = '3.0'
    FunctionsToExport = 'Add-AadTestAuthEnvironment', 'Remove-AadTestAuthEnvironment', 'Set-DicomServerApiApplicationRoles', 'Set-DicomServerClientAppRoleAssignments', 'Set-DicomServerUserAppRoleAssignments'
    CmdletsToExport   = @()
    AliasesToExport   = @()    
}
    