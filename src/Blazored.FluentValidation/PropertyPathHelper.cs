using System.Collections;
using System.Reflection;
using Microsoft.AspNetCore.Components.Forms;

namespace Blazored.FluentValidation;

internal static class PropertyPathHelper
{
    private class Node
    {
        public Node? Parent { get; set; }
        public object? ModelObject { get; set; }
        public string? PropertyName { get; set; }
        public int? Index { get; set; }
    }

    public static string ToFluentPropertyPath(EditContext editContext, FieldIdentifier fieldIdentifier)
    {
        var nodes = new Stack<Node>();
        nodes.Push(new Node()
        {
            ModelObject = editContext.Model,
        });

        while (nodes.Any())
        {
            var currentNode = nodes.Pop();
            var currentModelObject = currentNode.ModelObject;

            if (currentModelObject == fieldIdentifier.Model)
            {
                return BuildPropertyPath(currentNode, fieldIdentifier);
            }
            
            var nonPrimitiveProperties = currentModelObject?.GetType()
                .GetProperties()
                .Where(prop => !prop.PropertyType.IsPrimitive || prop.PropertyType.IsArray) ?? new List<PropertyInfo>();

            foreach (var nonPrimitiveProperty in nonPrimitiveProperties)
            {
                var instance = nonPrimitiveProperty.GetValue(currentModelObject);

                if (instance == fieldIdentifier.Model)
                {
                    var node = new Node()
                    {
                        Parent = currentNode,
                        PropertyName = nonPrimitiveProperty.Name,
                        ModelObject = instance
                    };
                    
                    return BuildPropertyPath(node, fieldIdentifier);
                }
                
                if(instance is IEnumerable enumerable)
                {
                    var itemIndex = 0;
                    foreach (var item in enumerable)
                    {
                        nodes.Push(new Node()
                        {
                            ModelObject = item,
                            Parent = currentNode,
                            PropertyName = nonPrimitiveProperty.Name,
                            Index = itemIndex++
                        });
                    }
                }
                else if(instance is not null)
                {
                    nodes.Push(new Node()
                    {
                        ModelObject = instance,
                        Parent = currentNode,
                        PropertyName = nonPrimitiveProperty.Name
                    });
                }
            }
        }

        return string.Empty;
    }
    
    private static string BuildPropertyPath(Node currentNode, FieldIdentifier fieldIdentifier)
    {
        var pathParts = new List<string>();
        pathParts.Add(fieldIdentifier.FieldName);
        var next = currentNode;

        while (next is not null)
        {
            if (!string.IsNullOrEmpty(next.PropertyName))
            {
                if (next.Index is not null)
                {
                    pathParts.Add($"{next.PropertyName}[{next.Index}]");
                }
                else
                {
                    pathParts.Add(next.PropertyName);
                }
            }

            next = next.Parent;
        }

        pathParts.Reverse();

        return string.Join('.', pathParts);
    }
}