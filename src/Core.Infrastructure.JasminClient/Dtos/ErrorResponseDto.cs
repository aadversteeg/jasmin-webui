namespace Core.Infrastructure.JasminClient.Dtos;

/// <summary>
/// DTO for error responses from the jasmin-server API.
/// </summary>
public record ErrorResponseDto(List<ErrorDto>? Errors);

/// <summary>
/// DTO for a single error.
/// </summary>
public record ErrorDto(string Code, string Message);
