# Azure Active Directory Authorization

This How-to Guide shows you how to configure the authorization settings for the Medical Imaging Server for DICOM through Azure. To complete this configuration, you will:

1. **Update a resource application in Azure AD**: This resource application will be a representation of the Medical Imaging Server for DICOM that can be used to authorization and obtain tokens. The application registration will need to be updated to create appRoles. 
1. **Assign the application roles in Azure AD**: Client application registrations, users, and groups need to be assigned the roles defined on the application registration.
1. **Provide configuration to your Medical Imaging Server for DICOM**: Once the resource application is updated, you will set the authorization settings of your Medical Imaging Server for DICOM App Service.

## Prerequisites

1. **Complete the authentication configuration**: Instructions for enabling authentication can be found in the [Azure Active Directory Authentication](enable-authentication-with-tokens.md) article.

## Authorization Settings Overview

The current authorization settings exposed in configuration are the following:

```json

{
  "DicomServer" : {
    "Security": {
      "Authorization": {
        "Enabled": true,
        "RolesClaim": "role",
        "Roles": [
            <DEFINED IN ROLES.JSON>
        ]
      }
    }
  }
}
```

| Element                    | Description |
| -------------------------- | --- |
| Authorization:Enabled      | Whether or not the server has any authorization enabled. |
| Authorization:RolesClaim   | Identifies the jwt claim that contains the assigned roles. This is set automatically by the `DevelopmentIdentityProvider`. |
| Authorization:Roles        | The defined roles. The roles are defined via the `roles.json`. [Additional information can be found here](../development/roles.md) |

## Authorization setup with Azure AD

### Azure AD Instructions

#### Creating App Roles
The instructions for adding app roles to an AAD application can be found [in this documentation article](https://docs.microsoft.com/azure/active-directory/develop/howto-add-app-roles-in-azure-ad-apps). This documentation also optionally shows you how to assign an app role to an application.

The app roles created need to match the name of the roles found in the `roles.json`. 

#### Assigning Users to App Role
This can be accomplished [via the Azure Portal](https://docs.microsoft.com/en-us/azure/active-directory/manage-apps/add-application-portal-assign-users) or [via a PowerShell cmdlet](https://docs.microsoft.com/en-us/azure/active-directory/manage-apps/assign-user-or-group-access-portal#assign-users-and-groups-to-an-app-using-powershell).

### Provide configuration to your Medical Imaging Server for DICOM
1. Make sure that you have deployed the `roles.json` to your web application
1. Update the configuration to have the following two settings
    * `DicomServer:Security:Authorization:Enabled` = `true`
    * `DicomServer:Security:Authorization:RolesClaim` = `"role"`

## Summary

In this How-to Guide, you learned how to configure the authorization settings for the Medical Imaging Server for DICOM through Azure.