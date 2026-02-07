using System.Collections;
using UnityEngine;

namespace InkSim
{
    /// <summary>
    /// Smooth camera follow with dead zone and bounds clamping.
    /// Attach to the Main Camera.
    /// </summary>
    public class CameraController : MonoBehaviour
    {
        [Header("Target")]
        public Transform target;
        public GridWorld gridWorld;
        
        [Header("Follow Settings")]
        [Tooltip("How quickly camera catches up to player")]
        public float smoothSpeed = 8f;
        
        [Tooltip("Camera won't move until player exits this zone (in world units)")]
        public Vector2 deadZone = new Vector2(0.5f, 0.3f);
        
        [Header("Look Ahead")]
        [Tooltip("Camera shifts slightly in movement direction")]
        public bool enableLookAhead = true;
        public float lookAheadDistance = 1f;
        public float lookAheadSpeed = 3f;
        
        private Camera _cam;
        private Vector3 _currentLookAhead;
        private Vector3 _lastTargetPos;

        // Temporary focus override (used by ConversationLogPanel click-to-focus)
        private bool _focusOverride;
        private Vector3 _focusPosition;
        private Coroutine _focusCoroutine;
        
        private void Start()
        {
            _cam = GetComponent<Camera>();
            
            Debug.Log($"[CameraController] Start - target={target?.name}, gridWorld={(gridWorld != null ? $"{gridWorld.width}x{gridWorld.height}" : "NULL")}");
            
            if (target != null)
            {
                _lastTargetPos = target.position;
                // Snap to target initially
                Vector3 startPos = target.position;
                startPos.z = -10f;
                transform.position = ClampToBounds(startPos);
            }
        }
        
        private void LateUpdate()
        {
            if (_cam == null) return;

            // Focus override: smoothly pan to a specific world position, skip normal follow
            if (_focusOverride)
            {
                Vector3 focusCurrentPos = transform.position;
                Vector3 focusSmoothed = Vector3.Lerp(focusCurrentPos, _focusPosition, smoothSpeed * Time.deltaTime);
                focusSmoothed.z = -10f;
                transform.position = ClampToBounds(focusSmoothed);
                return;
            }

            if (target == null) return;

            Vector3 targetPos = target.position;
            Vector3 currentPos = transform.position;

            // Calculate look-ahead based on movement direction
            if (enableLookAhead)
            {
                Vector3 moveDir = (targetPos - _lastTargetPos).normalized;
                Vector3 desiredLookAhead = moveDir * lookAheadDistance;
                desiredLookAhead.z = 0;
                _currentLookAhead = Vector3.Lerp(_currentLookAhead, desiredLookAhead, lookAheadSpeed * Time.deltaTime);
                _lastTargetPos = targetPos;
            }
            
            // Desired position with look-ahead
            Vector3 desired = targetPos + _currentLookAhead;
            desired.z = -10f;
            
            // Dead zone - only move if target is outside the dead zone
            Vector3 diff = desired - currentPos;
            diff.z = 0;
            
            if (Mathf.Abs(diff.x) < deadZone.x)
                desired.x = currentPos.x;
            if (Mathf.Abs(diff.y) < deadZone.y)
                desired.y = currentPos.y;
            
            // Smooth interpolation
            Vector3 smoothed = Vector3.Lerp(currentPos, desired, smoothSpeed * Time.deltaTime);
            smoothed.z = -10f;
            
            // Clamp to map bounds
            transform.position = ClampToBounds(smoothed);
        }
        
        private Vector3 ClampToBounds(Vector3 pos)
        {
            if (gridWorld == null || _cam == null) return pos;
            
            float halfHeight = _cam.orthographicSize;
            float halfWidth = halfHeight * _cam.aspect;
            
            float mapWidth = gridWorld.width * gridWorld.tileSize;
            float mapHeight = gridWorld.height * gridWorld.tileSize;
            
            // Clamp so camera doesn't show outside map
            float minX = halfWidth;
            float maxX = mapWidth - halfWidth;
            float minY = halfHeight;
            float maxY = mapHeight - halfHeight;
            
            // Handle case where map is smaller than viewport
            if (minX > maxX)
            {
                pos.x = mapWidth / 2f;
            }
            else
            {
                pos.x = Mathf.Clamp(pos.x, minX, maxX);
            }
            
            if (minY > maxY)
            {
                pos.y = mapHeight / 2f;
            }
            else
            {
                pos.y = Mathf.Clamp(pos.y, minY, maxY);
            }
            
            return pos;
        }
        
        /// <summary>
        /// Instantly snap camera to target (useful after teleport/load).
        /// </summary>
        public void SnapToTarget()
        {
            if (target == null) return;

            Vector3 pos = target.position;
            pos.z = -10f;
            transform.position = ClampToBounds(pos);
            _currentLookAhead = Vector3.zero;
            _lastTargetPos = target.position;
        }

        /// <summary>
        /// Temporarily pan and hold the camera on a world position, then resume following the player.
        /// Used by ConversationLogPanel to focus on a speaking entity.
        /// Multiple calls cancel the previous focus.
        /// </summary>
        public void FocusOnWorldPosition(Vector3 worldPos, float holdDuration = 1.5f)
        {
            // Cancel any existing focus coroutine
            if (_focusCoroutine != null)
                StopCoroutine(_focusCoroutine);

            Vector3 pos = worldPos;
            pos.z = -10f;
            _focusPosition = ClampToBounds(pos);
            _focusOverride = true;

            _focusCoroutine = StartCoroutine(FocusHoldCoroutine(holdDuration));
        }

        private IEnumerator FocusHoldCoroutine(float holdDuration)
        {
            yield return new WaitForSeconds(holdDuration);
            _focusOverride = false;
            _focusCoroutine = null;

            // Reset look-ahead so camera doesn't lurch when resuming player follow
            if (target != null)
                _lastTargetPos = target.position;
            _currentLookAhead = Vector3.zero;
        }
    }
}
