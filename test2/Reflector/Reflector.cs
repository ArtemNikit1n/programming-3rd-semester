// <copyright file="Reflector.cs" company="ArtemNikit1n">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace Reflector;

using System.Reflection;
using System.Text;

/// <summary>
/// A class for reflexive type analysis and C# code generation based on their structure.
/// Allows you to create files with class descriptions, including fields, methods, properties, and nested types.
/// </summary>
public static class Reflector
{
    /// <summary>
    /// Creates a file named className.cs.
    /// It describes the class className with all fields, methods, nested classes, and interfaces.
    /// Visibility and static modifiers as in the passed class.
    /// Declarations of fields, methods, and nested classes must preserve genericity.
    /// </summary>
    /// <param name="someClass">The type whose structure needs to be analyzed and generated.</param>
    public static void PrintStructure(Type someClass)
    {
        ArgumentNullException.ThrowIfNull(someClass);

        string className = someClass.Name;
        string fileName = $"{className}.cs";

        using StreamWriter writer = new StreamWriter(fileName);
        writer.Write(GenerateClassCode(someClass));
    }

    private static string GetTypeName(Type type)
    {
        if (type == typeof(void))
        {
            return "void";
        }

        if (type == typeof(int))
        {
            return "int";
        }

        if (type == typeof(long))
        {
            return "long";
        }

        if (type == typeof(short))
        {
            return "short";
        }

        if (type == typeof(byte))
        {
            return "byte";
        }

        if (type == typeof(bool))
        {
            return "bool";
        }

        if (type == typeof(char))
        {
            return "char";
        }

        if (type == typeof(float))
        {
            return "float";
        }

        if (type == typeof(double))
        {
            return "double";
        }

        if (type == typeof(decimal))
        {
            return "decimal";
        }

        if (type == typeof(string))
        {
            return "string";
        }

        if (type == typeof(object))
        {
            return "object";
        }

        return type.Name.Replace('+', '.');
    }

    private static string GetAccessModifier(MethodInfo method)
    {
        if (method == null)
        {
            return string.Empty;
        }

        if (method.IsPublic)
        {
            return "public";
        }

        if (method.IsFamily)
        {
            return "protected";
        }

        if (method.IsPrivate)
        {
            return "private";
        }

        if (method.IsAssembly)
        {
            return "internal";
        }

        if (method.IsFamilyOrAssembly)
        {
            return "protected internal";
        }

        return string.Empty;
    }

