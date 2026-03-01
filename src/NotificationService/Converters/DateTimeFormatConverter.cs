using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NotificationService.Converters
{
    public class DateTimeFormatConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return DateTime.TryParse(reader.GetString(), out var date) ? date : DateTime.MinValue;
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToLocalTime().ToString("HH:mm - d/M/yyyy"));
        }
    }
}
