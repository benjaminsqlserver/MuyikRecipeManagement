namespace RecipeManagement.IntegrationTests;

using RecipeManagement.Extensions.Services;
using RecipeManagement.Databases;
using RecipeManagement.Resources;
using RecipeManagement.SharedTestHelpers.Utilities;
using Configurations;
using FluentAssertions;
using FluentAssertions.Extensions;
using Hangfire;
using NSubstitute;
using System.Runtime.InteropServices;
using Testcontainers.MsSql;
using Testcontainers.SqlEdge;
using Testcontainers.RabbitMq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

[CollectionDefinition(nameof(TestFixture))]
public class TestFixtureCollection : ICollectionFixture<TestFixture> {}

public class TestFixture : IAsyncLifetime
{
    public static IServiceScopeFactory BaseScopeFactory;
    private SqlEdgeContainer _edgeContainer;
    private MsSqlContainer _msSqlContainer;
    private RabbitMqContainer _rmqContainer;

    public async Task InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Consts.Testing.IntegrationTestingEnvName
        });

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
        
        builder.Configuration.GetSection(ConnectionStringOptions.SectionName)[ConnectionStringOptions.RecipeManagementKey] = connection;
        await RunMigration(connection);

        var freePort = DockerUtilities.GetFreePort();
        _rmqContainer = new RabbitMqBuilder()
            .WithPortBinding(freePort, 5672)
            .Build();
        await _rmqContainer.StartAsync();
        builder.Configuration.GetSection(RabbitMqOptions.SectionName)[RabbitMqOptions.HostKey] = "localhost";
        builder.Configuration.GetSection(RabbitMqOptions.SectionName)[RabbitMqOptions.VirtualHostKey] = "/";
        builder.Configuration.GetSection(RabbitMqOptions.SectionName)[RabbitMqOptions.UsernameKey] = "guest";
        builder.Configuration.GetSection(RabbitMqOptions.SectionName)[RabbitMqOptions.PasswordKey] = "guest";
        builder.Configuration.GetSection(RabbitMqOptions.SectionName)[RabbitMqOptions.PortKey] = _rmqContainer.GetConnectionString();

        builder.ConfigureServices();
        var services = builder.Services;

        // add any mock services here
        services.ReplaceServiceWithSingletonMock<IHttpContextAccessor>();
        services.ReplaceServiceWithSingletonMock<IBackgroundJobClient>();

        var provider = services.BuildServiceProvider();
        BaseScopeFactory = provider.GetService<IServiceScopeFactory>();
    }

    private static async Task RunMigration(string connectionString)
    {
        var options = new DbContextOptionsBuilder<RecipesDbContext>()
            .UseSqlServer(connectionString)
            .Options;
        var context = new RecipesDbContext(options, null, null, null);
        await context?.Database?.MigrateAsync();
    }

    public async Task DisposeAsync()
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

public static class ServiceCollectionServiceExtensions
{
    public static IServiceCollection ReplaceServiceWithSingletonMock<TService>(this IServiceCollection services)
        where TService : class
    {
        services.RemoveAll(typeof(TService));
        services.AddSingleton(_ => Substitute.For<TService>());
        return services;
    }
}
