using System;
using UnityEngine;

namespace MagicLeap.LeapBrush
{
    /// <summary>
    /// Base object for brush tool components.
    /// </summary>
    public abstract class BrushToolBase : MonoBehaviour
    {
        [SerializeField, Tooltip("The transform to use for new brush poses while drawing.")]
        protected Transform _brushControllerTransform;

        /// <summary>
        /// The prefab to use for this brush
        /// </summary>
        public GameObject Prefab;

        /// <summary>
        /// Event for a new drawing using this brush tool being completed.
        /// </summary>
        public event Action<BrushToolBase> OnDrawingCompleted;

        /// <summary>
        /// Whether this brush is currently being drawn with.
        /// </summary>
        public bool IsDrawing => _drawing;

        /// <summary>
        /// The brush instance used when the tool is drawing.
        /// </summary>
        public abstract BrushBase Brush { get; }

        protected bool _drawing;
        protected Camera _camera;

        protected void OnEnable()
        {
            _camera = Camera.main;
        }

        /// <summary>
        /// Handle the select input action starting while using this brush as a tool.
        /// </summary>
        public abstract void OnSelectStarted();

        /// <summary>
        /// Handle the select input action ending while using this brush as a tool.
        /// </summary>
        public abstract void OnSelectEnded();

        /// <summary>
        /// Dispatch the event that a drawing using this brush as a tool has completed.
        /// </summary>
        protected void DispatchOnDrawingCompleted()
        {
            OnDrawingCompleted?.Invoke(this);
        }

        protected bool IsBrushControllerInFieldOfView()
        {
            Vector3 viewportPoint = _camera.WorldToViewportPoint(
                _brushControllerTransform.position);
            return viewportPoint.x is >= 0 and <= 1 && viewportPoint.y is >= 0
                and <= 1 && !(viewportPoint.z < 0);
        }
    }
}