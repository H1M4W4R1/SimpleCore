using UnityEngine;

namespace Systems.SimpleCore.Timing
{
    /// <summary>
    ///     Global tick system
    /// </summary>
    public sealed class TickSystem : MonoBehaviour
    {
        private static TickSystem _instance;
        
        public delegate void TickHandler(float deltaTimeSeconds);
        
        /// <summary>
        ///     Timer for tick interval
        /// </summary>
        private float _tickTimer;

        /// <summary>
        ///     Tick interval, if less or equal to 0, ticks every frame
        /// </summary>
        public float TickInterval { get; set; } = 0f;

        /// <summary>
        ///     Disable time passing
        /// </summary>
        public bool CanTimePass { get; set; } = true;
        
        /// <summary>
        ///     If true tick will be executed automatically in update
        /// </summary>
        public bool AutomaticTick { get; set; } = true;
        
        /// <summary>
        ///     Registered handlers will be called every frame or every turn.
        /// </summary>
        public static event TickHandler OnTick;
        
        private void Awake()
        {
            // Prevent from being destroyed on scene change
            DontDestroyOnLoad(gameObject);
            _instance = this;
        }

        private void Update()
        {
            if (!AutomaticTick) return;
            HandleTick();
        }
        
        internal void HandleTick()
        {
            float timePassedSeconds = Time.deltaTime;

            // Skip if time cannot pass
            if (!CanTimePass) return;

            if (TickInterval <= 0f)
                OnTick?.Invoke(timePassedSeconds);
            else
            {
                _tickTimer += timePassedSeconds;

                // Handle interval passed, skip if tick cannot be performed
                // execute for all ticks that completed on this frame
                while (_tickTimer >= TickInterval) 
                    OnTick?.Invoke(timePassedSeconds);
            }
        }

        public static void EnsureExists()
        {
            if (_instance) return;

            _instance = FindAnyObjectByType<TickSystem>(FindObjectsInactive.Include);

            if (_instance)
            {
                _instance.enabled = true;
                _instance.gameObject.SetActive(true);
                return;
            }
            
            _instance = new GameObject("TickSystem").AddComponent<TickSystem>();
        }
    }
}