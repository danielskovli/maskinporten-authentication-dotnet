using System.Text.Json.Serialization;

namespace SampleWebApp.HttpClients.Models;

public record SkattQueryDTO
{
    [JsonPropertyName("treff")]
    public int Treff { get; set; }

    [JsonPropertyName("rader")]
    public int Rader { get; set; }

    [JsonPropertyName("offset")]
    public int Offset { get; set; }

    [JsonPropertyName("nesteSide")]
    public int NesteSide { get; set; }

    [JsonPropertyName("seed")]
    public int Seed { get; set; }

    [JsonPropertyName("dokumentListe")]
    public IEnumerable<DokumentListe> DokumentListe { get; set; }

    [JsonPropertyName("fasetter")]
    public object Fasetter { get; set; }
}

public record DokumentListe
{
    [JsonPropertyName("tenorMetadata")]
    public TenorMetadata TenorMetadata { get; set; }
}

public record TenorMetadata
{
    [JsonPropertyName("id")]
    public string Id { get; set; }
}
