using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace ReadersRealm.Api.Tests;

public class AvatarTests : IDisposable
{
    private readonly TestWebAppFactory _factory;
    private readonly HttpClient _client;

    public AvatarTests()
    {
        _factory = new TestWebAppFactory();
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    private record ProfileResult(string UserName, string DisplayName, string AvatarUrl);

    private record MessageResult(string Message);

    private static MultipartFormDataContent BuildUpload(
        byte[] bytes,
        string contentType,
        string fileName
    )
    {
        var fileContent = new ByteArrayContent(bytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        return new MultipartFormDataContent { { fileContent, "file", fileName } };
    }

    [Fact]
    public async Task UploadingValidImage_SetsAvatarUrl()
    {
        var user = await _client.RegisterAsync("avataruser");
        _client.Authenticate(user.Token);

        var response = await _client.PostAsync(
            "/api/auth/profile/avatar",
            BuildUpload(new byte[] { 1, 2, 3 }, "image/png", "photo.png")
        );

        response.EnsureSuccessStatusCode();
        var profile = await response.Content.ReadFromJsonAsync<ProfileResult>();

        Assert.StartsWith("/avatars/", profile!.AvatarUrl);
        Assert.EndsWith(".png", profile.AvatarUrl);

        var env = _factory.Services.GetRequiredService<IWebHostEnvironment>();
        var savedPath = Path.Combine(
            env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot"),
            "avatars",
            Path.GetFileName(profile.AvatarUrl)
        );
        Assert.True(File.Exists(savedPath));
        File.Delete(savedPath);
    }

    [Fact]
    public async Task UploadingNonImageFile_ReturnsBadRequest()
    {
        var user = await _client.RegisterAsync("avataruser");
        _client.Authenticate(user.Token);

        var response = await _client.PostAsync(
            "/api/auth/profile/avatar",
            BuildUpload(new byte[] { 1, 2, 3 }, "text/plain", "notes.txt")
        );

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<MessageResult>();
        Assert.Equal("Only JPG, PNG, or WebP images are allowed", body!.Message);
    }

    [Fact]
    public async Task UploadingOversizedImage_ReturnsBadRequest()
    {
        var user = await _client.RegisterAsync("avataruser");
        _client.Authenticate(user.Token);

        var response = await _client.PostAsync(
            "/api/auth/profile/avatar",
            BuildUpload(new byte[3 * 1024 * 1024], "image/png", "huge.png")
        );

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<MessageResult>();
        Assert.Equal("Image must be 2MB or smaller", body!.Message);
    }
}
