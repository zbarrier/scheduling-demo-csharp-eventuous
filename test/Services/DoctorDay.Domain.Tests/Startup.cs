using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Xunit.DependencyInjection;
using Xunit.DependencyInjection.Logging;

namespace DoctorDay.Domain.Tests;
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddLogging();
    }

    public void Configure(ILoggerFactory loggerFactory, ITestOutputHelperAccessor accessor) =>
        loggerFactory.AddProvider(new XunitTestOutputLoggerProvider(accessor));
}
