using System;
using System.Collections.Generic;
using UnityEngine;

namespace MagicLeap.LeapBrush
{
    /// <summary>
    /// Base object for brush components. This class may be instantiated as either the live
    /// brush tool being manipulated by the user, or as an existing created brush stroke
    /// from the server.
    /// </summary>
    public abstract class BrushBase : MonoBehaviour
    {
        [SerializeField, Tooltip("The transform to use for new brush poses while drawing.")]
        protected Transform _brushControllerTransform;

        /// <summary>
        /// The prefab to use for this brush
        /// </summary>
        public GameObject Prefab;

        /// <summary>
        /// The spatial anchor id that this brush stroke is attached to.
        /// </summary>
        [NonSerialized]
        public string AnchorId;

        /// <summary>
        /// The unique brush stroke id for this created stroke.
        /// </summary>
        [NonSerialized]
        public string Id;

        /// <summary>
        /// The user identifier for the user that created this brush stroke.
        /// </summary>
        [NonSerialized]
        public string UserName;

        /// <summary>
        /// Whether this brush stroke was created as part of a server echo from the current user.
        /// </summary>
        [NonSerialized]
        public bool IsServerEcho;

        /// <summary>
        /// Event for this brush stroke being destroyed.
        /// </summary>
        public event Action<BrushBase> OnDestroyed;

        /// <summary>
        /// Event for a new drawing using this brush tool being completed.
        /// </summary>
        public event Action OnDrawingCompleted;

        /// <summary>
        /// Whether this brush is currently being drawn with.
        /// </summary>
        public bool IsDrawing => _drawing;

        /// <summary>
        /// List of poses that make up this brush stroke.
        /// </summary>
        public List<Pose> Poses => _poses;

        protected bool _drawing;
        protected List<Pose> _poses = new();

        public void OnDestroy()
        {
            OnDestroyed?.Invoke(this);
        }

        /// <summary>
        /// Set the colors that are being used for this brush stroke.
        /// </summary>
        /// <param name="strokeColor">The stroke (line) color.</param>
        /// <param name="fillColor">The fill color (for polygon brushes)</param>
        /// <param name="fillDimmerAlpha">
        /// The segmented dimmer alpha value (for polygon brushes)
        /// </param>
        public abstract void SetColors(Color32 strokeColor, Color32 fillColor,
            float fillDimmerAlpha);

        /// <summary>
        /// Update a set of poses for this brush, truncating any existing poses past the new
        /// received set.
        /// </summary>
        /// <param name="startIndex">The start index where new poses are being added</param>
        /// <param name="poses">The list of poses being added</param>
        /// <param name="receivedDrawing">
        /// Whether this is a received drawing from another user.
        /// </param>
        public abstract void SetPosesAndTruncate(int startIndex, IList<Pose> poses,
            bool receivedDrawing);

        /// <summary>
        /// Handle the trigger button being pushed down while using this brush as a tool.
        /// </summary>
        public abstract void OnTriggerButtonDown();

        /// <summary>
        /// Handle the trigger button being released while using this brush as a tool.
        /// </summary>
        public abstract void OnTriggerButtonUp();

        /// <summary>
        /// Dispatch the event that a drawing using this brush as a tool has completed.
        /// </summary>
        protected void DispatchOnDrawingCompleted()
        {
            OnDrawingCompleted?.Invoke();
        }
    }
}