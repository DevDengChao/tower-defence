using System.Collections.Generic;
using Core.Game;
using Core.UI;
using TowerDefense.Game;
using UnityEngine;
using UnityEngine.UI;

namespace TowerDefense.UI
{
    /// <inheritdoc />
    /// <summary>
    /// A manager for the level select user interface
    /// </summary>
    public sealed class LevelSelectScreen : SimpleMainMenuPage
    {
        /// <summary>
        /// The button to instantiate that 
        /// represents the level select buttons
        /// </summary>
        public LevelSelectButton selectionPrefab;

        /// <summary>
        /// The layout group to instantiate the buttons in
        /// </summary>
        public LayoutGroup layout;

        /// <summary>
        /// A buffer for the levels panel
        /// </summary>
        public Transform rightBuffer;

        public Button backButton;

        public MouseScroll mouseScroll;

        public Animation cameraAnimator;

        public string enterCameraAnim;

        public string exitCameraAnim;

        /// <summary>
        /// The reference to the list of levels to display
        /// </summary>
        private LevelList _mLevelList;

        private readonly List<Button> _mButtons = new List<Button>();

        /// <summary>
        /// Instantiate the buttons
        /// </summary>
        private void Start()
        {
            if (GameManager.instance == null)
            {
                return;
            }

            _mLevelList = GameManager.instance.levelList;
            if (layout == null || selectionPrefab == null || _mLevelList == null)
            {
                return;
            }

            var amount = _mLevelList.Count;
            for (var i = 0; i < amount; i++)
            {
                var button = CreateButton(_mLevelList[i]);
                var buttonTransform = button.transform;
                buttonTransform.SetParent(layout.transform);
                buttonTransform.localScale = Vector3.one;
                _mButtons.Add(button.GetComponent<Button>());
            }

            if (rightBuffer != null)
            {
                rightBuffer.SetAsLastSibling();
            }

            for (var index = 1; index < _mButtons.Count - 1; index++)
            {
                var button = _mButtons[index];
                SetUpNavigation(button, _mButtons[index - 1], _mButtons[index + 1]);
            }

            SetUpNavigation(_mButtons[0], backButton, _mButtons[1]);
            SetUpNavigation(_mButtons[_mButtons.Count - 1], _mButtons[_mButtons.Count - 2], null);

            mouseScroll.SetHasRightBuffer(rightBuffer != null);
        }

        /// <summary>
        /// Create and Initialise a Level select button based on item
        /// </summary>
        /// <param name="item">
        /// The level data
        /// </param>
        /// <returns>
        /// The initialised button
        /// </returns>
        private LevelSelectButton CreateButton(LevelItem item)
        {
            var button = Instantiate(selectionPrefab);
            button.Initialize(item, mouseScroll);
            return button;
        }

        /// <inheritdoc />
        /// <summary>
        /// Play camera animations
        /// </summary>
        public override void Show()
        {
            base.Show();

            if (cameraAnimator != null && enterCameraAnim != null)
            {
                cameraAnimator.Play(enterCameraAnim);
            }
        }

        /// <inheritdoc />
        /// <summary>
        /// Return camera to normal position
        /// </summary>
        public override void Hide()
        {
            base.Hide();

            if (cameraAnimator != null && exitCameraAnim != null)
            {
                cameraAnimator.Play(exitCameraAnim);
            }
        }

        /// <summary>
        /// Sets up the navigation for a selectable
        /// </summary>
        /// <param name="selectable">Selectable to set up</param>
        /// <param name="left">Select on left</param>
        /// <param name="right">Select on right</param>
        private static void SetUpNavigation(Selectable selectable, Selectable left, Selectable right)
        {
            var navigation = selectable.navigation;
            navigation.selectOnLeft = left;
            navigation.selectOnRight = right;
            selectable.navigation = navigation;
        }
    }
}