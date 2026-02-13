using Core.Infrastructure.JasminClient;
using FluentAssertions;
using Xunit;

namespace Tests.Infrastructure.JasminClient;

public class TargetHelperTests
{
    [Fact(DisplayName = "TH-001: BuildServerTarget should return correct format")]
    public void TH001()
    {
        // Act
        var result = TargetHelper.BuildServerTarget("my-server");

        // Assert
        result.Should().Be("mcp-servers/my-server");
    }

    [Fact(DisplayName = "TH-002: BuildInstanceTarget should return correct format")]
    public void TH002()
    {
        // Act
        var result = TargetHelper.BuildInstanceTarget("my-server", "abc-123");

        // Assert
        result.Should().Be("mcp-servers/my-server/instances/abc-123");
    }

    [Fact(DisplayName = "TH-003: ParseTarget should extract serverName from server target")]
    public void TH003()
    {
        // Act
        var (serverName, instanceId) = TargetHelper.ParseTarget("mcp-servers/my-server");

        // Assert
        serverName.Should().Be("my-server");
        instanceId.Should().BeNull();
    }

    [Fact(DisplayName = "TH-004: ParseTarget should extract serverName and instanceId from instance target")]
    public void TH004()
    {
        // Act
        var (serverName, instanceId) = TargetHelper.ParseTarget("mcp-servers/my-server/instances/abc-123");

        // Assert
        serverName.Should().Be("my-server");
        instanceId.Should().Be("abc-123");
    }

    [Fact(DisplayName = "TH-005: ParseTarget with empty string should return nulls")]
    public void TH005()
    {
        // Act
        var (serverName, instanceId) = TargetHelper.ParseTarget("");

        // Assert
        serverName.Should().BeNull();
        instanceId.Should().BeNull();
    }
}
