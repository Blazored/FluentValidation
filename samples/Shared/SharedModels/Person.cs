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
            RuleSet("Names", () =>
            {
                RuleFor(p => p.FirstName)
                .NotEmpty().WithMessage("You must enter your first name")
                .MaximumLength(50).WithMessage("First name cannot be longer than 50 characters");

                RuleFor(p => p.LastName)
                .NotEmpty().WithMessage("You must enter your last name")
                .MaximumLength(50).WithMessage("Last name cannot be longer than 50 characters");
            });

            RuleFor(p => p.Age)
                .NotNull().WithMessage("You must enter your age")
                .GreaterThanOrEqualTo(0).WithMessage("Age must be greater than 0")
                .LessThan(150).WithMessage("Age cannot be greater than 150");

            RuleFor(p => p.EmailAddress)
                .NotEmpty().WithMessage("You must enter a email address")
                .EmailAddress().WithMessage("You must provide a valid email address")
                .MustAsync(async (email, cancellationToken) => await IsUniqueAsync(email)).WithMessage("Email address must be unique").When(p => !string.IsNullOrEmpty(p.EmailAddress));

            RuleFor(p => p.Address).SetValidator(new AddressValidator());
        }

        private async Task<bool> IsUniqueAsync(string email)
        {
            await Task.Delay(300);
            return email.ToLower() != "mail@my.com";
        }
    }
}
