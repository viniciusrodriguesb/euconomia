using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Api.Serializacao;

internal sealed class DecimalJsonConverter : JsonConverter<decimal>
{
    public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number && reader.TryGetDecimal(out var numero))
            return numero;
        if (reader.TokenType == JsonTokenType.String && decimal.TryParse(reader.GetString(),
            NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var valor))
            return valor;
        throw new JsonException("Informe um número decimal válido.");
    }

    public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToString(CultureInfo.InvariantCulture));
}
