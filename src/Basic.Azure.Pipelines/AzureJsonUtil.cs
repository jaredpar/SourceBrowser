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
        public static T[] GetArray<T>(string json)
        {
            using var doc = JsonDocument.Parse(json);
            doc.RootElement.TryGetProperty("value", out var value);
            return value.Deserialize<T[]>()!;
        }

        public static T GetObject<T>(string json) => JsonSerializer.Deserialize<T>(json)!;
    }
}