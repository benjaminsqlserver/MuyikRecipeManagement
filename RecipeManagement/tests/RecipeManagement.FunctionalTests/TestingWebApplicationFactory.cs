namespace RecipeManagement.FunctionalTests;

using RecipeManagement.Resources;
using RecipeManagement.SharedTestHelpers.Utilities;
using Configurations;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using System.Runtime.InteropServices;
using Testcontainers.MsSql;
using Testcontainers.SqlEdge;
using Testcontainers.RabbitMq;
using Microsoft.Extensions.Logging;
using Xunit;

[CollectionDefinition(nameof(TestBase))]
public class TestingWebApplicationFactoryCollection : ICollectionFixture<TestingWebApplicationFactory> { }

public class TestingWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    
    private SqlEdgeContainer _edgeContainer;
    private MsSqlContainer _msSqlContainer;
    private RabbitMqContainer _rmqContainer;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Consts.Testing.FunctionalTestingEnvName);
        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
        });
        
        builder.ConfigureAppConfiguration(configurationBuilder =>
        {
            var functionalConfig = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables()
                .Build();
            functionalConfig.GetSection("JaegerHost").Value = "localhost";
            configurationBuilder.AddConfiguration(functionalConfig);
        });

        builder.ConfigureServices(services =>
        {
        });
    }

    public async Task InitializeAsync()
    {
        var isMacOs = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        var cpuArch = RuntimeInformation.ProcessArchitecture;
        var isRunningOnMacOsArm64 = isMacOs && cpuArch == Architecture.Arm64;
        string connection;

        if (isRunningOnMacOsArm64)
        {
            _edgeContainer = new SqlEdgeBuilder().Build();
            await _edgeContainer.StartAsync();
            connection = _edgeContainer.GetConnectionString();
        }
        else
        {
            _msSqlContainer = new MsSqlBuilder().Build();
            await _msSqlContainer.StartAsync();
            connection = _msSqlContainer.GetConnectionString();
        }
        
        Environment.SetEnvironmentVariable($"{ConnectionStringOptions.SectionName}__{ConnectionStringOptions.RecipeManagementKey}", connection);
        // migrations applied in MigrationHostedService

        var freePort = DockerUtilities.GetFreePort();
        _rmqContainer = new RabbitMqBuilder()
            .WithPortBinding(freePort, 5672)
            .Build();
        await _rmqContainer.StartAsync();
        Environment.SetEnvironmentVariable($"{RabbitMqOptions.SectionName}__{RabbitMqOptions.HostKey}", "localhost");
        Environment.SetEnvironmentVariable($"{RabbitMqOptions.SectionName}__{RabbitMqOptions.VirtualHostKey}", "/");
        Environment.SetEnvironmentVariable($"{RabbitMqOptions.SectionName}__{RabbitMqOptions.UsernameKey}", "guest");
        Environment.SetEnvironmentVariable($"{RabbitMqOptions.SectionName}__{RabbitMqOptions.PasswordKey}", "guest");
        Environment.SetEnvironmentVariable($"{RabbitMqOptions.SectionName}__{RabbitMqOptions.PortKey}", _rmqContainer.GetConnectionString());
    }

    public new async Task DisposeAsync() 
    {
        try
        {
            await _msSqlContainer.DisposeAsync();
        }
        catch { /* ignore*/ }

        try
        {
            await _edgeContainer.DisposeAsync();
        }
        catch { /* ignore*/ }
        await _rmqContainer.DisposeAsync();
    }
}