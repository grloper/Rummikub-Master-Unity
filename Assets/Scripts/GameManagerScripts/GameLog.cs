using UnityEngine;

// Verbose gameplay diagnostics. Calls to GameLog.Verbose (and any method marked with
// [Conditional("RUMMIKUB_VERBOSE")]) are compiled out entirely - including argument
// evaluation - unless RUMMIKUB_VERBOSE is added to
// Project Settings > Player > Scripting Define Symbols.
public static class GameLog
{
    [System.Diagnostics.Conditional("RUMMIKUB_VERBOSE")]
    public static void Verbose(string message) => Debug.Log(message);
}
