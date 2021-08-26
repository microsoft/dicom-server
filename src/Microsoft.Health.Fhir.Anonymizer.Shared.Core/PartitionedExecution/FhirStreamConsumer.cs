using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Health.Fhir.Anonymizer.Core.PartitionedExecution
{
    public class FhirStreamConsumer : IFhirDataConsumer<string>, IDisposable
    {
        private StreamWriter _writer;

        public FhirStreamConsumer(Stream stream)
        {
            _writer = new StreamWriter(stream);
        }

        public async Task CompleteAsync()
        {
            await _writer.FlushAsync().ConfigureAwait(false);
        }

        public async Task<int> ConsumeAsync(IEnumerable<string> data)
        {
            int result = 0;
            foreach (string content in data)
            {
                await _writer.WriteLineAsync(content).ConfigureAwait(false);
                result++;
            }

            return result;
        }

        #region IDisposable Support
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _writer?.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
