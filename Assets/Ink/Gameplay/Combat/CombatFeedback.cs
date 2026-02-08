using UnityEngine;
using System.Collections;

namespace InkSim
{
    /// <summary>
    /// Combat juice: hit-pause (brief time-scale freeze) and camera shake on impact.
    /// Called by CombatResolver after a successful hit lands.
    /// </summary>
    public static class CombatFeedback
    {
        /// <summary>Duration of the hit-pause freeze-frame in seconds.</summary>
        public const float HitPauseDurationSec = 0.06f;

        /// <summary>Camera shake displacement magnitude (world units).</summary>
        public const float ShakeIntensity = 0.08f;

        /// <summary>Camera shake total duration in seconds.</summary>
        public const float ShakeDurationSec = 0.1f;

        /// <summary>
        /// Fire hit-pause and camera shake. Safe to call from anywhere —
        /// finds the main camera and runs coroutines on a persistent helper.
        /// </summary>
        public static void Play()
        {
            var helper = CombatFeedbackRunner.Instance;
            if (helper == null) return;

            helper.StartCoroutine(HitPauseRoutine());
            helper.StartCoroutine(CameraShakeRoutine());
        }

        private static IEnumerator HitPauseRoutine()
        {
            Time.timeScale = 0f;
            yield return new WaitForSecondsRealtime(HitPauseDurationSec);
            Time.timeScale = 1f;
        }

        private static IEnumerator CameraShakeRoutine()
        {
            var cam = Camera.main;
            if (cam == null) yield break;

            Vector3 originalPos = cam.transform.localPosition;
            float elapsed = 0f;

            while (elapsed < ShakeDurationSec)
            {
                float x = Random.Range(-ShakeIntensity, ShakeIntensity);
                float y = Random.Range(-ShakeIntensity, ShakeIntensity);
                cam.transform.localPosition = originalPos + new Vector3(x, y, 0f);
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            cam.transform.localPosition = originalPos;
        }
    }

    /// <summary>
    /// Persistent MonoBehaviour to host CombatFeedback coroutines.
    /// Auto-creates itself on first access.
    /// </summary>
    public class CombatFeedbackRunner : MonoBehaviour
    {
        public static CombatFeedbackRunner Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            if (Instance != null) return;
            var go = new GameObject("[CombatFeedbackRunner]");
            Object.DontDestroyOnLoad(go);
            Instance = go.AddComponent<CombatFeedbackRunner>();
        }
    }
}
