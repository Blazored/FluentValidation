# FluentValidation
A library for using FluentValidation with Blazor

![Build & Test Main](https://github.com/Blazored/FluentValidation/workflows/Build%20&%20Test%20Main/badge.svg)

![Nuget](https://img.shields.io/nuget/v/blazored.fluentvalidation.svg)

### Installing

You can install from Nuget using the following command:

`Install-Package Blazored.FluentValidation`

Or via the Visual Studio package manger.

## Basic Usage
Start by add the following using statement to your root `_Imports.razor`.

```csharp
@using Blazored.FluentValidation
```

You can then use it as follows within a `EditForm` component.

```html
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

## Finding Validators
By default, the component will check for validators registered with DI first. If it can't find, any it will then try scanning the applications assemblies to find validators using reflection.

You can control this behaviour using the `DisableAssemblyScanning` parameter. If you only wish the component to get validators from DI, set the value to `true` and assembly scanning will be skipped.

```html
<FluentValidationValidator DisableAssemblyScanning="@true" />
```

You can find examples of different configurations in the sample projects. The Blazor Server project is configured to load validators from DI only. The Blazor WebAssembly project is setup to load validators using reflection.

**Note:** When scanning assemblies the component will swallow any exceptions thrown by that process. This is to stop exceptions thrown by scanning third party dependencies crashing your app.

## Intercepting the Model Type Used to Find Validators
By default, the component will use the type of the model being validated to determine what validator to resolve from the DI container.  In most scenarios, this will be the model's compile-time type; however, if your model is being proxied (by [Castle Project's `DynamicProxy`](http://www.castleproject.org/projects/dynamicproxy/), say), the component will fail to resolve validators from the DI container or from scanning assemblies because the runtime type differs from the compile-time type used to implement the validator.

You can control this behaviour using the `ModelTypeFunc` parameter.
```csharp
// using System;
// using Castle.DynamicProxy;

public static class ModelTypeInterceptor
{
	public static Type Execute(object model)
	{
		if (model is IProxyTargetAccessor proxy)
			return proxy.DynProxyGetTarget().GetType();

		return model.GetType();
	}
}
```

```html
<FluentValidationValidator ModelTypeFunc='@ModelTypeInterceptor.Execute' />
```