    private static string FormatConstantValue(object value)
    {
        if (value is string str)
        {
            return $"\"{str.Replace("\"", "\\\"")}\"";
        }

        if (value is char c)
        {
            return $"'{c}'";
        }

        if (value is bool b)
        {
            return b ? "true" : "false";
        }

        return value.ToString() ?? "null";
    }

    private static string GenerateClassCode(Type type)
    {
        StringBuilder sb = new();

        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine();

        if (!string.IsNullOrEmpty(type.Namespace))
        {
            sb.AppendLine($"namespace {type.Namespace};");
        }

        GenerateTypeDeclaration(type, sb, 0);

        return sb.ToString();
    }

    private static void GenerateTypeDeclaration(Type type, StringBuilder sb, int indentLevel)
    {
        string indent = new(' ', indentLevel * 4);

        if (type.IsPublic || type.IsNestedPublic)
        {
            sb.Append(indent + "public ");
        }
        else if (type.IsNestedFamily)
        {
            sb.Append(indent + "protected ");
        }
        else if (type.IsNestedPrivate)
        {
            sb.Append(indent + "private ");
        }
        else if (type.IsNestedAssembly)
        {
            sb.Append(indent + "internal ");
        }

        if (type.IsAbstract && type.IsSealed)
        {
            sb.Append("static ");
        }
        else if (type.IsAbstract && !type.IsInterface)
        {
            sb.Append("abstract ");
        }
        else if (type.IsSealed && !type.IsValueType)
        {
            sb.Append("sealed ");
        }

        if (type.IsClass)
        {
            sb.Append("class ");
        }
        else if (type.IsInterface)
        {
            sb.Append("interface ");
        }
        else if (type.IsValueType && !type.IsEnum)
        {
            sb.Append("struct ");
        }

        sb.Append(GetTypeNameWithGenerics(type));

        List<string> baseTypes = [];

        if (type.BaseType != null && type.BaseType != typeof(object) && type.BaseType != typeof(ValueType))
        {
            baseTypes.Add(GetTypeNameWithGenerics(type.BaseType));
        }

        var interfaces = type.GetInterfaces();
        foreach (var oneInterface in interfaces)
        {
            baseTypes.Add(GetTypeNameWithGenerics(oneInterface));
        }

        if (baseTypes.Count > 0)
        {
            sb.Append(" : " + string.Join(", ", baseTypes));
        }

        sb.AppendLine();
        sb.AppendLine(indent + "{");

        GenerateFields(type, sb, indentLevel + 1);

        GenerateProperties(type, sb, indentLevel + 1);

        GenerateMethods(type, sb, indentLevel + 1);

        GenerateNestedTypes(type, sb, indentLevel + 1);

        sb.AppendLine(indent + "}");
    }

    private static void GenerateFields(Type type, StringBuilder sb, int indentLevel)
    {
        string indent = new(' ', indentLevel * 4);

        var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic |
                                   BindingFlags.Instance | BindingFlags.Static |
                                   BindingFlags.DeclaredOnly)
                        .Where(f => !f.IsSpecialName);

        foreach (var field in fields)
        {
            if (field.IsPublic)
            {
                sb.Append(indent + "public ");
            }
            else if (field.IsFamily)
            {
                sb.Append(indent + "protected ");
            }
            else if (field.IsPrivate)
            {
                sb.Append(indent + "private ");
            }
            else if (field.IsAssembly)
            {
                sb.Append(indent + "internal ");
            }
            else if (field.IsFamilyOrAssembly)
            {
                sb.Append(indent + "protected internal ");
            }

            if (field.IsStatic)
            {
                sb.Append("static ");
            }

            if (field.IsInitOnly)
            {
                sb.Append("readonly ");
            }

            if (field.IsLiteral && !field.IsInitOnly)
            {
                sb.Append("const ");
            }

            sb.Append(GetTypeNameWithGenerics(field.FieldType) + " ");

            sb.Append(field.Name);

            sb.AppendLine(";");
        }

        if (fields.Any())
        {
            sb.AppendLine();
        }
    }

    private static void GenerateProperties(Type type, StringBuilder sb, int indentLevel)
    {
        string indent = new(' ', indentLevel * 4);

        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic |
                                          BindingFlags.Instance | BindingFlags.Static |
                                          BindingFlags.DeclaredOnly);

        foreach (var property in properties)
        {
            var getMethod = property.GetGetMethod(true);
            var setMethod = property.GetSetMethod(true);

            string? setAccessModifier = null;
            string? getAccessModifier = null;
            if (getMethod != null && setMethod != null)
            {
                setAccessModifier = GetAccessModifier(setMethod);
                getAccessModifier = GetAccessModifier(getMethod);
            }

            if (getMethod?.IsPublic == true || setMethod?.IsPublic == true)
            {
                sb.Append(indent + "public ");
            }
            else
            {
                sb.Append(indent + "private ");
            }

            if ((getMethod?.IsStatic ?? false) || (setMethod?.IsStatic ?? false))
            {
                sb.Append("static ");
            }

            sb.Append($"{GetTypeNameWithGenerics(property.PropertyType)} {property.Name}");

            sb.AppendLine();
            sb.AppendLine(indent + "{");

            if (getMethod != null)
            {
                string getIndent = new(' ', (indentLevel + 1) * 4);
                if (!string.IsNullOrEmpty(getAccessModifier) && getAccessModifier != "public")
                {
                    sb.Append(getIndent + getAccessModifier + " ");
                }

                sb.AppendLine(getIndent + "get");
                sb.AppendLine(getIndent + "{");
                sb.AppendLine(getIndent + "    throw new NotImplementedException();");
                sb.AppendLine(getIndent + "}");
            }

            if (setMethod != null)
            {
                string setIndent = new(' ', (indentLevel + 1) * 4);
                if (!string.IsNullOrEmpty(setAccessModifier) && setAccessModifier != "public")
                {
                    sb.Append(setIndent + setAccessModifier + " ");
                }

                sb.AppendLine(setIndent + "set");
                sb.AppendLine(setIndent + "{");
                sb.AppendLine(setIndent + "    throw new NotImplementedException();");
                sb.AppendLine(setIndent + "}");
            }

            sb.AppendLine(indent + "}");
            sb.AppendLine();
        }
    }

    private static void GenerateMethods(Type type, StringBuilder sb, int indentLevel)
    {
        string indent = new(' ', indentLevel * 4);

        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic |
                                     BindingFlags.Instance | BindingFlags.Static |
                                     BindingFlags.DeclaredOnly)
                         .Where(m => !m.IsSpecialName &&
                                    !m.Name.StartsWith("get_") &&
                                    !m.Name.StartsWith("set_") &&
                                    !m.Name.StartsWith("add_") &&
                                    !m.Name.StartsWith("remove_"));

        foreach (var method in methods)
        {
            GenerateMethodSignature(method, sb, indentLevel);

            string methodIndent = new(' ', (indentLevel + 1) * 4);
            sb.AppendLine(indent + "{");

            if (method.IsAbstract || type.IsInterface)
            {
                sb.AppendLine(methodIndent + "throw new NotImplementedException();");
            }
            else if (method.ReturnType == typeof(void))
            {
                sb.AppendLine(methodIndent + "// TODO: Implement method");
            }
            else
            {
                sb.AppendLine(methodIndent + $"return default({GetTypeNameWithGenerics(method.ReturnType)});");
            }

            sb.AppendLine(indent + "}");
            sb.AppendLine();
        }
    }

    private static void GenerateMethodSignature(MethodInfo method, StringBuilder sb, int indentLevel)
    {
        string indent = new(' ', indentLevel * 4);

        if (method.IsPublic)
        {
            sb.Append(indent + "public ");
        }
        else if (method.IsFamily)
        {
            sb.Append(indent + "protected ");
        }
        else if (method.IsPrivate)
        {
            sb.Append(indent + "private ");
        }
        else if (method.IsAssembly)
        {
            sb.Append(indent + "internal ");
        }
        else if (method.IsFamilyOrAssembly)
        {
            sb.Append(indent + "protected internal ");
        }

        if (method.IsStatic)
        {
            sb.Append("static ");
        }

        if (method.IsAbstract)
        {
            sb.Append("abstract ");
        }

        if (method.IsVirtual && !method.IsAbstract && !method.IsFinal)
        {
            sb.Append("virtual ");
        }

        if (method.IsGenericMethod)
        {
            var genericParams = method.GetGenericArguments();
            sb.Append($"<{string.Join(", ", genericParams.Select(g => g.Name))}> ");
        }

        sb.Append($"{GetTypeNameWithGenerics(method.ReturnType)} {method.Name}");

        var parameters = method.GetParameters();
        sb.Append("(" + string.Join(", ", parameters.Select(p =>
        {
            string paramStr = string.Empty;
            if (p.ParameterType.IsByRef)
            {
                if (p.IsOut)
                {
                    paramStr = "out ";
                }
                else if (p.IsIn)
                {
                    paramStr = "in ";
                }
                else
                {
                    paramStr = "ref ";
                }
            }

            paramStr += $"{GetTypeNameWithGenerics(p.ParameterType)} {p.Name}";

            return paramStr;
        })) + ")");

        sb.AppendLine();
    }

    private static void GenerateNestedTypes(Type type, StringBuilder sb, int indentLevel)
    {
        var nestedTypes = type.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic);

        foreach (var nestedType in nestedTypes)
        {
            if (nestedType.IsNested)
            {
                GenerateTypeDeclaration(nestedType, sb, indentLevel);
                sb.AppendLine();
            }
        }
    }

    private static string GetTypeNameWithGenerics(Type type)
    {
        if (!type.IsGenericType)
        {
            return GetTypeName(type);
        }

        if (type.IsGenericTypeDefinition)
        {
            return $"{type.Name.Split('`')[0]}<{string.Join(", ", type.GetGenericArguments().Select(a => a.Name))}>";
        }

        string baseName = GetTypeName(type.GetGenericTypeDefinition()).Split('`')[0];
        var genericArgs = type.GetGenericArguments();

        return $"{baseName}<{string.Join(", ", genericArgs.Select(GetTypeNameWithGenerics))}>";
    }
}
