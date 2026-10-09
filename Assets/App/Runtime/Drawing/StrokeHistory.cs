using System;
using System.Collections.Generic;
using UnityEngine;

namespace AirCanvas.Drawing
{
    // Commands retain stroke objects so undo and redo preserve their original geometry.
    public sealed class StrokeHistory : IDisposable
    {
        private sealed class Change
        {
            public readonly List<GameObject> Strokes;
            public readonly bool Show;
            public Change(List<GameObject> strokes, bool show) { Strokes = strokes; Show = show; }
            public void Apply(bool forward) { foreach (var stroke in Strokes) if (stroke) stroke.SetActive(forward ? Show : !Show); }
        }
        private readonly List<Change> changes = new();
        private readonly List<GameObject> owned = new();
        private int cursor;
        private readonly Action<GameObject> release;
        public StrokeHistory(Action<GameObject> destroy = null) { release = destroy ?? (stroke => UnityEngine.Object.Destroy(stroke)); }
        public bool CanUndo => cursor > 0;
        public bool CanRedo => cursor < changes.Count;
        public int VisibleCount { get { int n = 0; foreach (var stroke in owned) if (stroke && stroke.activeSelf) n++; return n; } }
        public event Action Changed;

        public void Add(GameObject stroke)
        {
            owned.Add(stroke);
            Push(new Change(new List<GameObject> { stroke }, true));
        }
        public void Clear()
        {
            var visible = owned.FindAll(stroke => stroke && stroke.activeSelf);
            if (visible.Count > 0) Push(new Change(visible, false));
        }
        private void Push(Change change)
        {
            if (cursor < changes.Count) changes.RemoveRange(cursor, changes.Count - cursor);
            // Hidden strokes referenced only by a discarded redo branch can be released.
            for (int i = owned.Count - 1; i >= 0; i--)
            {
                var stroke = owned[i];
                if (stroke && !stroke.activeSelf && !changes.Exists(c => c.Strokes.Contains(stroke)))
                { release(stroke); owned.RemoveAt(i); }
            }
            changes.Add(change); cursor++;
            change.Apply(true); Changed?.Invoke();
        }
        public void Undo() { if (!CanUndo) return; changes[--cursor].Apply(false); Changed?.Invoke(); }
        public void Redo() { if (!CanRedo) return; changes[cursor++].Apply(true); Changed?.Invoke(); }
        public void Dispose()
        {
            foreach (var stroke in owned) if (stroke) release(stroke);
            owned.Clear(); changes.Clear(); cursor = 0;
        }
    }
}
