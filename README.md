# FluentValidation
A library for using FluentValidation with Blazor

[![Nuget version](https://img.shields.io/nuget/v/Blazored.FluentValidation.svg?logo=nuget)](https://www.nuget.org/packages/Blazored.FluentValidation/)
[![Nuget downloads](https://img.shields.io/nuget/dt/Blazored.FluentValidation?logo=nuget)](https://www.nuget.org/packages/Blazored.FluentValidation/)
![Build & Test Main](https://github.com/Blazored/FluentValidation/workflows/Build%20&%20Test%20Main/badge.svg)

## Installing

You can install from Nuget using the following command:

`Install-Package Blazored.FluentValidation`

Or via the Visual Studio package manger.

## Basic Usage
Start by add the following using statement to your root `_Imports.razor`.

```razor
@using Blazored.FluentValidation
```

You can then use it as follows within a `EditForm` component.

```razor
<EditForm Model="@_person" OnValidSubmit="@SubmitValidForm">
    <FluentValidationValidator />
    <ValidationSummary />

    <p>
        <label>Name: </label>
        <InputText @bind-Value="@_person.Name" />
    </p>

    <p>
        <label>Age: </label>
        <InputNumber @bind-Value="@_person.Age" />
    </p>

    <p>
        <label>Email Address: </label>
        <InputText @bind-Value="@_person.EmailAddress" />
    </p>

    <button type="submit">Save</button>
</EditForm>

@code {
    private Person _person = new();

    private void SubmitValidForm()
        => Console.WriteLine("Form Submitted Successfully!");
}
```

## Finding Validators
By default, the component will check for validators registered with DI first. If it can't find, any it will then try scanning the applications assemblies to find validators using reflection.

You can control this behaviour using the `DisableAssemblyScanning` parameter. If you only wish the component to get validators from DI, set the value to `true` and assembly scanning will be skipped.

```html
<FluentValidationValidator DisableAssemblyScanning="@true" />
```

You can find examples of different configurations in the sample projects. The Blazor Server project is configured to load validators from DI only. The Blazor WebAssembly project is setup to load validators using reflection.

**Note:** When scanning assemblies the component will swallow any exceptions thrown by that process. This is to stop exceptions thrown by scanning third party dependencies crashing your app.

The validator must be publicly accessible and inherit directly from `AbstractValidator<T>`.

## Async Validation
If you're using async validation, you can use the `ValidateAsync` method on the `FluentValidationValidator`.

```razor
<EditForm Model="@_person" OnSubmit="@SubmitFormAsync">
    <FluentValidationValidator @ref="_fluentValidationValidator" />
    <ValidationSummary />

    <p>
        <label>Name: </label>
        <InputText @bind-Value="@_person.Name" />
    </p>

    <p>
        <label>Age: </label>
        <InputNumber @bind-Value="@_person.Age" />
    </p>

    <p>
        <label>Email Address: </label>
        <InputText @bind-Value="@_person.EmailAddress" />
    </p>

    <button type="submit">Save</button>

</EditForm>

@code {
    private Person _person = new();
    private FluentValidationValidator? _fluentValidationValidator;

    private async void SubmitFormAsync()
    {
		if (await _fluentValidationValidator!.ValidateAsync())
        {
            Console.WriteLine("Form Submitted Successfully!");
        }
    }
}
```

## RuleSets
[RuleSets](https://docs.fluentvalidation.net/en/latest/rulesets.html) allow validation rules to be grouped and executed together while ignoring other rules. RulesSets are supported in two ways.

The first is setting RuleSets via the `Options` parameter on the `FluentValidationValidator` component.

```razor
<FluentValidationValidator Options="@(options => options.IncludeRuleSets("Names"))" />
```

The second is when manually validating the model using the `Validate` or `ValidateAsync` methods. 

```razor
<FluentValidationValidator @ref="_fluentValidationValidator" />

@code {
    private FluentValidationValidator? _fluentValidationValidator;

    private void PartialValidate()
        => _fluentValidationValidator?.Validate(options => options.IncludeRuleSets("Names"));
}
```

## Access to full `ValidationFailure`
If you need details about the specifics of a validation result (e.g. its `Severity`), you can access the result of the 
last validation by calling the `GetFailuresFromLastValidation` method on the `FluentValidationValidator` component.

```razor
<div class="validation-message @GetValidationClass()">
    <input type="text" @bind="@_person.Name" />
</div>

@code {
    private FluentValidationValidator? _fluentValidationValidator;

    private string GetValidationClass() 
    {
        var lastResult = _fluentValidationValidator?.GetFailuresFromLastValidation();
        if (lastResult is null || !lastResult.Any())
        {
            return "valid";
        }
        if (lastResult.Any(failure => failure.Severity == Severity.Error))
        {
            return "invalid";
        }
        return "warning";
    }
}
```