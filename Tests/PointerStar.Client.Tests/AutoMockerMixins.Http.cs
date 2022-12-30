using System.Diagnostics.CodeAnalysis;
using System.Net;
using Moq.AutoMock.Resolvers;

namespace PointerStar.Client.Tests;

public interface IHttpSetup
{
    IHttpReturnsResult Returns(Func<HttpRequestMessage, HttpResponseMessage> response);
}

public interface IHttpReturnsResult
{

}

public static partial class AutoMockerMixins
{
    public static IHttpReturnsResult ReturnsJson<T>(this IHttpSetup setup, T data)
        => setup.Returns(_ => new HttpResponseMessage()
        {
            Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(data))
        });

    public static IHttpSetup SetupHttpCall(this AutoMocker mocker, Func<HttpRequestMessage, bool> matchPredicate)
    {
        if (mocker.Resolvers.OfType<HttpMockResolver>().FirstOrDefault() is not { } resolver)
        {
            resolver = new HttpMockResolver();
            InsertBefore<MockResolver>(mocker, resolver);
        }
        HttpSetup setup = new(matchPredicate);
        resolver.Setups.Add(setup);

        return setup;
    }


    public static IHttpSetup SetupHttpGet(this AutoMocker mocker,
        Func<HttpRequestMessage, bool> matchPredicate)
    {
        return mocker.SetupHttpCall(message =>
        {
            return message.Method == HttpMethod.Get && matchPredicate(message);
        });
    }

    public static IHttpSetup SetupHttpGet(this AutoMocker mocker, Uri uri)
    {
        return mocker.SetupHttpGet(message =>
        {
            if (uri.IsAbsoluteUri)
            {
                return message.RequestUri == uri;
            }
            return message.RequestUri?.AbsolutePath == uri.ToString();
        });
    }

    private static void InsertBefore<TResolver>(AutoMocker mocker, IMockResolver resolver)
        where TResolver : IMockResolver
    {
        if (mocker.Resolvers.OfType<TResolver>().FirstOrDefault() is { } mockResolver)
        {
            int insertIndex = mocker.Resolvers.IndexOf(mockResolver);
            mocker.Resolvers.Insert(insertIndex, resolver);
        }
        else
        {
            mocker.Resolvers.Add(resolver);
        }
    }

    private class HttpSetup : IHttpSetup, IHttpReturnsResult
    {
        public HttpSetup(Func<HttpRequestMessage, bool> matchPredicate)
            => MatchPredicate = matchPredicate;

        private Func<HttpRequestMessage, bool> MatchPredicate { get; }
        private Func<HttpRequestMessage, HttpResponseMessage>? ReturnsCallback { get; set; }

        public IHttpReturnsResult Returns(Func<HttpRequestMessage, HttpResponseMessage> response)
        {
            ReturnsCallback = response;
            return this;
        }

        public bool TryGetReturn(HttpRequestMessage request, [NotNullWhen(true)] out HttpResponseMessage? response)
        {
            if (MatchPredicate(request))
            {
                response = ReturnsCallback?.Invoke(request) ?? new HttpResponseMessage(HttpStatusCode.OK);
                return true;
            }
            response = null;
            return false;
        }
    }

    private class HttpMockResolver : IMockResolver
    {
        public List<HttpSetup> Setups => Handler.Setups;

        private TestHttpMessageHandler Handler { get; } = new();

        public void Resolve(MockResolutionContext context)
        {
            if (typeof(HttpClient).IsAssignableFrom(context.RequestType))
            {
                context.Value = new HttpClient(Handler) { BaseAddress = new Uri("http://localhost:4242") };
            }
            else if (typeof(HttpMessageHandler).IsAssignableFrom(context.RequestType))
            {
                context.Value = Handler;
            }
        }

        private sealed class TestHttpMessageHandler : HttpMessageHandler
        {
            public List<HttpSetup> Setups { get; } = new();
            public TestHttpMessageHandler()
            { }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                foreach (HttpSetup setup in Setups)
                {
                    if (setup.TryGetReturn(request, out HttpResponseMessage? response))
                    {
                        return Task.FromResult(response);
                    }
                }
                return Task.FromResult(new HttpResponseMessage((HttpStatusCode)418));
            }
        }
    }
}
