using MixedReality.Toolkit.Editor;
using UnityEditor;
using UnityEngine;

namespace MagicLeap.LeapBrush
{
    [CustomEditor(typeof(ColorPickerHueRing))]
    public class ColorPickerHueRingEditor : StatefulInteractableEditor
    {
        protected override void DrawInspector()
        {
            base.DrawInspector();

            ColorPickerHueRing hueRing = (ColorPickerHueRing) target;

            if (GUILayout.Button("Generate mesh..."))
            {
                hueRing.GenerateMesh();
            }
        }
    }
}
