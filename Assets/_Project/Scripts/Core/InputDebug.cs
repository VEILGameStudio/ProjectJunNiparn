// InputDebug
// Debug messages for the input system that are turned OFF by default.
// The [Conditional] attribute means the compiler REMOVES these calls (and the text
// they build) unless the INPUT_DEBUG symbol is defined, so the Console stays clean
// and there is no cost while playing normally.
//
// To turn the input logs on:
//   Edit > Project Settings > Player > Other Settings > Scripting Define Symbols
//   add:  INPUT_DEBUG
//
// Put this on: nothing. It is a static helper used by the input scripts.

using UnityEngine;

public static class InputDebug
{
    private const string DebugSymbol = "INPUT_DEBUG";

    // Prints an input message, only when INPUT_DEBUG is defined.
    [System.Diagnostics.Conditional(DebugSymbol)]
    public static void Log(string message, Object context = null)
    {
        Debug.Log($"[Input] {message}", context);
    }

    // Prints an input warning, only when INPUT_DEBUG is defined.
    [System.Diagnostics.Conditional(DebugSymbol)]
    public static void LogWarning(string message, Object context = null)
    {
        Debug.LogWarning($"[Input] {message}", context);
    }
}
