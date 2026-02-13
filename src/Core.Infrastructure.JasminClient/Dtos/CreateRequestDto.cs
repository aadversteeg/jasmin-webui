using System.Text.Json;

namespace Core.Infrastructure.JasminClient.Dtos;

/// <summary>
/// DTO for creating an async request.
/// POST /v1/requests
/// </summary>
public record CreateRequestDto(
    string Action,
    string Target,
    JsonElement? Parameters = null);
