namespace Blazored.FluentValidation.Tests.Model
{
    public record Address
    {
        public string? Line1 { get; set; }
        public string? Line2 { get; set; }
        public string? Town { get; set; }
        public string? County { get; set; }
        public string? Postcode { get; set; }
    }

    public class AddressValidator : AbstractValidator<Address>
    {
        public const string Line1Required = "You must enter Line 1";
        public const string TownRequired = "You must enter a town";
        public const string CountyRequired = "You must enter a county";
        public const string PostcodeRequired = "You must enter a postcode";

        public AddressValidator()
        {
            RuleFor(p => p.Line1).NotEmpty().WithMessage(Line1Required);
            RuleFor(p => p.Town).NotEmpty().WithMessage(TownRequired);
            RuleFor(p => p.County).NotEmpty().WithMessage(CountyRequired);
            RuleFor(p => p.Postcode).NotEmpty().WithMessage(PostcodeRequired);
        }
    }
}
