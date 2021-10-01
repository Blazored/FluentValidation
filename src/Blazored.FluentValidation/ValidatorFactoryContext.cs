using Microsoft.AspNetCore.Components.Forms;
using System;

namespace Blazored.FluentValidation
{
    public class ValidatorFactoryContext
    {
        public ValidatorFactoryContext(Type validatorType, IServiceProvider serviceProvider, EditContext editContext, object model, FieldIdentifier fieldIdentifier = default)
        {
            ValidatorType = validatorType;
            ServiceProvider = serviceProvider;
            EditContext = editContext;
            Model = model;
            FieldIdentifier = fieldIdentifier;
        }
        public Type ValidatorType { get; }
        public IServiceProvider ServiceProvider { get; }
        public EditContext EditContext { get; }
        public object Model { get; }
        public FieldIdentifier FieldIdentifier { get; }
    }
}
