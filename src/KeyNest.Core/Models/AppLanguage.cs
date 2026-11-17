using System.Text.Json;
using System.Text.Json.Serialization;

namespace KeyNest.Core.Models;

/// <summary>界面语言。默认中文；配置缺失或非法时一律回退中文。</summary>
public enum AppLanguage
{
    Chinese = 0,
    English = 1
}

public static class AppLanguageExtensions
{
    public const string ChineseCode = "zh-CN";
    public const string EnglishCode = "en-US";

    /// <summary>配置与安装器统一使用 BCP-47 代码。</summary>
    public static string ToCode(this AppLanguage language)
        => language == AppLanguage.English ? EnglishCode : ChineseCode;

    public static AppLanguage FromCode(string? code)
    {
        if (string.Equals(code, EnglishCode, StringComparison.OrdinalIgnoreCase)
            || string.Equals(code, "en", StringComparison.OrdinalIgnoreCase)
            || string.Equals(code, nameof(AppLanguage.English), StringComparison.OrdinalIgnoreCase))
        {
            return AppLanguage.English;
        }

        return AppLanguage.Chinese;
    }
}

/// <summary>把 AppLanguage 按 zh-CN / en-US 写入配置，同时兼容旧枚举名。</summary>
public sealed class AppLanguageJsonConverter : JsonConverter<AppLanguage>
{
    public override AppLanguage Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            return AppLanguageExtensions.FromCode(reader.GetString());
        }

        if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out var number))
        {
            return Enum.IsDefined(typeof(AppLanguage), number) ? (AppLanguage)number : AppLanguage.Chinese;
        }

        return AppLanguage.Chinese;
    }

    public override void Write(Utf8JsonWriter writer, AppLanguage value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToCode());
    }
}
