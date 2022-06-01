using DicomUploaderFunction.Configuration;
using DicomUploaderFunction.Extensions;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Health.Dicom.Client;

[assembly: FunctionsStartup(typeof(DicomUploaderFunction.Startup))]

namespace DicomUploaderFunction;

public class Startup : FunctionsStartup
{
    public override void Configure(IFunctionsHostBuilder builder)
    {
        var configuration = builder.GetContext().Configuration;

        var dicomConfiguration = new DicomConfiguration();
        var dicomWebConfigurationSection = configuration.GetSection(DicomConfiguration.SectionName);
        dicomWebConfigurationSection.Bind(dicomConfiguration);

        builder.Services.AddHttpClient<IDicomWebClient, DicomWebClient>((sp, client) =>
            {
                client.BaseAddress = dicomConfiguration.Endpoint;
            })
            .AddAuthenticationHandler(builder.Services, dicomWebConfigurationSection.GetSection(AuthenticationConfiguration.SectionName), DicomConfiguration.SectionName);
    }
}