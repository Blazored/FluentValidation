using Blazored.FluentValidation.Tests.Model;

namespace Blazored.FluentValidation.Tests.BasicValidation;

public class Tests : TestContext
{
    private readonly Fixture _fixture = new();

    [Fact]
    public void Validate_DataIsValid_ValidSubmit()
    {
        // Arrange
        var cut = RenderComponent<Component>();
        var person = _fixture.ValidPerson();

        // Act
        FillForm(cut, person);
        cut.Find("button").Click();

        // Assert
        cut.Instance.Result.Should().Be(ValidationResultType.Valid);
    }

    [Fact]
    public void Validate_FirstNameMissing_InvalidSubmit()
    {
        // Arrange
        var cut = RenderComponent<Component>();
        var person = _fixture.ValidPerson() with { FirstName = string.Empty };

        // Act
        FillForm(cut, person);
        cut.Find("button").Click();

        // Assert
        cut.Instance.Result.Should().Be(ValidationResultType.Error);
    }

    [Fact]
    public void Validate_FirstNameMissing_ValidationErrorsPresent()
    {
        // Arrange
        var cut = RenderComponent<Component>();
        var person = _fixture.ValidPerson() with { FirstName = string.Empty };

        // Act
        FillForm(cut, person);
        cut.Find("button").Click();

        // Assert
        cut.Find(".validation-errors>.validation-message").TextContent.Should().Contain(PersonValidator.FirstNameRequired);
        cut.Find("li.validation-message").TextContent.Should().Contain(PersonValidator.FirstNameRequired);
    }

    [Fact]
    public void Validate_AgeTooOld_ValidationErrorsPresent()
    {
        // Arrange
        var cut = RenderComponent<Component>();
        var person = _fixture.ValidPerson() with { Age = 250 };

        // Act
        FillForm(cut, person);
        cut.Find("button").Click();

        // Assert
        cut.Find(".validation-errors>.validation-message").TextContent.Should().Contain(PersonValidator.AgeMax);
    }

    [Fact]
    public void Validate_AddressLine1Missing_ValidationErrorsPresent()
    {
        // Arrange
        var cut = RenderComponent<Component>();
        var person = _fixture.ValidPerson() with { Address = new() { Line1 = string.Empty } };

        // Act
        FillForm(cut, person);
        cut.Find("button").Click();

        // Assert
        cut.Find(".validation-errors>.validation-message").TextContent.Should().Contain(AddressValidator.Line1Required);
    }

    private static void FillForm(IRenderedComponent<Component> cut, Person person)
    {
        cut.Find($"input[name={nameof(Person.FirstName)}]").Change(person.FirstName);
        cut.Find($"input[name={nameof(Person.LastName)}]").Change(person.LastName);
        cut.Find($"input[name={nameof(Person.EmailAddress)}]").Change(person.EmailAddress);
        cut.Find($"input[name={nameof(Person.Age)}]").Change(person.Age.ToString());
        cut.Find($"input[name={nameof(Person.Address.Line1)}]").Change(person.Address!.Line1);
    }

    private class Fixture
    {
        public Person ValidPerson() => new()
        {
            FirstName = "John",
            LastName = "Doe",
            EmailAddress = "john.doe@blazored.org",
            Age = 30,
            Address = new()
            {
                Line1 = "123 Main St",
                Town = "Springfield",
                Postcode = "12345"
            }
        };
    }
}