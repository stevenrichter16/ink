using UnityEngine;
using System.Text;

namespace InkSim
{
    /// <summary>
    /// Lightweight performance monitor - logs stats every few seconds.
    /// </summary>
    public class PerfMonitor : MonoBehaviour
    {
        public float logInterval = 3f;
        
        private float _timer;
        private int _frameCount;
        private float _worstMs;
        private float _totalTime;
        private long _lastMem;
        private int _gcSpikes;
        
        private static PerfMonitor _instance;
        
void Awake()
        {
            if (_instance != null) { Destroy(gameObject); return; }
            _instance = this;
            _lastMem = System.GC.GetTotalMemory(false);
            _timer = -2f; // Skip first 2 seconds (warmup/loading)
            
            // CAP FRAME RATE - lower = less CPU heat
            // 30 FPS is plenty for a turn-based game
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
            
            // Reduce physics update rate too
            Time.fixedDeltaTime = 0.05f; // 20 Hz instead of 50 Hz
            
            Debug.Log($"[PerfMonitor] Frame rate capped to 60 FPS, Physics at 20Hz. Editor={Application.isEditor}");
        }
        
void Update()
        {
            float dt = Time.unscaledDeltaTime;
            _timer += dt;
            
            // Skip during warmup
            if (_timer < 0) return;
            
            float ms = dt * 1000f;
            _frameCount++;
            _totalTime += dt;
            if (ms > _worstMs) _worstMs = ms;
            
            // Track GC spikes (>50KB jump)
            long mem = System.GC.GetTotalMemory(false);
            if (mem - _lastMem > 100 * 1024) _gcSpikes++; // 100KB threshold
            _lastMem = mem;
            
            if (_timer >= logInterval)
            {
                LogStats();
                _timer = 0; _frameCount = 0; _worstMs = 0; _totalTime = 0; _gcSpikes = 0;
            }
        }
        
void LogStats()
        {
            float avgFps = _frameCount / _totalTime;
            float avgMs = (_totalTime / _frameCount) * 1000f;
            long memKB = System.GC.GetTotalMemory(false) / 1024;
            
            int factions = FactionMember.ActiveMembers?.Count ?? 0;
            int pickups = ItemPickup.ActivePickups?.Count ?? 0;
            
            var sb = new StringBuilder();
            sb.Append($"[Perf] FPS:{avgFps:F0} Avg:{avgMs:F1}ms Worst:{_worstMs:F1}ms | ");
            sb.Append($"Mem:{memKB}KB GC:{_gcSpikes} | ");
            sb.Append($"Factions:{factions} Pickups:{pickups}");
            
            if (avgFps < 30) sb.Append(" LOW-FPS");
            if (_worstMs > 50) sb.Append(" HITCH");
            
            Debug.Log(sb.ToString());
        }
        
        void OnDestroy() { if (_instance == this) _instance = null; }
    }
}
