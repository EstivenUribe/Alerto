namespace Alerto.IntegrationTests;

[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<AlertoApiFactory>
{
    public const string Name = "alerto-api";
}
