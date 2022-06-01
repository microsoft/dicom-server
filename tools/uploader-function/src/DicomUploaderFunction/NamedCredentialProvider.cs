using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Health.Client;

namespace DicomUploaderFunction;

public class NamedCredentialProvider : ICredentialProvider
{
    private readonly ICredentialProvider _credentialProvider;

    public NamedCredentialProvider(string name, ICredentialProvider credentialProvider)
    {
        EnsureArg.IsNotNull(name, nameof(name));
        EnsureArg.IsNotNull(credentialProvider, nameof(credentialProvider));

        Name = name;
        _credentialProvider = credentialProvider;
    }

    public string Name { get; }

    public Task<string> GetBearerToken(CancellationToken cancellationToken)
    {
        return _credentialProvider.GetBearerToken(cancellationToken);
    }
}