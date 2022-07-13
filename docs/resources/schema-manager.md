# DICOM Schema Manager

#### What is it?
Schema Manager is a command line app that upgrades the schema in your database from one version to the next through migration scripts.

------------

#### How do you use it?
DICOM Schema Manager currently has one command (**apply**) with the following options:

| Option | Description |
| ------------ | ------------ |
| `-cs, --connection-string` | The connection string of the SQL server to apply the schema update. (REQUIRED) |
| `-mici, --managed-identity-client-id` | The client ID of the managed identity to be used. |
| `-at, --authentication-type` | The authentication type to use. Valid values are `ManagedIdentity` and `ConnectionString`. |
| `-v, --version` | Applies all available versions from the current schema version to the specified version. |
| `-n, --next` | Applies the next available version. |
| `-l, --latest` | Applies all available versions from the current schema version to the latest. |
| `-f, --force` | The schema migration is run without validating the schema version. |
| `-?, -h, --help` | Show help and usage information. |

You can view the most up-to-date options by running the following command:
`.\Microsoft.Health.Dicom.SchemaManager.Console.exe apply -?`

Example command line usage:
`.\Microsoft.Health.Dicom.SchemaManager.Console.exe apply --connection-string "server=(local);Initial Catalog=DICOM;TrustServerCertificate=True;Integrated Security=True" --version 20`

`.\Microsoft.Health.Dicom.SchemaManager.Console.exe apply -cs "server=(local);Initial Catalog=DICOM;TrustServerCertificate=True;Integrated Security=True" --latest`

------------

#### Terminology

Available version: Any version greater than or equal to the current version.

Compatible version: Any version from SchemaVersionConstants.Min to SchemaVersionConstants.Max (inclusive).

------------

#### Important Database Tables
SchemaVersion
- This table holds all schema versions that have been applied to the database.

InstanceSchema
- Each DICOM instance reports the schema version it is at, as well as the versions it is compatible with, to the InstanceSchema database table.

------------

#### How does it work?

Schema Manager runs through the following steps:
1. Verifies all arguments are supplied and valid.
2. Calls the [healthcare-shared-components ApplySchema function](https://github.com/microsoft/healthcare-shared-components/blob/20506ffba19905abe882812a25d74866d1e1dcb0/src/Microsoft.Health.SqlServer/Features/Schema/Manager/SqlSchemaManager.cs#L53), which:
	1. Ensures the base schema exists.
	2. Ensures instance schema records exist.
		1. Since DICOM Server implements its own ISchemaClient (DicomSchemaClient), if there are no instance schema records, the upgrade continues uninterrupted. In healthcare-shared-components, this would throw an exception and cancel the upgrade.
	3. Gets all available versions and compares them against all compatible versions.
	4. Based on the current schema version:
		1. If there is no version (base schema only), the latest full migration script is applied.
		2. If the current version is >= 1, each available version is applied (excluding the current version) one at a time until the database's schema version reaches the desired version input by the user (latest, next, or a specific version).

------------

#### SQL Script Locations

- [Base Schema Script](https://github.com/microsoft/healthcare-shared-components/blob/main/src/Microsoft.Health.SqlServer/Features/Schema/Migrations/BaseSchema.sql)

- [DICOM Migration Scripts](https://github.com/microsoft/dicom-server/tree/main/src/Microsoft.Health.Dicom.SqlServer/Features/Schema/Migrations)