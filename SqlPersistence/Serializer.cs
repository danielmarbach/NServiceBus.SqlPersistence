﻿using System.IO;
using System.Text;
using Newtonsoft.Json;

static class Serializer
{
    public static JsonSerializer JsonSerializer;
    static JsonSerializerSettings settings;

    static Serializer()
    {
        settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            DefaultValueHandling = DefaultValueHandling.Ignore
        };
        JsonSerializer = JsonSerializer.Create(settings);
    }

    public static T Deserialize<T>(TextReader textReader)
    {
        using (var jsonReader = new JsonTextReader(textReader))
        {
            return JsonSerializer.Deserialize<T>(jsonReader);
        }
    }

    public static string Serialize(object target)
    {
        var stringBuilder = new StringBuilder();
        var stringWriter = new StringWriter(stringBuilder);
        using (var jsonWriter = new JsonTextWriter(stringWriter))
        {
            JsonSerializer.Serialize(jsonWriter, target);
        }
        return stringBuilder.ToString();
    }
}