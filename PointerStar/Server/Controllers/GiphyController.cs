using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Text.Json;

namespace PointerStar.Server.Controllers;

/// <summary>
/// Controller for Giphy-related API operations.
/// Handles searching Giphy with rate-limiting and caching.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class GiphyController(IHttpClientFactory httpClientFactory, IMemoryCache memoryCache) : ControllerBase
{
    private const string GiphyApiKey = "YOUR_GIPHY_API_KEY"; // TODO: Use user secrets in production
    private const string GiphyApiBaseUrl = "https://api.giphy.com/v1";
    private const int RateLimitPerMinute = 10;
    private const int CacheDurationMinutes = 5;

    private static readonly ConcurrentDictionary<string, DateTime[]> UserSearchTimestamps = new();
    private IHttpClientFactory HttpClientFactory { get; } = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
    private IMemoryCache MemoryCache { get; } = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));

    /// <summary>
    /// Search Giphy for images matching the query.
    /// Rate-limited to 10 searches per user per minute.
    /// Results cached for 5 minutes.
    /// </summary>
    [HttpGet("Search")]
    public async Task<ActionResult<GiphySearchResponse>> Search([FromQuery] string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return BadRequest(new { error = "Query parameter is required" });
        }

        // Rate limiting: check if user has exceeded the limit
        string userId = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        if (!IsWithinRateLimit(userId))
        {
            return StatusCode(429, new { error = "Rate limit exceeded. Maximum 10 searches per minute." });
        }

        // Check cache first
        string cacheKey = $"giphy_search_{query.ToLower()}";
        if (MemoryCache.TryGetValue(cacheKey, out GiphySearchResponse? cachedResult))
        {
            return cachedResult!;
        }

        try
        {
            using var httpClient = HttpClientFactory.CreateClient();
            string url = $"{GiphyApiBaseUrl}/gifs/search?api_key={GiphyApiKey}&q={Uri.EscapeDataString(query)}&limit=20&offset=0&rating=g&lang=en";

            using var response = await httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                // Graceful failure: return empty results if Giphy API is unavailable
                var emptyResult = new GiphySearchResponse { Data = [], Pagination = null };
                return Ok(emptyResult);
            }

            var content = await response.Content.ReadAsStringAsync();
            using var jsonDoc = JsonDocument.Parse(content);
            var root = jsonDoc.RootElement;

            // Parse the response and extract GIFs
            var result = new GiphySearchResponse
            {
                Data = ExtractGiphyIds(root),
                Pagination = new PaginationInfo { Count = 20 }
            };

            // Cache the result
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CacheDurationMinutes)
            };
            MemoryCache.Set(cacheKey, result, cacheOptions);

            return Ok(result);
        }
        catch
        {
            // Log the exception and return empty results
            // In production, this should be logged properly
            var emptyResult = new GiphySearchResponse { Data = [], Pagination = null };
            return Ok(emptyResult);
        }
    }

    /// <summary>
    /// Checks if the user is within the rate limit.
    /// Maintains a sliding window of timestamps for each user.
    /// </summary>
    private static bool IsWithinRateLimit(string userId)
    {
        var now = DateTime.UtcNow;
        var timestamps = UserSearchTimestamps.GetOrAdd(userId, _ => []);

        // Remove timestamps older than 1 minute
        var recentTimestamps = timestamps
            .Where(ts => (now - ts).TotalSeconds < 60)
            .ToArray();

        // Update the stored timestamps
        UserSearchTimestamps[userId] = recentTimestamps;

        if (recentTimestamps.Length >= RateLimitPerMinute)
        {
            return false;
        }

        // Record this search
        var newTimestamps = recentTimestamps.Append(now).ToArray();
        UserSearchTimestamps[userId] = newTimestamps;

        return true;
    }

    /// <summary>
    /// Extracts Giphy IDs from the API response.
    /// </summary>
    private static List<GiphyItem> ExtractGiphyIds(JsonElement root)
    {
        var items = new List<GiphyItem>();

        try
        {
            if (root.TryGetProperty("data", out var dataArray))
            {
                foreach (var gif in dataArray.EnumerateArray())
                {
                    string? id = null;
                    string? title = null;
                    string? imageUrl = null;

                    // Extract ID
                    if (gif.TryGetProperty("id", out var idElement))
                    {
                        id = idElement.GetString();
                    }

                    // Extract title (prefer 'title', fallback to 'slug')
                    if (gif.TryGetProperty("title", out var titleElement) && titleElement.ValueKind != JsonValueKind.Null)
                    {
                        title = titleElement.GetString();
                    }
                    else if (gif.TryGetProperty("slug", out var slugElement) && slugElement.ValueKind != JsonValueKind.Null)
                    {
                        title = slugElement.GetString();
                    }
                    
                    if (string.IsNullOrEmpty(title))
                    {
                        title = "Untitled";
                    }

                    // Extract image URL from images.fixed_height.url
                    if (gif.TryGetProperty("images", out var imagesElement) &&
                        imagesElement.TryGetProperty("fixed_height", out var fixedHeightElement) &&
                        fixedHeightElement.TryGetProperty("url", out var urlElement))
                    {
                        imageUrl = urlElement.GetString();
                    }

                    if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(imageUrl))
                    {
                        items.Add(new GiphyItem
                        {
                            Id = id,
                            Title = title ?? "Untitled",
                            ImageUrl = imageUrl
                        });
                    }
                }
            }
        }
        catch
        {
            // If parsing fails, return empty list
        }

        return items;
    }
}

/// <summary>
/// Response model for Giphy search results.
/// </summary>
public class GiphySearchResponse
{
    public List<GiphyItem> Data { get; set; } = [];
    public PaginationInfo? Pagination { get; set; }
}

/// <summary>
/// Represents a single Giphy GIF result.
/// </summary>
public class GiphyItem
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
}

/// <summary>
/// Pagination information for search results.
/// </summary>
public class PaginationInfo
{
    public int Count { get; set; }
    public int Offset { get; set; }
    public int TotalCount { get; set; }
}
