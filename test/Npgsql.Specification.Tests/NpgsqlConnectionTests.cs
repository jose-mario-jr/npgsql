using AdoNet.Specification.Tests;

namespace Npgsql.Specification.Tests;

public sealed class NpgsqlConnectionOrigTests : ConnectionTestBase<NpgsqlDbFactoryFixture>
{
    public NpgsqlConnectionOrigTests(NpgsqlDbFactoryFixture fixture)
        : base(fixture)
    {
    }
}