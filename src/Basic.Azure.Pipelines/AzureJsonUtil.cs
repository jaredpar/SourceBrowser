using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Basic.Azure.Pipelines
{
    public static class AzureJsonUtil
    {
        public static readonly JsonSerializerOptions Options = new JsonSerializerOptions()
        {
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true,
            Converters = 
            {
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
            }
        };

        public static T[] GetArray<T>(string json)
        {
            using var doc = JsonDocument.Parse(json);
            doc.RootElement.TryGetProperty("value", out var value);
            var list = new List<T>();
            foreach (var elem in value.EnumerateArray())
            {
                T e = JsonSerializer.Deserialize<T>(elem, Options)!;
                list.Add(e);
            }

            return list.ToArray();
        }

        public static T GetObject<T>(string json) => JsonSerializer.Deserialize<T>(json)!;
    }
}