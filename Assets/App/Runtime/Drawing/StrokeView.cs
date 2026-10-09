using UnityEngine;

namespace AirCanvas.Drawing
{
    public sealed class StrokeView : MonoBehaviour
    {
        private LineRenderer line;
        private Vector3 previous;
        private int count;
        public int PointCount => count;
        public bool Full => count >= 4096;

        public void Initialize(Transform canvas, Material material, Color color, float width, Vector3 point)
        {
            transform.SetParent(canvas, false);
            line = gameObject.AddComponent<LineRenderer>();
            line.sharedMaterial = material;
            line.useWorldSpace = false;
            line.startColor = line.endColor = color;
            line.widthMultiplier = width;
            line.numCapVertices = 4; line.numCornerVertices = 2;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.positionCount = 0;
            Append(point, true);
            // A single tap is a short capped segment, so dots remain visible.
            Append(point + Vector3.up * .0001f, true);
        }
        public bool Append(Vector3 world, bool force = false)
        {
            var local = transform.InverseTransformPoint(world);
            if (Full || (!force && Vector3.Distance(previous, local) < .0025f)) return false;
            previous = local;
            line.positionCount = ++count;
            line.SetPosition(count - 1, local);
            return true;
        }
    }
}
