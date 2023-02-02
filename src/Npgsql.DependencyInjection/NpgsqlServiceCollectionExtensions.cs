﻿using System;
using System.Data.Common;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Npgsql;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension method for setting up Npgsql services in an <see cref="IServiceCollection" />.
/// </summary>
public static class NpgsqlServiceCollectionExtensions
{
    /// <summary>
    /// Registers an <see cref="NpgsqlDataSourceOrig" /> and an <see cref="NpgsqlConnectionOrig" /> in the <see cref="IServiceCollection" />.
    /// </summary>
    /// <param name="serviceCollection">The <see cref="IServiceCollection" /> to add services to.</param>
    /// <param name="connectionString">An Npgsql connection string.</param>
    /// <param name="dataSourceBuilderAction">
    /// An action to configure the <see cref="NpgsqlDataSourceBuilder" /> for further customizations of the <see cref="NpgsqlDataSourceOrig" />.
    /// </param>
    /// <param name="connectionLifetime">
    /// The lifetime with which to register the <see cref="NpgsqlConnectionOrig" /> in the container.
    /// Defaults to <see cref="ServiceLifetime.Scoped" />.
    /// </param>
    /// <param name="dataSourceLifetime">
    /// The lifetime with which to register the <see cref="NpgsqlDataSourceOrig" /> service in the container.
    /// Defaults to <see cref="ServiceLifetime.Singleton" />.
    /// </param>
    /// <returns>The same service collection so that multiple calls can be chained.</returns>
    public static IServiceCollection AddNpgsqlDataSource(
        this IServiceCollection serviceCollection,
        string connectionString,
        Action<NpgsqlDataSourceBuilder> dataSourceBuilderAction,
        ServiceLifetime connectionLifetime = ServiceLifetime.Transient,
        ServiceLifetime dataSourceLifetime = ServiceLifetime.Singleton)
        => AddNpgsqlDataSourceCore(serviceCollection, connectionString, dataSourceBuilderAction, connectionLifetime, dataSourceLifetime);

    /// <summary>
    /// Registers an <see cref="NpgsqlDataSourceOrig" /> and an <see cref="NpgsqlConnectionOrig" /> in the <see cref="IServiceCollection" />.
    /// </summary>
    /// <param name="serviceCollection">The <see cref="IServiceCollection" /> to add services to.</param>
    /// <param name="connectionString">An Npgsql connection string.</param>
    /// <param name="connectionLifetime">
    /// The lifetime with which to register the <see cref="NpgsqlConnectionOrig" /> in the container.
    /// Defaults to <see cref="ServiceLifetime.Scoped" />.
    /// </param>
    /// <param name="dataSourceLifetime">
    /// The lifetime with which to register the <see cref="NpgsqlDataSourceOrig" /> service in the container.
    /// Defaults to <see cref="ServiceLifetime.Singleton" />.
    /// </param>
    /// <returns>The same service collection so that multiple calls can be chained.</returns>
    public static IServiceCollection AddNpgsqlDataSource(
        this IServiceCollection serviceCollection,
        string connectionString,
        ServiceLifetime connectionLifetime = ServiceLifetime.Transient,
        ServiceLifetime dataSourceLifetime = ServiceLifetime.Singleton)
        => AddNpgsqlDataSourceCore(
            serviceCollection, connectionString, dataSourceBuilderAction: null, connectionLifetime, dataSourceLifetime);

    /// <summary>
    /// Registers an <see cref="NpgsqlMultiHostDataSourceOrig" /> and an <see cref="NpgsqlConnectionOrig" /> in the
    /// </summary>
    /// <param name="serviceCollection">The <see cref="IServiceCollection" /> to add services to.</param>
    /// <param name="connectionString">An Npgsql connection string.</param>
    /// <param name="dataSourceBuilderAction">
    /// An action to configure the <see cref="NpgsqlDataSourceBuilder" /> for further customizations of the <see cref="NpgsqlDataSourceOrig" />.
    /// </param>
    /// <param name="connectionLifetime">
    /// The lifetime with which to register the <see cref="NpgsqlConnectionOrig" /> in the container.
    /// Defaults to <see cref="ServiceLifetime.Scoped" />.
    /// </param>
    /// <param name="dataSourceLifetime">
    /// The lifetime with which to register the <see cref="NpgsqlDataSourceOrig" /> service in the container.
    /// Defaults to <see cref="ServiceLifetime.Singleton" />.
    /// </param>
    /// <returns>The same service collection so that multiple calls can be chained.</returns>
    public static IServiceCollection AddMultiHostNpgsqlDataSource(
        this IServiceCollection serviceCollection,
        string connectionString,
        Action<NpgsqlDataSourceBuilder> dataSourceBuilderAction,
        ServiceLifetime connectionLifetime = ServiceLifetime.Transient,
        ServiceLifetime dataSourceLifetime = ServiceLifetime.Singleton)
        => AddNpgsqlMultiHostDataSourceOrigCore(
            serviceCollection, connectionString, dataSourceBuilderAction, connectionLifetime, dataSourceLifetime);

