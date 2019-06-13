# FluentValidation
A library for using FluentValidation with Blazor

[![Build Status](https://dev.azure.com/blazored/FluentValidation/_apis/build/status/Blazored.FluentValidation?branchName=master)](https://dev.azure.com/blazored/FluentValidation/_build/latest?definitionId=11&branchName=master)

![Nuget](https://img.shields.io/nuget/v/blazored.fluentvalidation.svg)

### Installing

You can install from Nuget using the following command:

`Install-Package Blazored.FluentValidation`

Or via the Visual Studio package manger.

## Usage
Start by add the following using statement to your root `_Imports.razor`.

```csharp
@using Blazored.FluentValidation
```

You can then use it as follows within a `EditForm` component.

```
<EditForm Model="@Person" OnValidSubmit="@SubmitValidForm">
    <FluentValidationValidator />
    <ValidationSummary />

    <p>
        <label>Name: </label>
        <InputText @bind-Value="@Person.Name" />
    </p>

    <p>
        <label>Age: </label>
        <InputNumber @bind-Value="@Person.Age" />
    </p>

    <p>
        <label>Email Address: </label>
        <InputText @bind-Value="@Person.EmailAddress" />
    </p>

    <button type="submit">Save</button>

</EditForm>

@code {
    Person Person { get; set; } = new Person();

    void SubmitValidForm()
    {
        Console.WriteLine("Form Submitted Successfully!");
    }
}
```
