# Health Check API

Health check API allows user to check the health of the DICOM-Server and all the underlying services.

## API Design

The health check API exposes a GET endpoint and responds with JSON content.

Verb | Route              | Returns     
:--- | :----------------- | :---------- 
GET  | /health/check      | Json Object 

## Object Model

Following is the details on the JSON object that is returned in response to the GET request:

Field         | Type   | Description
:------------ | :----- | :----------
overallStatus | string | Status `Healthy` or `Unhealthy`
details       | array  | Array of objects with details on underlying services

Objects of the `details` array have the following model:

Field         | Type   | Description
:------------ | :----- | :----------
name		  | string | Name of the service
status		  | string | Status `Healthy` or `Unhealthy`
description   | string | Description of the status

## Get Health Status

Internally, Microsoft.Extensions.Diagnostics.HealthChecks nuget is used for getting the health status.Its documentation can be found [here](https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.diagnostics.healthchecks?view=dotnet-plat-ext-3.1).

To check the health status of DICOM server, user needs to issue a GET request at /health/check. Following is a sample JSON response if all the underlying services are healthy:
```
{
	"overallStatus":"Healthy",
	"details":
	[
		{
			"name":"DicomBlobHealthCheck",
			"status":"Healthy",
			"description":"Successfully connected to the blob data store."
		},
		{
			"name":"MetadataHealthCheck",
			"status":"Healthy",
			"description":"Successfully connected to the blob data store."
		},
		{
			"name":"SqlServerHealthCheck",
			"status":"Healthy",
			"description":"Successfully connected to the data store."
		}
	]
}
```

Healthy (HTTP Status Code 200) is returned as the overall status if all the underlying services are healthy. If either of the underlying services is unhealthy, overall status of the DICOM server will be returned as unhealthy (HTTP Status Code 503).

Following is an example JSON if SQL Server service is unhealthy:
```
{
	"overallStatus":"Unhealthy",
	"details":
	[
		{
			"name":"DicomBlobHealthCheck",
			"status":"Healthy",
			"description":"Successfully connected to the blob data store."
		},
		{
			"name":"MetadataHealthCheck",
			"status":"Healthy",
			"description":"Successfully connected to the blob data store."
		},
		{
			"name":"SqlServerHealthCheck",
			"status":"Unhealthy",
			"description":"Failed to connect to the data store."
		}
	]
}
```

Details array in the response contains details of all the services and their health status.