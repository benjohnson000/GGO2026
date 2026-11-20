using UnityEngine;

namespace benjohnson
{
    /// <summary>
    /// Sizes (and optionally centers) a world-space canvas so it fills the target camera's view.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(Canvas))]
    [DisallowMultipleComponent]
    public class MatchWorldCanvasToCamera : MonoBehaviour
    {
        [Header("Camera")]
        public Camera targetCamera;

        [Header("Behavior")]
        [Tooltip("Update every frame (Editor + Play). Turn off to size only on demand.")]
        public bool matchContinuously = true;

        [Tooltip("Keep the canvas centered in front of the camera (like Screen Space).")]
        public bool keepCentered = true;

        private RectTransform _rt;
        private Canvas _canvas;

        // Last applied values, so we only touch the transform when something changed
        private Vector3 _lastCamPos;
        private Quaternion _lastCamRot;
        private float _lastSize;
        private float _lastFov;
        private float _lastAspect;
        private bool _hasApplied;

        private void OnEnable()
        {
            CacheRefs();
            MatchNow();
        }

        private void Reset()
        {
            CacheRefs();
            if (_canvas != null) _canvas.renderMode = RenderMode.WorldSpace;
            if (targetCamera == null) targetCamera = Camera.main;
            MatchNow();
        }

        private void Update()
        {
            if (matchContinuously && CameraChanged())
                MatchNow();
        }

        private void CacheRefs()
        {
            if (_rt == null) _rt = GetComponent<RectTransform>();
            if (_canvas == null) _canvas = GetComponent<Canvas>();
            if (targetCamera == null) targetCamera = Camera.main;
        }

        private bool CameraChanged()
        {
            if (!_hasApplied || targetCamera == null) return true;

            Transform t = targetCamera.transform;
            return t.position != _lastCamPos
                || t.rotation != _lastCamRot
                || targetCamera.orthographicSize != _lastSize
                || targetCamera.fieldOfView != _lastFov
                || targetCamera.aspect != _lastAspect;
        }

        [ContextMenu("Match Now")]
        public void MatchNow()
        {
            if (_rt == null || _canvas == null) CacheRefs();
            if (_rt == null || targetCamera == null) return;

            // Determine distance between camera and canvas along camera forward
            float distance = GetSignedDistanceAlongForward(targetCamera, _rt.position);
            if (keepCentered)
            {
                distance = Mathf.Max(0.0001f, distance);
                _rt.position = targetCamera.transform.position + targetCamera.transform.forward * distance;
            }

            // Compute world-space size of camera frustum at that distance
            float height, width;
            if (targetCamera.orthographic)
            {
                height = 2f * targetCamera.orthographicSize;
                width = height * targetCamera.aspect;
            }
            else
            {
                float d = Mathf.Max(0.0001f, distance);
                float halfFovRad = 0.5f * targetCamera.fieldOfView * Mathf.Deg2Rad;
                height = 2f * d * Mathf.Tan(halfFovRad);
                width = height * targetCamera.aspect;
            }

            // Convert world-space size to local sizeDelta, accounting for lossy scale
            Vector3 lossy = _rt.lossyScale;
            float sx = Mathf.Approximately(lossy.x, 0f) ? 1f : lossy.x;
            float sy = Mathf.Approximately(lossy.y, 0f) ? 1f : lossy.y;

            _rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width / sx);
            _rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height / sy);

            RememberCameraState();
        }

        private void RememberCameraState()
        {
            Transform t = targetCamera.transform;
            _lastCamPos = t.position;
            _lastCamRot = t.rotation;
            _lastSize = targetCamera.orthographicSize;
            _lastFov = targetCamera.fieldOfView;
            _lastAspect = targetCamera.aspect;
            _hasApplied = true;
        }

        private static float GetSignedDistanceAlongForward(Camera cam, Vector3 worldPos)
        {
            Vector3 toCanvas = worldPos - cam.transform.position;
            return Vector3.Dot(cam.transform.forward, toCanvas);
        }
    }
}
