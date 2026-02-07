using System.Text.Json;

namespace Core.Application.McpServers;

/// <summary>
/// Utility class for parsing JSON Schema into ToolInputSchema.
/// </summary>
public static class ToolInputSchemaParser
{
    /// <summary>
    /// Parses a JSON Schema string into a ToolInputSchema.
    /// </summary>
    public static ToolInputSchema? Parse(string? jsonSchema)
    {
        if (string.IsNullOrWhiteSpace(jsonSchema))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(jsonSchema);
            return ParseSchema(doc.RootElement);
        }
        catch
        {
            return null;
        }
    }

    private static ToolInputSchema? ParseSchema(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        if (!element.TryGetProperty("properties", out var properties))
        {
            return null;
        }

        var requiredSet = new HashSet<string>();
        if (element.TryGetProperty("required", out var required) && required.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in required.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String)
                {
                    requiredSet.Add(item.GetString()!);
                }
            }
        }

        var parameters = new List<ToolInputParameter>();

        foreach (var prop in properties.EnumerateObject())
        {
            var param = ParseParameter(prop.Name, prop.Value, requiredSet.Contains(prop.Name));
            if (param != null)
            {
                parameters.Add(param);
            }
        }

        // Sort: required parameters first, then alphabetically
        parameters = parameters
            .OrderByDescending(p => p.Required)
            .ThenBy(p => p.Name)
            .ToList();

        return new ToolInputSchema(parameters);
    }

    private static ToolInputParameter? ParseParameter(string name, JsonElement element, bool isRequired)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var type = GetStringProperty(element, "type") ?? "string";
        var description = GetStringProperty(element, "description");
        var defaultValue = GetDefaultValue(element);
        var enumValues = GetEnumValues(element);

        ToolInputSchema? nestedSchema = null;
        string? itemsType = null;

        // Handle array types
        if (type == "array" && element.TryGetProperty("items", out var items))
        {
            if (items.TryGetProperty("type", out var itemTypeProp))
            {
                itemsType = itemTypeProp.GetString();

                // Parse nested schema for arrays of objects
                if (itemsType == "object")
                {
                    nestedSchema = ParseSchema(items);
                }
            }
        }

        // Handle object types with properties
        if (type == "object" && element.TryGetProperty("properties", out _))
        {
            nestedSchema = ParseSchema(element);
        }

        // Handle object types with additionalProperties (dynamic key-value pairs)
        string? additionalPropertiesType = null;
        if (type == "object" && element.TryGetProperty("additionalProperties", out var additionalProps))
        {
            if (additionalProps.ValueKind == JsonValueKind.True)
            {
                // additionalProperties: true means any value type is allowed
                additionalPropertiesType = "any";
            }
            else if (additionalProps.ValueKind == JsonValueKind.Object)
            {
                // additionalProperties: { type: "string" } specifies the value type
                additionalPropertiesType = GetStringProperty(additionalProps, "type") ?? "string";
            }
        }

        return new ToolInputParameter(
            name,
            type,
            description,
            isRequired,
            enumValues,
            defaultValue,
            nestedSchema,
            itemsType,
            additionalPropertiesType);
    }

    private static string? GetStringProperty(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String)
        {
            return prop.GetString();
        }
        return null;
    }

    private static object? GetDefaultValue(JsonElement element)
    {
        if (!element.TryGetProperty("default", out var defaultProp))
        {
            return null;
        }

        return defaultProp.ValueKind switch
        {
            JsonValueKind.String => defaultProp.GetString(),
            JsonValueKind.Number => defaultProp.TryGetInt64(out var l) ? l : defaultProp.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => null
        };
    }

    private static IReadOnlyList<string>? GetEnumValues(JsonElement element)
    {
        if (!element.TryGetProperty("enum", out var enumProp) || enumProp.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var values = new List<string>();
        foreach (var item in enumProp.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String)
            {
                values.Add(item.GetString()!);
            }
        }

        return values.Count > 0 ? values : null;
    }
}
