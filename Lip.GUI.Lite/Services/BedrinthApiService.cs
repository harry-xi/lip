using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Lip.GUI.Lite.Services
{
    public class BedrinthApiService
    {
        private const string ApiUrl = "https://api.bedrinth.com/v3";
        private readonly HttpClient _httpClient;
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public BedrinthApiService(HttpClient? httpClient = null)
        {
            _httpClient = httpClient ?? new HttpClient();
        }

        public async Task<SearchPackagesResponse> SearchPackagesAsync(
            string? q = null,
            int? perPage = null,
            int? page = null,
            string? sort = null,
            string? order = null)
        {
            if (q != "") {
                q = "platform:levilamina " + q;
            } else
            {
                q = "platform:levilamina";
            }
                var url = $"{ApiUrl}/packages";
            var query = new List<string>();
            if (!string.IsNullOrEmpty(q)) query.Add($"q={Uri.EscapeDataString(q)}");
            if (perPage.HasValue) query.Add($"perPage={perPage.Value}");
            if (page.HasValue) query.Add($"page={page.Value}");
            if (!string.IsNullOrEmpty(sort)) query.Add($"sort={sort}");
            if (!string.IsNullOrEmpty(order)) query.Add($"order={order}");
            if (query.Count > 0) url += "?" + string.Join("&", query);


            Debug.WriteLine("Send request to Bedrinth API: " + url);
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            Debug.WriteLine(json);
            var data = JsonSerializer.Deserialize<ApiResponse<SearchPackagesResponse>>(json, _jsonOptions);
            return data?.Data ?? new SearchPackagesResponse();
        }

        public async Task<GetPackageResponse> GetPackageAsync(string identifier)
        {
            var url = $"{ApiUrl}/packages/{Uri.EscapeDataString(identifier)}";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                var errorJson = await response.Content.ReadAsStringAsync();
                var err = JsonSerializer.Deserialize<ResponseErr>(errorJson, _jsonOptions);
                throw new BedrinthApiException(err?.Message ?? "Unknown error", err?.Code ?? (int)response.StatusCode);
            }

            var json = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<ApiResponse<GetPackageResponse>>(json, _jsonOptions);
            return data?.Data ?? throw new BedrinthApiException("No data returned", (int)response.StatusCode);
        }

        public async Task<string> FetchGithubReadmeAsync(string identifier)
        {
            var url = $"https://raw.githubusercontent.com/{identifier}/HEAD/README.md";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                throw new BedrinthApiException($"Fail to get GitHub README: {response.StatusCode}", (int)response.StatusCode);
            }

            return await response.Content.ReadAsStringAsync();
        }
    }

    public record ApiResponse<T>
    {
        public required T Data { get; init; }
    }

    public record SearchPackagesResponse
    {
        public int PageIndex { get; init; } = 0;
        public int TotalPages { get; init; } = 0;
        public List<SearchPackageItem> Items { get; init; } = new();
    }

    public record SearchPackageItem
    {
        public string Identifier { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public string Author { get; init; } = string.Empty;
        public List<string> Tags { get; init; } = new();
        public string AvatarUrl { get; init; } = string.Empty;
        public string ProjectUrl { get; init; } = string.Empty;
        public double Hotness { get; init; } = 0;
        public string Updated { get; init; } = string.Empty;
    }

    public record GetPackageResponse
    {
        public string Identifier { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public string Author { get; init; } = string.Empty;
        public List<string> Tags { get; init; } = new();
        public string AvatarUrl { get; init; } = string.Empty;
        public string ProjectUrl { get; init; } = string.Empty;
        public double Hotness { get; init; } = 0;
        public string Updated { get; init; } = string.Empty;
        public List<Contributor> Contributors { get; init; } = new();
        public List<Version> Versions { get; init; } = new();
    }

    public record Version
    {
        public string VersionNumber { get; init; } = string.Empty;
        public string ReleasedAt { get; init; } = string.Empty;
        public string Source { get; init; } = string.Empty;
        public string PackageManager { get; init; } = string.Empty;
        public string PlatformVersionRequirement { get; init; } = string.Empty;
    }

    public record Contributor
    {
        public string Username { get; init; } = string.Empty;
        public int Contributions { get; init; } = 0;
    }

    public record ResponseErr
    {
        public int Code { get; init; } = 0;
        public string Message { get; init; } = string.Empty;
    }

    public class BedrinthApiException : Exception
    {
        public int Code { get; }
        public BedrinthApiException(string message, int code) : base(message)
        {
            Code = code;
        }
    }
}