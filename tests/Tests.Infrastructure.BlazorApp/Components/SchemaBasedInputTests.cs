using Bunit;
using Core.Application.McpServers;
using Core.Infrastructure.BlazorApp.Components;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Xunit;

namespace Tests.Infrastructure.BlazorApp.Components;

public class SchemaBasedInputTests : TestContext
{
    [Fact(DisplayName = "SBI-001: Render string input for string parameter")]
    public void SBI001()
    {
        // Arrange
        var parameter = new ToolInputParameter(
            "name", "string", "Enter your name", true, null, null, null, null);

        // Act
        var cut = RenderComponent<SchemaBasedInput>(p => p
            .Add(x => x.Parameter, parameter)
            .Add(x => x.Value, null));

        // Assert
        cut.Find("input[type='text']").Should().NotBeNull();
        cut.Find("code").TextContent.Should().Be("name");
    }

    [Fact(DisplayName = "SBI-002: Render number input for number parameter")]
    public void SBI002()
    {
        // Arrange
        var parameter = new ToolInputParameter(
            "count", "number", "Enter count", false, null, null, null, null);

        // Act
        var cut = RenderComponent<SchemaBasedInput>(p => p
            .Add(x => x.Parameter, parameter)
            .Add(x => x.Value, null));

        // Assert
        cut.Find("input[type='number']").Should().NotBeNull();
    }

    [Fact(DisplayName = "SBI-003: Render checkbox for boolean parameter")]
    public void SBI003()
    {
        // Arrange
        var parameter = new ToolInputParameter(
            "enabled", "boolean", "Enable feature", false, null, null, null, null);

        // Act
        var cut = RenderComponent<SchemaBasedInput>(p => p
            .Add(x => x.Parameter, parameter)
            .Add(x => x.Value, true));

        // Assert
        var checkbox = cut.Find("input[type='checkbox']");
        checkbox.Should().NotBeNull();
        checkbox.HasAttribute("checked").Should().BeTrue();
    }

    [Fact(DisplayName = "SBI-004: Render select for enum parameter")]
    public void SBI004()
    {
        // Arrange
        var parameter = new ToolInputParameter(
            "status", "string", "Select status", true,
            new List<string> { "active", "inactive", "pending" }, null, null, null);

        // Act
        var cut = RenderComponent<SchemaBasedInput>(p => p
            .Add(x => x.Parameter, parameter)
            .Add(x => x.Value, "active"));

        // Assert
        var select = cut.Find("select");
        select.Should().NotBeNull();
        var options = cut.FindAll("option");
        options.Should().HaveCount(4); // Including "Select a value..."
    }

    [Fact(DisplayName = "SBI-005: Render array with add button when empty")]
    public void SBI005()
    {
        // Arrange
        var parameter = new ToolInputParameter(
            "tags", "array", "List of tags", false, null, null, null, "string");

        // Act
        var cut = RenderComponent<SchemaBasedInput>(p => p
            .Add(x => x.Parameter, parameter)
            .Add(x => x.Value, new List<object?>()));

        // Assert
        cut.Find("button").TextContent.Should().Contain("Add Item");
        cut.FindAll(".array-item").Should().BeEmpty();
    }

    [Fact(DisplayName = "SBI-006: Render array items with remove buttons")]
    public void SBI006()
    {
        // Arrange
        var parameter = new ToolInputParameter(
            "tags", "array", "List of tags", false, null, null, null, "string");
        var items = new List<object?> { "tag1", "tag2", "tag3" };

        // Act
        var cut = RenderComponent<SchemaBasedInput>(p => p
            .Add(x => x.Parameter, parameter)
            .Add(x => x.Value, items));

        // Assert
        var arrayItems = cut.FindAll(".array-item");
        arrayItems.Should().HaveCount(3);

        // Each item should have a remove button
        var removeButtons = cut.FindAll(".array-item button");
        removeButtons.Should().HaveCount(3);
    }

