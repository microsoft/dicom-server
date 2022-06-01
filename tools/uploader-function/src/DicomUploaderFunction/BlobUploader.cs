using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Dicom.Client;

namespace DicomUploaderFunction
{
    public class BlobUploader
    {
        private readonly IDicomWebClient _dicomWebClient;

        public BlobUploader(IDicomWebClient dicomWebClient)
        {
            _dicomWebClient = dicomWebClient;
        }

        [FunctionName("BlobUploader")]
        public async Task Run(
            [BlobTrigger("%sourceblobcontainer%/{name}", Connection = "sourcestorage")]
            Stream myBlob,
            string name,
            ILogger log)
        {
            var streamContent = new StreamContent(myBlob);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/dicom");
            await _dicomWebClient.StoreAsync(streamContent, partitionName: null, CancellationToken.None);
        }
    }
}