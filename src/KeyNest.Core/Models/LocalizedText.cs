using System.Text.Json;
using System.Text.Json.Serialization;

namespace KeyNest.Core.Models;

/// <summary>
/// 双语文本。JSON 结构中写作 { "zh-CN": "...", "en-US": "..." }，
/// 也兼容旧版直接写字符串的数据（此时两种语言使用同一文本）。
/// 读取时优先当前语言，其次中文，最后另一种语言。
/// </summary>
[JsonConverter(typeof(LocalizedTextJsonConverter))]
public sealed class LocalizedText
{
    [JsonPropertyName("zh-CN")]
    public string ZhCn { get; set; } = string.Empty;

    [JsonPropertyName("en-US")]
    public string EnUs { get; set; } = string.Empty;

    public bool HasAny => !string.IsNullOrWhiteSpace(ZhCn) || !string.IsNullOrWhiteSpace(EnUs);

    public string Get(AppLanguage language)
    {
        if (language == AppLanguage.English)
        {
            return !string.IsNullOrWhiteSpace(EnUs) ? EnUs : ZhCn;
        }

        return !string.IsNullOrWhiteSpace(ZhCn) ? ZhCn : EnUs;
    }

    public static LocalizedText From(string text) => new() { ZhCn = text, EnUs = text };
}

public sealed class LocalizedTextJsonConverter : JsonConverter<LocalizedText>
{
    public override LocalizedText Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            return LocalizedText.From(reader.GetString() ?? string.Empty);
        }

        if (reader.TokenType != JsonTokenType.StartObject)
        {
            return new LocalizedText();
        }

        var result = new LocalizedText();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                continue;
            }

            var name = reader.GetString();

            if (!reader.Read())
            {
                break;
            }

            var value = reader.TokenType == JsonTokenType.String ? reader.GetString() ?? string.Empty : string.Empty;

            if (string.Equals(name, "zh-CN", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "zh", StringComparison.OrdinalIgnoreCase))
            {
                result.ZhCn = value;
            }
            else if (string.Equals(name, "en-US", StringComparison.OrdinalIgnoreCase)
                     || string.Equals(name, "en", StringComparison.OrdinalIgnoreCase))
            {
                result.EnUs = value;
            }
        }

        return result;
    }

    public override void Write(Utf8JsonWriter writer, LocalizedText value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("zh-CN", value.ZhCn);
        writer.WriteString("en-US", value.EnUs);
        writer.WriteEndObject();
    }
}