    [Fact(DisplayName = "SBI-007: Add item to array calls OnValueChanged")]
    public void SBI007()
    {
        // Arrange
        var parameter = new ToolInputParameter(
            "tags", "array", "List of tags", false, null, null, null, "string");
        object? changedValue = null;

        var cut = RenderComponent<SchemaBasedInput>(p => p
            .Add(x => x.Parameter, parameter)
            .Add(x => x.Value, new List<object?>())
            .Add(x => x.OnValueChanged, EventCallback.Factory.Create<object?>(this, v => changedValue = v)));

        // Act
        cut.Find("button").Click();

        // Assert
        changedValue.Should().NotBeNull();
        changedValue.Should().BeOfType<List<object?>>();
        ((List<object?>)changedValue!).Should().HaveCount(1);
    }

    [Fact(DisplayName = "SBI-008: Remove item from array calls OnValueChanged")]
    public void SBI008()
    {
        // Arrange
        var parameter = new ToolInputParameter(
            "tags", "array", "List of tags", false, null, null, null, "string");
        var items = new List<object?> { "tag1", "tag2" };
        object? changedValue = null;

        var cut = RenderComponent<SchemaBasedInput>(p => p
            .Add(x => x.Parameter, parameter)
            .Add(x => x.Value, items)
            .Add(x => x.OnValueChanged, EventCallback.Factory.Create<object?>(this, v => changedValue = v)));

        // Act - click first remove button
        cut.FindAll(".array-item button")[0].Click();

        // Assert
        changedValue.Should().NotBeNull();
        changedValue.Should().BeOfType<List<object?>>();
        ((List<object?>)changedValue!).Should().HaveCount(1);
        ((List<object?>)changedValue!)[0].Should().Be("tag2");
    }

    [Fact(DisplayName = "SBI-009: Render nested object properties")]
    public void SBI009()
    {
        // Arrange
        var nestedSchema = new ToolInputSchema(new List<ToolInputParameter>
        {
            new("host", "string", "Hostname", true, null, null, null, null),
            new("port", "number", "Port number", false, null, null, null, null)
        });
        var parameter = new ToolInputParameter(
            "config", "object", "Configuration", false, null, null, nestedSchema, null);

        // Act
        var cut = RenderComponent<SchemaBasedInput>(p => p
            .Add(x => x.Parameter, parameter)
            .Add(x => x.Value, new Dictionary<string, object?>()));

        // Assert
        var inputs = cut.FindAll("input");
        inputs.Should().HaveCount(2); // host (text) and port (number)
    }

    [Fact(DisplayName = "SBI-010: Render deeply nested structure")]
    public void SBI010()
    {
        // Arrange - array of objects with nested properties
        var itemSchema = new ToolInputSchema(new List<ToolInputParameter>
        {
            new("name", "string", "Name", true, null, null, null, null),
            new("value", "number", "Value", false, null, null, null, null)
        });
        var parameter = new ToolInputParameter(
            "items", "array", "List of items", false, null, null, itemSchema, "object");

        var items = new List<object?>
        {
            new Dictionary<string, object?> { ["name"] = "item1", ["value"] = 10 }
        };

        // Act
        var cut = RenderComponent<SchemaBasedInput>(p => p
            .Add(x => x.Parameter, parameter)
            .Add(x => x.Value, items));

        // Assert
        cut.FindAll(".array-item").Should().HaveCount(1);
        // Should have nested inputs for name and value
        cut.FindAll(".array-item input").Should().HaveCount(2);
    }

    [Fact(DisplayName = "SBI-011: Show description as title attribute")]
    public void SBI011()
    {
        // Arrange
        var parameter = new ToolInputParameter(
            "name", "string", "This is the description", true, null, null, null, null);

        // Act
        var cut = RenderComponent<SchemaBasedInput>(p => p
            .Add(x => x.Parameter, parameter)
            .Add(x => x.Value, null));

        // Assert
        var label = cut.Find("label");
        label.GetAttribute("title").Should().Be("This is the description");
    }

    [Fact(DisplayName = "SBI-012: Show required indicator for required parameters")]
    public void SBI012()
    {
        // Arrange
        var parameter = new ToolInputParameter(
            "name", "string", null, true, null, null, null, null);

        // Act
        var cut = RenderComponent<SchemaBasedInput>(p => p
            .Add(x => x.Parameter, parameter)
            .Add(x => x.Value, null));

        // Assert
        cut.Find(".text-danger").TextContent.Should().Be("*");
    }
}
