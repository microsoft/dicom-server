// -------------------------------------------------------------------------------------------------
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;

namespace Microsoft.Health.Dicom.Core.Features.Reindex
{
    public interface IReindexService
    {
        Task ReindexAsync(IEnumerable<ExtendedQueryTagStoreEntry> entries, long watermark, CancellationToken cancellationToken = default);
    }
}
