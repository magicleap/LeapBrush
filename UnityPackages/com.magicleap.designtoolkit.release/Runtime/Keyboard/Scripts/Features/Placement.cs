// Copyright (c) 2019-present, Magic Leap, Inc. All Rights Reserved.
// Use of this file is governed by the Developer Agreement, located
// here: https://auth.magicleap.com/terms/developer
using System.Collections;
using UnityEngine;

/// <summary>
/// Placement of an object relative to a base transform
/// </summary>
public class Placement : MonoBehaviour
{
    #region NestedType / Constructors
    [System.Serializable]
    public class PlacementBase
    {
        public Transform BaseTransform;
        public Vector3 SpawnStartLocalPositionOffset;
        public Vector3 SpawnEndLocalPositionOffset;

        // Rotational offsets are in degrees
        public Vector3 SpawnStartLocalRotationOffset;
        public Vector3 SpawnEndLocalRotationOffset;

        // If this is false, the VKB does not lock to the base, then the base is only used for the
        // spawn animation. This gives us the opportunity to maintain the previous behavior of VKB
        // staying free without locking to anything. To achieve that, just set this to false and
        // use the camera as the BaseTransform for the spawn animation (BaseTransform is set to
        // be the main camera's transform automatically if it is null)
        public bool LockToSpawnEndAfterSpawnAnim;
    }
    #endregion

    #region Public Members
    // Home Menu programmatically modifies the locked position & rotation
    // when switching between near/far field
    public PlacementBase Base;
    #endregion

    #region [SerializeField] Private Members
    [Tooltip("The object to be placed smoothly and locked to the base transform")]
    [SerializeField]
    private Transform _objectToPlace;

    [Tooltip("Time of the ease-in animation")]
    [SerializeField]
    private float _lerpTime = 1.0f;

    [SerializeField]
    private bool _rebuildKDTreesAfterPlacement = true;
    #endregion [SerializeField] Private Members

    #region Private Members
    private bool _lockedToBase;
    private IEnumerator _initPlacementCoroutine;
    #endregion Private Members

    #region MonoBehaviour Methods
    private void Awake()
    {
        if (Base.BaseTransform == null)
        {
            Base.BaseTransform = Camera.main.transform;
        }
    }
    private void OnEnable()
    {
        LockToBase(false);
        SmoothPlace();
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        _initPlacementCoroutine = null;
    }

    private void Update()
    {
        if (_lockedToBase)
        {
            Transform baseTransform = Base.BaseTransform;
            _objectToPlace.transform.position =
                GetOffsetPosition(baseTransform, Base.SpawnEndLocalPositionOffset);
            _objectToPlace.transform.rotation =
                GetOffsetRotation(baseTransform, Base.SpawnEndLocalRotationOffset);
        }
    }
    #endregion MonoBehaviour Methods

    #region Public Methods
    public void LockToBase(bool locking)
    {
        if (_initPlacementCoroutine != null)
        {
            StopCoroutine(_initPlacementCoroutine);
            _initPlacementCoroutine = null;
        }
        _lockedToBase = locking;
    }
    public void SmoothPlace()
    {
        if (_initPlacementCoroutine == null)
        {
            _initPlacementCoroutine = SmoothPlaceCoroutine();
            StartCoroutine(_initPlacementCoroutine);
        }
    }
    #endregion Public Methods

    #region Private Methods
    private Vector3 GetOffsetPosition(Transform referenceTransform, Vector3 positionOffset)
    {
        return referenceTransform.position + referenceTransform.rotation * positionOffset;
    }

    private Quaternion GetOffsetRotation(Transform referenceTransform, Vector3 rotationOffset)
    {
        return referenceTransform.rotation * Quaternion.Euler(rotationOffset);
    }
    #endregion Private Methods

    #region Coroutines
    // Plays an animation to move _objectToPlace from spawn start location to spawn end location
    IEnumerator SmoothPlaceCoroutine()
    {
        // If not locking to the base at the end of spawn animation, then likely the camera
        // is used as the base transform. In this case we:
        // 1. eliminate the y component when determining the start & end locations so
        //    the object does not move much vertically (caused by pitch rotation of the camera)
        //    during spawn animation;
        // 2. determine the end location at the beginning of the animation instead of every frame
        //    so the object does not follow the camera's movement during the spawn animation.
        Transform baseTransform = Base.BaseTransform;
        Vector3 baseForward = baseTransform.rotation * Vector3.forward;
        Vector3 baseForwardNoY = new Vector3(baseForward.x, 0.0f, baseForward.z);
        Quaternion baseRotationNoY =
            Quaternion.FromToRotation(Vector3.forward, Vector3.Normalize(baseForwardNoY));

        Vector3 startPosition = Base.LockToSpawnEndAfterSpawnAnim
            ? GetOffsetPosition(baseTransform, Base.SpawnStartLocalPositionOffset)
            : baseTransform.position + baseRotationNoY * Base.SpawnStartLocalPositionOffset;

        Quaternion startRotation = Base.LockToSpawnEndAfterSpawnAnim
            ? GetOffsetRotation(baseTransform, Base.SpawnStartLocalRotationOffset)
            : baseRotationNoY * Quaternion.Euler(Base.SpawnStartLocalRotationOffset);

        Vector3 endPosition =
            baseTransform.position + baseRotationNoY * Base.SpawnEndLocalPositionOffset;

        Quaternion endRotation =
            baseRotationNoY * Quaternion.Euler(Base.SpawnEndLocalRotationOffset);

        float elapsedTime = 0.0f;
        float percentComplete = 0.0f;
        while (elapsedTime < _lerpTime)
        {
            percentComplete = Mathf.Clamp(elapsedTime / _lerpTime, 0.0f, 1.0f);

            if (Base.LockToSpawnEndAfterSpawnAnim)
            {
                // In this case end position & rotation are determined frame-by-frame cause
                // we want to make sure the object ends up at the lock location at the end of
                // the spawn animation
                endPosition = GetOffsetPosition(baseTransform, Base.SpawnEndLocalPositionOffset);
                endRotation = GetOffsetRotation(baseTransform, Base.SpawnEndLocalRotationOffset);
            }

            _objectToPlace.transform.position =
                Vector3.Lerp(startPosition, endPosition, percentComplete);
            _objectToPlace.transform.rotation =
                Quaternion.Lerp(startRotation, endRotation, percentComplete);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        if (Base.LockToSpawnEndAfterSpawnAnim)
        {
            // make sure _objectToPlace ends up in the locked location
            _objectToPlace.transform.position =
                GetOffsetPosition(baseTransform, Base.SpawnEndLocalPositionOffset);
            _objectToPlace.transform.rotation =
                GetOffsetRotation(baseTransform, Base.SpawnEndLocalRotationOffset);
        }

#if FIXME
        if (_rebuildKDTreesAfterPlacement)
        {
            DistanceMaster.RebuildKDTreesNextFrame();
        }
#endif

        if (Base.LockToSpawnEndAfterSpawnAnim)
        {
            LockToBase(true);
        }

        _initPlacementCoroutine = null;
    }
    #endregion Coroutines
}
