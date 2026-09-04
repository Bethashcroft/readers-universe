using System.Net;
using ReadersRealm.Api.Services;

namespace ReadersRealm.Api.Tests;

public class FakeCoverSource : ICoverSource
{
    public CoverResult Result { get; set; } = CoverResult.NothingThere;

    public Task<CoverResult> FindCoverAsync(
        string title,
        string author,
        string isbn,
        CancellationToken cancellationToken
    ) => Task.FromResult(Result);
}

public class StubCoverHandler : HttpMessageHandler
{
    public HttpStatusCode Status { get; set; } = HttpStatusCode.NotFound;
    public long ContentLength { get; set; }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        var response = new HttpResponseMessage(Status)
        {
            RequestMessage = request,
            Content = new ByteArrayContent(new byte[Math.Max(ContentLength, 0)]),
        };

        return Task.FromResult(response);
    }
}
