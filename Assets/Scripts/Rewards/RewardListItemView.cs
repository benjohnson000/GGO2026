using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ggo2026
{
    /// <summary>
    /// One row in the <see cref="RewardScreen"/>'s list: the reward's icon and title on a button. Clicking it asks the
    /// screen to claim the reward. Goes on the root of the RewardListItem prefab.
    /// </summary>
    public class RewardListItemView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text titleText;

        [Tooltip("Optional. Hidden when the reward has no icon.")]
        [SerializeField] private Image icon;

        private Action<RewardListItemView> clicked;

        public Reward Reward { get; private set; }

        /// <summary>Shows <paramref name="reward"/> and calls <paramref name="onClicked"/> with this row when it is clicked.</summary>
        public void Bind(Reward reward, Action<RewardListItemView> onClicked)
        {
            Reward = reward;
            clicked = onClicked;

            if (titleText != null) titleText.text = reward.Title;
            if (icon != null)
            {
                icon.sprite = reward.Icon;
                icon.gameObject.SetActive(reward.Icon != null);
            }
        }

        private void OnEnable()
        {
            if (button != null) button.onClick.AddListener(OnClicked);
        }

        private void OnDisable()
        {
            if (button != null) button.onClick.RemoveListener(OnClicked);
        }

        private void OnClicked()
        {
            clicked?.Invoke(this);
        }
    }
}
