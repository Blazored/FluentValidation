using Blazored.FluentValidation.Tests.Model;

namespace Blazored.FluentValidation.Tests.AsyncValidation;

public class Tests : TestContext
{
    private readonly Fixture _fixture = new();
    
    [Fact]
    public void AsyncValidate_PersonIsValid_ResultIsValid()
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
    public void AsyncValidate_FirstNameTooLong_ResultIsError()
    {
        // Arrange
        var cut = RenderComponent<Component>();
        var person = _fixture.ValidPerson() with
        {
            FirstName = "This is a very long first name that is over 50 characters long"
        };

        // Act
        FillForm(cut, person);
        cut.Find("button").Click();

        // Assert
        cut.Instance.Result.Should().Be(ValidationResultType.Error);
    }
    
    [Fact]
    public void AsyncValidate_FirstNameTooLong_ValidationMessagesPresent()
    {
        // Arrange
        var cut = RenderComponent<Component>();
        var person = _fixture.ValidPerson() with
        {
            FirstName = "This is a very long first name that is over 50 characters long",
            LastName = "",
        };

        // Act
        FillForm(cut, person);
        cut.Find("button").Click();

        // Assert
        cut.Find(".validation-errors>.validation-message").TextContent.Should().Contain(PersonValidator.FirstNameMaxLength);
        cut.Find("li.validation-message").TextContent.Should().Contain(PersonValidator.FirstNameMaxLength);
    }

    private void FillForm(IRenderedComponent<Component> cut, Person person)
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