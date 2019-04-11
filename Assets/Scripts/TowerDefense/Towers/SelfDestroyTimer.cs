using Core.Utilities;
using UnityEngine;
using UnityEngine.Events;

namespace TowerDefense.Towers
{
    /// <inheritdoc />
    /// <summary>
    /// A helper component for self destruction
    /// </summary>
    public sealed class SelfDestroyTimer : MonoBehaviour
    {
        /// <summary>
        /// The time before destruction
        /// </summary>
        public float time = 5;

        /// <summary>
        /// The controlling timer
        /// </summary>
        private Timer _timer;

        /// <summary>
        /// The exposed death callback
        /// </summary>
        public UnityEvent death;

        /// <summary>
        /// Potentially initialize the time if necessary
        /// </summary>
        private void OnEnable()
        {
            if (_timer == null)
            {
                _timer = new Timer(time, OnTimeEnd);
            }
            else
            {
                _timer.Reset();
            }
        }

        /// <summary>
        /// Update the timer
        /// </summary>
        private void Update()
        {
            if (_timer == null) return;

            _timer.Tick(Time.deltaTime);
        }

        /// <summary>
        /// Fires at the end of timer
        /// </summary>
        private void OnTimeEnd()
        {
            death.Invoke();
            Poolable.TryPool(gameObject);
            _timer.Reset();
        }
    }
}