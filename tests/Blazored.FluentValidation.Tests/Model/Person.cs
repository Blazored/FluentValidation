namespace Blazored.FluentValidation.Tests.Model
{
    public record Person
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public int? Age { get; set; }
        public string? EmailAddress { get; set; }
        public Address? Address { get; set; }
    }

    public class PersonValidator : AbstractValidator<Person>
    {
        public const string FirstNameRequired = "You must enter your first name";
        public const string FirstNameMaxLength = "First name cannot be longer than 50 characters";
        public const string LastNameRequired = "You must enter your last name";
        public const string LastNameMaxLength = "Last name cannot be longer than 50 characters";
        public const string AgeRequired = "You must enter your age";
        public const string AgeMin = "Age must be greater than 0";
        public const string AgeMax = "Age cannot be greater than 150";
        public const string EmailRequired = "You must enter an email address";
        public const string EmailValid = "You must provide a valid email address";
        public const string EmailUnique = "Email address must be unique";
        public const string DuplicateEmail = "mail@my.com";
        
        public PersonValidator()
        {
            RuleSet("Names", () =>
            {
                RuleFor(p => p.FirstName)
                .NotEmpty().WithMessage(FirstNameRequired)
                .MaximumLength(50).WithMessage(FirstNameMaxLength);

                RuleFor(p => p.LastName)
                .NotEmpty().WithMessage(LastNameRequired)
                .MaximumLength(50).WithMessage(LastNameMaxLength);
            });

            RuleFor(p => p.Age)
                .NotNull().WithMessage(AgeRequired)
                .GreaterThanOrEqualTo(0).WithMessage(AgeMin)
                .LessThan(150).WithMessage(AgeMax);

            RuleFor(p => p.EmailAddress)
                .NotEmpty().WithMessage(EmailRequired)
                .EmailAddress().WithMessage(EmailValid)
                .MustAsync(async (email, _) => await IsUniqueAsync(email)).WithMessage(EmailUnique);

            RuleFor(p => p.Address!)
                .SetValidator(new AddressValidator())
                .When(p => p.Address is not null);
        }

        private static async Task<bool> IsUniqueAsync(string? email)
        {
            await Task.Delay(300);
            return email?.ToLower() != DuplicateEmail;
        }
    }
}
