using System;
using System.Collections.Generic;
using UnityEngine;

namespace MagicLeap.LeapBrush
{
    public abstract class BrushBase : MonoBehaviour
    {
        [SerializeField]
        protected Transform _brushControllerTransform;

        public GameObject Prefab;

        [NonSerialized]
        public string AnchorId;

        [NonSerialized]
        public string Id;

        [NonSerialized]
        public string UserName;

        [NonSerialized]
        public bool IsServerEcho;

        public event Action<BrushBase> OnDestroyed;
        public event Action OnDrawingCompleted;

        public bool IsDrawing => _drawing;

        public List<Pose> Poses => _poses;

        protected bool _drawing;
        protected List<Pose> _poses = new();

        public void OnDestroy()
        {
            OnDestroyed?.Invoke(this);
        }

        public abstract void SetColors(Color32 strokeColor, Color32 fillColor,
            float fillDimmerAlpha);

        public abstract void SetPosesAndTruncate(int startIndex, IList<Pose> poses,
            bool receivedDrawing);

        public abstract void OnTriggerButtonDown();
        public abstract void OnTriggerButtonUp();

        protected void DispatchOnDrawingCompleted()
        {
            OnDrawingCompleted?.Invoke();
        }
    }
}