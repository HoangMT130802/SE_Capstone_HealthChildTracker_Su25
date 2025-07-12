using System.Text.Json;
using System.Text.Json.Serialization;

namespace KidTracking.API
{
    /// <summary>
    /// Custom JSON converter for TimeOnly type to handle serialization/deserialization
    /// </summary>
    public class TimeOnlyJsonConverter : JsonConverter<TimeOnly>
    {
        private const string TimeFormat = "HH:mm";

        public override TimeOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var value = reader.GetString();
            if (string.IsNullOrEmpty(value))
            {
                return default;
            }

            if (TimeOnly.TryParseExact(value, TimeFormat, out var result))
            {
                return result;
            }

            throw new JsonException($"Unable to convert \"{value}\" to TimeOnly. Expected format: {TimeFormat}");
        }

        public override void Write(Utf8JsonWriter writer, TimeOnly value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString(TimeFormat));
        }
    }

    /// <summary>
    /// Custom JSON converter for nullable TimeOnly type
    /// </summary>
    public class TimeOnlyNullableJsonConverter : JsonConverter<TimeOnly?>
    {
        private const string TimeFormat = "HH:mm";

        public override TimeOnly? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var value = reader.GetString();
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }

            if (TimeOnly.TryParseExact(value, TimeFormat, out var result))
            {
                return result;
            }

            throw new JsonException($"Unable to convert \"{value}\" to TimeOnly. Expected format: {TimeFormat}");
        }

        public override void Write(Utf8JsonWriter writer, TimeOnly? value, JsonSerializerOptions options)
        {
            if (value.HasValue)
            {
                writer.WriteStringValue(value.Value.ToString(TimeFormat));
            }
            else
            {
                writer.WriteNullValue();
            }
        }
    }
} 