    /// <summary>
    /// Registers an <see cref="NpgsqlMultiHostDataSourceOrig" /> and an <see cref="NpgsqlConnectionOrig" /> in the
    /// <see cref="IServiceCollection" />.
    /// </summary>
    /// <param name="serviceCollection">The <see cref="IServiceCollection" /> to add services to.</param>
    /// <param name="connectionString">An Npgsql connection string.</param>
    /// <param name="connectionLifetime">
    /// The lifetime with which to register the <see cref="NpgsqlConnectionOrig" /> in the container.
    /// Defaults to <see cref="ServiceLifetime.Scoped" />.
    /// </param>
    /// <param name="dataSourceLifetime">
    /// The lifetime with which to register the <see cref="NpgsqlDataSourceOrig" /> service in the container.
    /// Defaults to <see cref="ServiceLifetime.Singleton" />.
    /// </param>
    /// <returns>The same service collection so that multiple calls can be chained.</returns>
    public static IServiceCollection AddMultiHostNpgsqlDataSource(
        this IServiceCollection serviceCollection,
        string connectionString,
        ServiceLifetime connectionLifetime = ServiceLifetime.Transient,
        ServiceLifetime dataSourceLifetime = ServiceLifetime.Singleton)
        => AddNpgsqlMultiHostDataSourceOrigCore(
            serviceCollection, connectionString, dataSourceBuilderAction: null, connectionLifetime, dataSourceLifetime);

    static IServiceCollection AddNpgsqlDataSourceCore(
        this IServiceCollection serviceCollection,
        string connectionString,
        Action<NpgsqlDataSourceBuilder>? dataSourceBuilderAction,
        ServiceLifetime connectionLifetime,
        ServiceLifetime dataSourceLifetime)
    {
        serviceCollection.TryAdd(
            new ServiceDescriptor(
                typeof(NpgsqlDataSourceOrig),
                sp =>
                {
                    var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
                    dataSourceBuilder.UseLoggerFactory(sp.GetService<ILoggerFactory>());
                    dataSourceBuilderAction?.Invoke(dataSourceBuilder);
                    return dataSourceBuilder.Build();
                },
                dataSourceLifetime));

        AddCommonServices(serviceCollection, connectionLifetime, dataSourceLifetime);

        return serviceCollection;
    }

    static IServiceCollection AddNpgsqlMultiHostDataSourceOrigCore(
        this IServiceCollection serviceCollection,
        string connectionString,
        Action<NpgsqlDataSourceBuilder>? dataSourceBuilderAction,
        ServiceLifetime connectionLifetime,
        ServiceLifetime dataSourceLifetime)
    {
        serviceCollection.TryAdd(
            new ServiceDescriptor(
                typeof(NpgsqlMultiHostDataSourceOrig),
                sp =>
                {
                    var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
                    dataSourceBuilder.UseLoggerFactory(sp.GetService<ILoggerFactory>());
                    dataSourceBuilderAction?.Invoke(dataSourceBuilder);
                    return dataSourceBuilder.BuildMultiHost();
                },
                dataSourceLifetime));

        serviceCollection.TryAdd(
            new ServiceDescriptor(
                typeof(NpgsqlDataSourceOrig),
                sp => sp.GetRequiredService<NpgsqlMultiHostDataSourceOrig>(),
                dataSourceLifetime));

        AddCommonServices(serviceCollection, connectionLifetime, dataSourceLifetime);

        return serviceCollection;
    }

    static void AddCommonServices(
        IServiceCollection serviceCollection,
        ServiceLifetime connectionLifetime,
        ServiceLifetime dataSourceLifetime)
    {
        serviceCollection.TryAdd(
            new ServiceDescriptor(
                typeof(NpgsqlConnectionOrig),
                sp => sp.GetRequiredService<NpgsqlDataSourceOrig>().CreateConnection(),
                connectionLifetime));

        serviceCollection.TryAdd(
            new ServiceDescriptor(
                typeof(DbDataSource),
                sp => sp.GetRequiredService<NpgsqlDataSourceOrig>(),
                dataSourceLifetime));

        serviceCollection.TryAdd(
            new ServiceDescriptor(
                typeof(DbConnection),
                sp => sp.GetRequiredService<NpgsqlConnectionOrig>(),
                connectionLifetime));
    }
}