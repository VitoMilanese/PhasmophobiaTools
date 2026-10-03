using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace PhasmophobiaTools;

internal static class ImguiBridge
{
    private static bool _initializationAttempted;

    private static MethodInfo _box = null!;
    private static MethodInfo _beginArea = null!;
    private static MethodInfo _endArea = null!;
    private static MethodInfo _label = null!;
    private static MethodInfo _button = null!;
    private static MethodInfo _textField = null!;
    private static MethodInfo _beginHorizontal = null!;
    private static MethodInfo _endHorizontal = null!;
    private static MethodInfo _flexibleSpace = null!;
    private static MethodInfo _space = null!;
    private static MethodInfo _beginScrollView = null!;
    private static MethodInfo _endScrollView = null!;

    public static string? InitializationError { get; private set; }

    public static bool EnsureInitialized()
    {
        if (_initializationAttempted)
        {
            return InitializationError is null;
        }

        _initializationAttempted = true;

        try
        {
            var assembly = AppDomain.CurrentDomain
                .GetAssemblies()
                .FirstOrDefault(
                    candidate => string.Equals(
                        candidate.GetName().Name,
                        "UnityEngine.IMGUIModule",
                        StringComparison.Ordinal));

            assembly ??= Assembly.Load("UnityEngine.IMGUIModule");

            var guiType = assembly.GetType("UnityEngine.GUI", throwOnError: true)!;
            var guiLayoutType = assembly.GetType("UnityEngine.GUILayout", throwOnError: true)!;

            _box = FindExactMethod(guiType, "Box", typeof(Rect), typeof(string));
            _beginArea = FindExactMethod(guiLayoutType, "BeginArea", typeof(Rect));
            _endArea = FindExactMethod(guiLayoutType, "EndArea");
            _label = FindMethodWithOptions(guiLayoutType, "Label", typeof(string));
            _button = FindMethodWithOptions(guiLayoutType, "Button", typeof(string));
            _textField = FindMethodWithOptions(guiLayoutType, "TextField", typeof(string));
            _beginHorizontal = FindMethodWithOptions(guiLayoutType, "BeginHorizontal");
            _endHorizontal = FindExactMethod(guiLayoutType, "EndHorizontal");
            _flexibleSpace = FindExactMethod(guiLayoutType, "FlexibleSpace");
            _space = FindExactMethod(guiLayoutType, "Space", typeof(float));
            _beginScrollView = FindMethodWithOptions(
                guiLayoutType,
                "BeginScrollView",
                typeof(Vector2));
            _endScrollView = FindExactMethod(guiLayoutType, "EndScrollView");

            return true;
        }
        catch (Exception exception)
        {
            InitializationError = exception.ToString();
            return false;
        }
    }

    public static void Box(Rect position, string text)
    {
        _box.Invoke(null, new object?[] { position, text });
    }

    public static void BeginArea(Rect screenRect)
    {
        _beginArea.Invoke(null, new object?[] { screenRect });
    }

    public static void EndArea()
    {
        _endArea.Invoke(null, null);
    }

    public static void Label(string text)
    {
        _label.Invoke(null, new object?[] { text, null });
    }

    public static bool Button(string text)
    {
        return (bool)(_button.Invoke(null, new object?[] { text, null }) ?? false);
    }

    public static string TextField(string text)
    {
        return (string?)_textField.Invoke(null, new object?[] { text, null }) ?? text;
    }

    public static void BeginHorizontal()
    {
        _beginHorizontal.Invoke(null, new object?[] { null });
    }

    public static void EndHorizontal()
    {
        _endHorizontal.Invoke(null, null);
    }

    public static void FlexibleSpace()
    {
        _flexibleSpace.Invoke(null, null);
    }

    public static void Space(float pixels)
    {
        _space.Invoke(null, new object?[] { pixels });
    }

    public static Vector2 BeginScrollView(Vector2 scrollPosition)
    {
        return (Vector2)(_beginScrollView.Invoke(
            null,
            new object?[] { scrollPosition, null }) ?? scrollPosition);
    }

    public static void EndScrollView()
    {
        _endScrollView.Invoke(null, null);
    }

    private static MethodInfo FindExactMethod(
        Type type,
        string name,
        params Type[] parameterTypes)
    {
        return type.GetMethod(
                   name,
                   BindingFlags.Public | BindingFlags.Static,
                   binder: null,
                   types: parameterTypes,
                   modifiers: null)
               ?? throw new MissingMethodException(
                   type.FullName,
                   $"{name}({string.Join(", ", parameterTypes.Select(item => item.Name))})");
    }

    private static MethodInfo FindMethodWithOptions(
        Type type,
        string name,
        params Type[] leadingParameterTypes)
    {
        var method = type
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(candidate => string.Equals(candidate.Name, name, StringComparison.Ordinal))
            .FirstOrDefault(
                candidate =>
                {
                    var parameters = candidate.GetParameters();

                    if (parameters.Length != leadingParameterTypes.Length + 1)
                    {
                        return false;
                    }

                    for (var index = 0; index < leadingParameterTypes.Length; index++)
                    {
                        if (parameters[index].ParameterType != leadingParameterTypes[index])
                        {
                            return false;
                        }
                    }

                    return IsOptionsArray(parameters[^1].ParameterType);
                });

        return method
               ?? throw new MissingMethodException(
                   type.FullName,
                   $"{name} with a GUILayout options array");
    }

    private static bool IsOptionsArray(Type type)
    {
        return type.IsArray
            || type.Name.Contains("Il2CppReferenceArray", StringComparison.Ordinal)
            || type.BaseType?.Name.Contains("Il2CppArrayBase", StringComparison.Ordinal) == true;
    }
}
