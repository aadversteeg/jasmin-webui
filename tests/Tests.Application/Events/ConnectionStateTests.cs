using Core.Application.Events;
using FluentAssertions;
using Xunit;

namespace Tests.Application.Events;

public class ConnectionStateTests
{
    [Fact(DisplayName = "CON-001: ConnectionState should have 5 values")]
    public void CON001()
    {
        // Arrange & Act
        var values = Enum.GetValues<ConnectionState>();

        // Assert
        values.Should().HaveCount(5);
    }

    [Fact(DisplayName = "CON-002: ConnectionState.Disconnected should be the default")]
    public void CON002()
    {
        // Arrange & Act
        var defaultState = default(ConnectionState);

        // Assert
        defaultState.Should().Be(ConnectionState.Disconnected);
    }
}
