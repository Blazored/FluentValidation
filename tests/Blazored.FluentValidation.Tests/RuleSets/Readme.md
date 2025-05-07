### What does this test?
This test checks if using the `IncludeRuleSets..` method work, once by attribute

```html
<FluentValidationValidator Options="@(options => options.IncludeRuleSets("Names"))" />
```

and once by code

```csharp
@code {
    private FluentValidationValidator? _fluentValidationValidator;

    private void PartialValidate()
        => _fluentValidationValidator?.Validate(options => options.IncludeRuleSets("Names"));
}
```