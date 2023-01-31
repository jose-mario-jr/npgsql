using System;
using System.Collections.Generic;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Npgsql.Internal.TypeHandling;
using Npgsql.Internal.TypeMapping;

namespace Npgsql;

/// <summary>
/// Represents the configuration options for a Npgsql data source.
/// </summary>
public sealed record NpgsqlDataSourceConfiguration(
    NpgsqlLoggingConfiguration LoggingConfiguration,
    RemoteCertificateValidationCallback? UserCertificateValidationCallback,
    Action<X509CertificateCollection>? ClientCertificatesCallback,
    Func<NpgsqlConnectionStringBuilder, CancellationToken, ValueTask<string>>? PeriodicPasswordProvider,
    TimeSpan PeriodicPasswordSuccessRefreshInterval,
    TimeSpan PeriodicPasswordFailureRefreshInterval,
    List<TypeHandlerResolverFactory> ResolverFactories,
    Dictionary<string, IUserTypeMapping> UserTypeMappings,
    INpgsqlNameTranslator DefaultNameTranslator,
    Action<NpgsqlConnectionOrig>? ConnectionInitializer,
    Func<NpgsqlConnectionOrig, Task>? ConnectionInitializerAsync);
