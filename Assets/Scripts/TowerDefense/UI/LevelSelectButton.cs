using Core.Game;
using TowerDefense.Game;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TowerDefense.UI
{
    /// <inheritdoc cref="MonoBehaviour" />
    /// <inheritdoc cref="ISelectHandler" />
    /// <summary>
    ///     The button for selecting a level
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class LevelSelectButton : MonoBehaviour, ISelectHandler
    {
        /// <summary>
        ///     Reference to the required button component
        /// </summary>
        private Button _mButton;

        /// <summary>
        ///     The data concerning the level this button displays
        /// </summary>
        private LevelItem _mItem;

        private MouseScroll _mMouseScroll;

        public Text description;

        public Sprite starAchieved;

        public Image[] stars;

        /// <summary>
        ///     The UI text element that displays the name of the level
        /// </summary>
        public Text titleDisplay;

        /// <inheritdoc cref="ISelectHandler.OnSelect" />
        /// <summary>
        ///     Implementation of ISelectHandler
        /// </summary>
        /// <param name="eventData">Select event data</param>
        public void OnSelect(BaseEventData eventData)
        {
            _mMouseScroll.SelectChild(this);
        }

        /// <summary>
        ///     A method for assigning the data from item to the button
        /// </summary>
        /// <param name="item">
        ///     The data with the information concerning the level
        /// </param>
        /// <param name="mouseScroll">
        ///     The MouseScroll component
        /// </param>
        public void Initialize(LevelItem item, MouseScroll mouseScroll)
        {
            LazyLoad();

            _mButton.onClick.AddListener(ChangeScenes);
            _mItem = item;
            titleDisplay.text = item.name;
            description.text = item.description;
            HasPlayedState();
            _mMouseScroll = mouseScroll;
        }

        /// <summary>
        ///     Configures the feedback concerning if the player has played
        /// </summary>
        private void HasPlayedState()
        {
            var gameManager = GameManager.Instance;
            if (gameManager == null) return;

            var starsForLevel = gameManager.GetStarsForLevel(_mItem.id);
            // override image sprite
            for (var i = 0; i < starsForLevel; i++) stars[i].sprite = starAchieved;
        }

        /// <summary>
        ///     Changes the scene to the scene name provided by m_Item
        /// </summary>
        private void ChangeScenes()
        {
            SceneManager.LoadScene(_mItem.sceneName);
        }

        /// <summary>
        ///     Ensure <see cref="_mButton" /> is not null
        /// </summary>
        private void LazyLoad()
        {
            if (_mButton == null) _mButton = GetComponent<Button>();
        }

        /// <summary>
        ///     Remove all listeners on the button before destruction
        /// </summary>
        protected void OnDestroy()
        {
            if (_mButton != null) _mButton.onClick.RemoveAllListeners();
        }
    }
}