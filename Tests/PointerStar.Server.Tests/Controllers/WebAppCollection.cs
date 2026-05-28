using PointerStar.Server.Tests;

namespace PointerStar.Server.Tests.Controllers;

[CollectionDefinition(Name)]
public class WebAppCollection : ICollectionFixture<WebApplicationFactory>
{
    public const string Name = "WebApp";
}
