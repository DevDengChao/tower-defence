using UnityEngine;
using UnityEngine.UI;

namespace TowerDefense.UI
{
    /// <summary>
    ///     A button for exiting the game
    /// </summary>
    public class ExitButton : Button
    {
        protected ExitButton()
        {
            onClick.AddListener(Application.Quit);
        }

        /// <summary>
        ///     Disable this button on mobile platforms
        /// </summary>
        protected override void Awake()
        {
            base.Awake();
#if UNITY_ANDROID || UNITY_IOS
            if (Application.isPlaying) gameObject.SetActive(false);
#endif
        }
    }
}