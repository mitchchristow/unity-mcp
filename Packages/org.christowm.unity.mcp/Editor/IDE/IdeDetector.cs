using System.IO;
using UnityEditor;
using UnityEngine;

namespace UnityMcp.Editor.IDE
{
    public static class IdeDetector
    {
        public static bool IsCursorDetected()
        {
            // Simple check: look for .cursor folder or cursor specific files
            return Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), ".cursor")) ||
                   File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "cursor.json"));
        }

        public static bool IsVsCodeDetected()
        {
            return Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), ".vscode"));
        }
        
        public static bool IsAntigravityDetected()
        {
             // Check for .gemini folder or specific Antigravity markers if known
             // For now, let's assume if the user says so, or if we see .agent
             return Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), ".agent")) ||
                    Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), ".gemini"));
        }
    }
}
