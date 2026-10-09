using System;
using AirCanvas.Drawing;
using UnityEngine;

namespace AirCanvas.Editor
{
    internal static class ProjectChecks
    {
        public static void Run()
        {
            var a = new GameObject("Test stroke A"); var b = new GameObject("Test stroke B"); var c = new GameObject("Test stroke C");
            using var history = new StrokeHistory(stroke => UnityEngine.Object.DestroyImmediate(stroke));
            history.Add(a); history.Add(b);
            Check(history.VisibleCount == 2 && !history.CanRedo, "Two strokes are visible");
            history.Undo(); Check(a.activeSelf && !b.activeSelf && history.CanRedo, "Undo removes only the last stroke");
            history.Redo(); Check(b.activeSelf, "Redo restores the original stroke");
            history.Clear(); Check(history.VisibleCount == 0, "Clear hides all strokes");
            history.Undo(); Check(a.activeSelf && b.activeSelf, "Clear can be undone as one action");
            history.Redo(); history.Undo(); history.Undo();
            history.Add(c); Check(!history.CanRedo && a.activeSelf && c.activeSelf && (!b || !b.activeSelf), "New strokes discard the redo branch");
            history.Undo(); history.Undo(); Check(history.VisibleCount == 0, "Undo reaches the empty canvas");
            history.Redo(); history.Redo(); Check(history.VisibleCount == 2, "Redo restores the branch");
            Debug.Log("AIR_CANVAS_HISTORY_CHECKS_PASSED");
        }
        private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    }
}
