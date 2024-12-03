using System.Collections.Generic;
using UnityEngine;

public static class NameManager
{
    private static HashSet<string> usedNames = new HashSet<string>();

    public static bool IsNameAvailable(string name)
    {
        return !usedNames.Contains(name);
    }

    public static void RegisterName(string name)
    {
        usedNames.Add(name);
    }
}
