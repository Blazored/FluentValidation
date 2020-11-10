using FluentValidation;
using System.Threading.Tasks;

namespace SharedModels
{
    public class Person
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int? Age { get; set; }
        public string EmailAddress { get; set; }
        public Address Address { get; set; } = new Address();
    }

    public class PersonValidator : AbstractValidator<Person>
    {
        public PersonValidator()
        {
            RuleSet("Names", () => {
                RuleFor(p => p.FirstName).NotEmpty().WithMessage("You must enter your first name");
                RuleFor(p => p.FirstName).MaximumLength(50).WithMessage("First name cannot be longer than 50 characters");
                RuleFor(p => p.LastName).NotEmpty().WithMessage("You must enter your last name");
                RuleFor(p => p.LastName).MaximumLength(50).WithMessage("Last name cannot be longer than 50 characters");
            });
           
            RuleFor(p => p.Age).NotNull().GreaterThanOrEqualTo(0).WithMessage("Age must be greater than 0");
            RuleFor(p => p.Age).LessThan(150).WithMessage("Age cannot be greater than 150");
            RuleFor(p => p.EmailAddress).NotEmpty().WithMessage("You must enter a email address");
            RuleFor(p => p.EmailAddress).EmailAddress().WithMessage("You must provide a valid email address");

            RuleFor(x => x.EmailAddress).MustAsync(async (name, cancellationToken) => await IsUniqueAsync(name))
                                .WithMessage("Email address must be unique")
                                .When(person => !string.IsNullOrEmpty(person.EmailAddress));

            RuleFor(p => p.Address).SetValidator(new AddressValidator());
        }

        private async Task<bool> IsUniqueAsync(string name)
        {
            await Task.Delay(300);
            return name.ToLower() != "test";
        }
    }
}
