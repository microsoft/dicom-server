# Developing
## Requirements
- [Azure storage emulator](https://go.microsoft.com/fwlink/?linkid=717179)
- SQL Server 2019 with Full text index feature
- .NET core SDK version specified [here](/global.json)
   - https://dotnet.microsoft.com/download/dotnet-core/3.1 

## Getting Started in Visual Studio
### Developing
- Install Visual Studio 2019
- [Clone the Medical Imaging Server for DICOM repo](https://github.com/microsoft/dicom-server.git)
- Navigate to the cloned repository
- Open Microsoft.Health.Dicom.sln in VS
- Build
- Make sure the storage emulator is running
- Run all tests from the Test explorer

# Testing
- Set Microsoft.Health.Dicom.Web as your startup project
- Run the project
- Web server is now running at https://localhost:63838/

## Posting DICOM files using Fiddler
- [Install fiddler](https://www.telerik.com/download/fiddler)
- Go to Tools->Options->HTTPS on fiddler. Click protocols and add "tls1.2" to the list of protocols.

![Fiddler Config Image](/docs/images/FiddlerConfig.png)
- Download DCM example file from [here](/docs/dcms) 
- Upload DCM file 
   - Use `Upload file...` link at request body section as shown in picture below
      - Located in `Parsed` tab inside the `Composer` tab
- Update request header:
   - `Accept: application/dicom+json`
   - `Content-Type: multipart/related` **(don't change boundary part)**
- Update request body:
   - `Content-Type: application/dicom`
- Post the request to https://localhost:63838/studies
   - Hit Execute button

![Post A Dicom Image](/docs/images/FiddlerPost.png)
- If the POST is successful, you should be see an HTTP 200 response.

![Post Succeeds](/docs/images/FiddlerSucceedPost.png)
- Note: you cannot upload the same DCM file again unless it is first deleted. Doing so will result in an HTTP 409 Conflict error.

## Posting DICOM files using Postman
Postman only supports uploading DICOM files using the single part payload defined in the DICOM standard. This is because Postman cannot support custom separators in a multipart/related POST request.
- [Install Postman](https://www.postman.com/downloads/)
- Set the request to be a post request to the url `https://localhost:63838/studies`
- In the `Body` tab of Postman select `binary` and and then select the single part DICOM file you would like to upload
- In the headers section add the following. All the headers should match as in the image below 
   - Accept: `application/dicom+json`
   - Content-Type: `application/dicom`
- Click `Send`, a successful post will result in a 200 OK

![Postman headers](/docs/images/postman-singlepart-headers.PNG)

## Postman for Get
- Example QIDO to get all studies
```http
GET https://localhost:63838/studies
accept: application/dicom+json
```
