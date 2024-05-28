using Blazored.FluentValidation.Tests.Model;

namespace Blazored.FluentValidation.Tests.DirectValidation;

public class SyncTests : TestContext
{
    private readonly Fixture _fixture = new();

    [Fact]
    public void Validate_PersonIsValid_ResultIsValid()
    {
        // Arrange
        var cut = RenderComponent<SyncComponent>();
        var person = _fixture.ValidPerson();

        // Act
        FillForm(cut, person);
        cut.Find("button").Click();

        // Assert
        cut.Instance.Result.Should().Be(ValidationResultType.Valid);
    }

    [Fact]
    public void Validate_AgeNegative_ResultIsError()
    {
        // Arrange
        var cut = RenderComponent<SyncComponent>();
        var person = _fixture.ValidPerson() with { Age = -5 };

        // Act
        FillForm(cut, person);
        cut.Find("button").Click();

        // Assert
        cut.Instance.Result.Should().Be(ValidationResultType.Error);
    }

    [Fact]
    public void Validate_AgeNegative_ValidationMessagesPresent()
    {
        // Arrange
        var cut = RenderComponent<SyncComponent>();
        var person = _fixture.ValidPerson() with { Age = -5 };

        // Act
        FillForm(cut, person);
        cut.Find("button").Click();

        // Assert
        cut.Find(".validation-errors>.validation-message").TextContent.Should().Contain(PersonValidator.AgeMin);
        cut.Find("li.validation-message").TextContent.Should().Contain(PersonValidator.AgeMin);
    }

    private void FillForm(IRenderedComponent<SyncComponent> cut, Person person)
    {
        cut.Find("input[name=FirstName]").Change(person.FirstName);
        cut.Find("input[name=LastName]").Change(person.LastName);
        cut.Find("input[name=Age]").Change(person.Age.ToString());
        cut.Find("input[name=EmailAddress]").Change(person.EmailAddress);
    }

    private class Fixture
    {
        public Person ValidPerson() => new()
        {
            FirstName = "John",
            LastName = "Doe",
            Age = 30,
            EmailAddress = "john.doe@blazored.com"
        };
    }
}