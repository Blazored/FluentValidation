using FluentValidation;
using FluentValidation.Internal;

namespace Blazored.FluentValidation;

internal class IntersectingCompositeValidatorSelector : IValidatorSelector {
    private readonly IEnumerable<IValidatorSelector> _selectors;

    public IntersectingCompositeValidatorSelector(IEnumerable<IValidatorSelector> selectors) {
        _selectors = selectors;
    }

    public bool CanExecute(IValidationRule rule, string propertyPath, IValidationContext context) {
        return _selectors.All(s => s.CanExecute(rule, propertyPath, context));
    }
}