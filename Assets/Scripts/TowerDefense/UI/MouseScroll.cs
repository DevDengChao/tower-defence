using UnityEngine;
using UnityEngine.UI;
using UnityInput = UnityEngine.Input;

namespace TowerDefense.UI
{
    /// <inheritdoc />
    /// <summary>
    /// Component to override ScrollRect, uses normalized mouse position inside ScrollRect to scroll
    /// </summary>
    [RequireComponent(typeof(ScrollRect))]
    public class MouseScroll : MonoBehaviour
    {
        /// <summary>
        /// If the normalized scroll position should be clamped between 0 & 1
        /// </summary>
        public bool clampScroll = true;

        /// <summary>
        /// Buffer to adjust ScrollRect size
        /// </summary>
        public float scrollXBuffer;

        public float scrollYBuffer;

        private ScrollRect _mScrollRect;
        private RectTransform _mScrollRectTransform;

        private bool _mOverrideScrolling,
            _mHasRightBuffer;

        public void SetHasRightBuffer(bool rightBuffer)
        {
            _mHasRightBuffer = rightBuffer;
        }

        /// <summary>
        /// If appropriate, we cache ScrollRect reference, disable it and enable scrolling override
        /// </summary>
        private void Start()
        {
#if UNITY_STANDALONE || UNITY_EDITOR
            _mScrollRect = GetComponent<ScrollRect>();
            _mScrollRect.enabled = false;
            _mOverrideScrolling = true;
            _mScrollRectTransform = (RectTransform) _mScrollRect.transform;
#else
			_mOverrideScrolling = false;
#endif
        }

        /// <summary>
        ///  Use normalized mouse position inside ScrollRect to scroll
        /// </summary>
        private void Update()
        {
            if (!_mOverrideScrolling)
            {
                return;
            }

            var mousePosition = UnityInput.mousePosition;

            // only scroll if mouse is inside ScrollRect
            var inside = RectTransformUtility.RectangleContainsScreenPoint(_mScrollRectTransform, mousePosition);
            if (!inside) return;

            var rect = _mScrollRectTransform.rect;
            var adjustmentX = rect.width * scrollXBuffer;
            var adjustmentY = rect.height * scrollYBuffer;

            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_mScrollRectTransform, mousePosition, null,
                out localPoint);

            var pivot = _mScrollRectTransform.pivot;
            var x = (localPoint.x + (rect.width - adjustmentX) * pivot.x) / (rect.width - 2 * adjustmentX);
            var y = (localPoint.y + (rect.height - adjustmentY) * pivot.y) / (rect.height - 2 * adjustmentY);

            if (clampScroll)
            {
                x = Mathf.Clamp01(x);
                y = Mathf.Clamp01(y);
            }

            _mScrollRect.normalizedPosition = new Vector2(x, y);
        }

        /// <summary>
        /// Called when a button inside the scroll is selected
        /// </summary>
        /// <param name="levelSelectButton">Selected child</param>
        public void SelectChild(LevelSelectButton levelSelectButton)
        {
            // minus one if  buffer
            var childCount = levelSelectButton.transform.parent.childCount - (_mHasRightBuffer ? 1 : 0);
            if (childCount <= 1) return;
            var normalized = (float) levelSelectButton.transform.GetSiblingIndex() / (childCount - 1);
            _mScrollRect.normalizedPosition = new Vector2(normalized, 0);
        }
    }
}