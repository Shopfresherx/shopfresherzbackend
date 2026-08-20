namespace ShopFresherz.Infrastructure.Configuration;

/// <summary>Configuration options for Elasticsearch product search.</summary>
public sealed class ElasticsearchOptions
{
    public string Uri { get; init; } = "http://localhost:9200";
    public string ApiKey { get; init; } = string.Empty;
}
