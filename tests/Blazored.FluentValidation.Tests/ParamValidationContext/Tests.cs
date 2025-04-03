using Blazored.FluentValidation.Tests.Model;
using BlazoredFluentValidation.Tests.ParamValidationContext;
using static Blazored.FluentValidation.Tests.Model.AddressValidator;

namespace Blazored.FluentValidation.Tests.ParamValidationContext;

public class Tests : TestContext
{
    private readonly Fixture _fixture = new();

    [Fact]
    public void Validate_PostcodeEmptyAndIgnorePostcodeContextNotSet_InvalidSubmit()
    {
        // Arrange
        var cut = RenderComponent<Component>();
        var address = _fixture.ValidAddress();
        address.Postcode = "";
        // Act
        FillForm(cut, address);
        cut.Find("button").Click();

        // Assert
        cut.Instance.SubmitResult.Should().Be(ValidationResultType.Error);
    }


    [Fact]
    public void Validate_PostcodeEmptyAndIgnorePostcodeContextFalse_InvalidSubmit()
    {
        // Arrange
        var cut = RenderComponent<Component>();
        var address = _fixture.ValidAddress();
        address.Postcode = "";
        cut.Instance.ContextData.Add(IgnorePostcodeFlag, false);

        // Act
        FillForm(cut, address);
        cut.Find("button").Click();

        // Assert
        cut.Instance.SubmitResult.Should().Be(ValidationResultType.Error);
    }

    [Fact]
    public void Validate_PostcodeEmptyAndIgnorePostcodeContextTrue_ValidSubmit()
    {
        // Arrange
        var cut = RenderComponent<Component>();
        var address = _fixture.ValidAddress();
        address.Postcode = "";
        cut.Instance.ContextData.Add(IgnorePostcodeFlag, true);

        // Act
        FillForm(cut, address);
        cut.Find("button").Click();
        cut.WaitForState(() => cut.Instance.SubmitResult == ValidationResultType.Valid);
        // Assert
        cut.Instance.SubmitResult.Should().Be(ValidationResultType.Valid);
    }

    private static void FillForm(IRenderedComponent<Component> cut, Address address)
    {
        cut.Find($"input[name={nameof(Address.Line1)}]").Change(address.Line1);
        cut.Find($"input[name={nameof(Address.Town)}]").Change(address.Town);
        cut.Find($"input[name={nameof(Address.County)}]").Change(address.County);
        cut.Find($"input[name={nameof(Address.Postcode)}]").Change(address.Postcode);
    }

    private class Fixture
    {
        public Address ValidAddress() => new()
        {
            Line1 = "123 Main St",
            Town = "Springfield",
            Postcode = "12345",
            County = "Real"
        };
    }
}
