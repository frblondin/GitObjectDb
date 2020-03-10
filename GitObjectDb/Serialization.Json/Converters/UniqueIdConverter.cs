using GitObjectDb.Tools;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GitObjectDb.Serialization.Json.Converters
{
    internal class UniqueIdConverter : JsonConverter<UniqueId>
    {
        [ExcludeFromGuardForNull]
        public override UniqueId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            new UniqueId(reader.GetString());

        [ExcludeFromGuardForNull]
        public override void Write(Utf8JsonWriter writer, UniqueId value, JsonSerializerOptions options) =>
            writer.WriteStringValue(value.ToString());
    }
}
