using System.Net.Http.Json;
using System.Text.Json.Serialization;
using NesterApp.Models;

namespace NesterApp.Services;

public class JsonPlaceholderService(HttpClient httpClient)
{
  public async Task<Post[]> GetPostsAsync(CancellationToken token)
  {
    return await httpClient.GetFromJsonAsync(
             "https://jsonplaceholder.typicode.com/posts",
             JsonPlaceholderJsonContext.Default.PostArray, token)
           ?? [];
  }

  public async Task<Post?> GetPostAsync(int id, CancellationToken token)
  {
    return await httpClient.GetFromJsonAsync(
             $"https://jsonplaceholder.typicode.com/posts/{id}",
             JsonPlaceholderJsonContext.Default.Post, token);
  }

  public async Task<Comment[]> GetCommentsAsync(int postId, CancellationToken token)
  {
    return await httpClient.GetFromJsonAsync(
             $"https://jsonplaceholder.typicode.com/posts/{postId}/comments",
             JsonPlaceholderJsonContext.Default.CommentArray, token)
           ?? [];
  }
}

[JsonSourceGenerationOptions(
  PropertyNameCaseInsensitive = true,
  PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(Post))]
[JsonSerializable(typeof(Post[]))]
[JsonSerializable(typeof(Comment[]))]
internal sealed partial class JsonPlaceholderJsonContext : JsonSerializerContext;
