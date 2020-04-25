#
# Module manifest for module 'DicomServer'
#
@{
    RootModule = 'DicomServer.psm1'
    ModuleVersion = '0.0.1'
    GUID = '953E9CB7-49D5-448E-970C-E9E41BF60A1E'
    Author = 'Microsoft Healthcare NExT'
    CompanyName = 'https://microsoft.com'
    Description = 'PowerShell Module for managing Azure Active Directory registrations and users for Microsoft Dicom Server.'
    PowerShellVersion = '3.0'
    FunctionsToExport = 'Remove-DicomServerApplicationRegistration', 'New-DicomServerClientApplicationRegistration', 'New-DicomServerApiApplicationRegistration'
    CmdletsToExport = @()
    AliasesToExport = @()    
}
    