using Blazored.FluentValidation.Tests.Model;

namespace Blazored.FluentValidation.Tests.RuleSets;

public class Tests : TestContext
{
    private readonly Fixture _fixture = new();

    [Fact]
    public void AddedByAttribute_PersonValid_ValidationPasses()
    {
        // Arrange
        var person = _fixture.ValidPerson();
        var cut = RenderComponent<Component>(p => p.Add(c => c.IncludeWithCode, true));

        // Act
        FillForm(cut, person);
        cut.Find("button").Click();

        // Assert
        cut.Instance.Result.Should().Be(ValidationResultType.Valid);
    }

    [Fact]
    public void AddedByAttribute_PersonFirstNameTooLong_ValidationFails()
    {
        // Arrange
        var person = _fixture.ValidPerson() with
        {
            FirstName = "This name is clearly longer than 50 characters and thus should fail."
        };
        var cut = RenderComponent<Component>(p => p.Add(c => c.IncludeWithCode, true));

        // Act
        FillForm(cut, person);
        cut.Find("button").Click();

        // Assert
        cut.Instance.Result.Should().Be(ValidationResultType.Error);
    }

    [Fact]
    public void AddedByAttribute_PersonFirstNameTooLong_ValidationMessagesPresent()
    {
        // Arrange
        var person = _fixture.ValidPerson() with
        {
            FirstName = "This name is clearly longer than 50 characters and thus should fail."
        };
        var cut = RenderComponent<Component>(p => p.Add(c => c.IncludeWithCode, true));

        // Act
        FillForm(cut, person);
        cut.Find("button").Click();

        // Assert
        cut.Find(".validation-errors>.validation-message").TextContent.Should().Contain(PersonValidator.FirstNameMaxLength);
        cut.Find("li.validation-message").TextContent.Should().Contain(PersonValidator.FirstNameMaxLength);
    }

    private static void FillForm(IRenderedComponent<Component> cut, Person person)
    {
        cut.Find($"input[name={nameof(Person.FirstName)}]").Change(person.FirstName);
        cut.Find($"input[name={nameof(Person.LastName)}]").Change(person.LastName);
        cut.Find($"input[name={nameof(Person.EmailAddress)}]").Change(person.EmailAddress);
        cut.Find($"input[name={nameof(Person.Age)}]").Change(person.Age.ToString());
    }

    private class Fixture
    {
        public Person ValidPerson() => new()
        {
            FirstName = "John",
            LastName = "Doe",
            EmailAddress = "john.doe@blazored.com",
            Age = 30
        };
    }
}