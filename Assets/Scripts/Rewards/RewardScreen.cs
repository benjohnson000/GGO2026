using System;
using System.Collections.Generic;
using benjohnson;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ggo2026
{
    /// <summary>
    /// The list of rewards shown after a win, in the UI scene. <see cref="Show"/> fills the list with one
    /// <see cref="RewardListItemView"/> per reward; clicking a row claims its reward and removes the row. The button
    /// under the list reads "Skip" while anything is left and "Continue" once everything is collected; either way it
    /// closes the screen and calls back whoever opened it.
    /// </summary>
    public class RewardScreen : Singleton<RewardScreen>
    {
        [Tooltip("The panel shown and hidden, e.g. RewardsList. Hidden on Awake. Must not be this object, which needs to stay active.")]
        [SerializeField] private GameObject panel;

        [Tooltip("Parent for the reward rows, e.g. ListRoot (with a layout group).")]
        [SerializeField] private Transform listRoot;

        [SerializeField] private RewardListItemView itemPrefab;

        [Tooltip("Closes the screen. Reads Skip while rewards are left, Continue once all are collected.")]
        [SerializeField] private Button continueButton;

        [Tooltip("The continue button's label.")]
        [SerializeField] private TMP_Text continueLabel;

        [SerializeField] private string skipText = "Skip";
        [SerializeField] private string continueText = "Continue";

        [Tooltip("Optional. The pick-a-tile screen; must implement ITileChoicePresenter. Without it, tile rewards can't be collected.")]
        [SerializeField] private MonoBehaviour tileChoiceScreen;

        private readonly List<RewardListItemView> items = new List<RewardListItemView>();
        private readonly List<Reward> rewards = new List<Reward>();
        private Action finished;

        public bool IsOpen => panel != null && panel.activeSelf;

        /// <summary>True once every reward on the screen has been collected (also when there were none).</summary>
        public bool AllClaimed
        {
            get
            {
                foreach (Reward reward in rewards)
                {
                    if (!reward.IsClaimed) return false;
                }
                return true;
            }
        }

        protected override void Awake()
        {
            base.Awake();
            if (instance != this) return;

            if (panel == null || listRoot == null || itemPrefab == null || continueButton == null)
            {
                Debug.LogError($"{name}: RewardScreen needs Panel, List Root, Item Prefab and Continue Button assigned.", this);
            }
            if (panel == gameObject)
            {
                Debug.LogError($"{name}: Panel must be a child, not the RewardScreen object itself, or the screen can't reopen.", this);
            }
            if (tileChoiceScreen != null && !(tileChoiceScreen is ITileChoicePresenter))
            {
                Debug.LogError($"{name}: {tileChoiceScreen.GetType().Name} doesn't implement ITileChoicePresenter, so it is ignored.", this);
            }

            if (panel != null) panel.SetActive(false);
        }

        private void OnEnable()
        {
            if (continueButton != null) continueButton.onClick.AddListener(OnContinueClicked);
        }

        private void OnDisable()
        {
            if (continueButton != null) continueButton.onClick.RemoveListener(OnContinueClicked);
        }

        protected override void OnDestroy()
        {
            ClearItems();
            base.OnDestroy();
        }

        /// <summary>
        /// Opens the screen with <paramref name="newRewards"/> (null entries are skipped) and calls
        /// <paramref name="onFinished"/> when the player presses Skip or Continue. Replaces anything already showing.
        /// </summary>
        public void Show(IReadOnlyList<Reward> newRewards, Action onFinished)
        {
            ClearItems();
            finished = onFinished;

            if (newRewards != null)
            {
                foreach (Reward reward in newRewards)
                {
                    if (reward == null) continue;
                    rewards.Add(reward);
                    reward.Claimed += OnRewardClaimed;
                    if (itemPrefab == null || listRoot == null) continue;

                    RewardListItemView item = Instantiate(itemPrefab, listRoot);
                    item.Bind(reward, OnItemClicked);
                    items.Add(item);
                }
            }

            RefreshButton();
            if (panel != null) panel.SetActive(true);
        }

        /// <summary>Closes the screen without calling back. Uncollected rewards are lost.</summary>
        public void Hide()
        {
            if (panel != null) panel.SetActive(false);
            ClearItems();
            finished = null;
        }

        private void OnItemClicked(RewardListItemView item)
        {
            ITileChoicePresenter tileChoice = tileChoiceScreen as ITileChoicePresenter;
            item.Reward.Claim(new RewardClaimContext(CoinsView.instance, tileChoice, item.transform.position));
        }

        private void OnRewardClaimed(Reward reward)
        {
            for (int i = items.Count - 1; i >= 0; i--)
            {
                if (items[i].Reward != reward) continue;
                Destroy(items[i].gameObject);
                items.RemoveAt(i);
            }
            RefreshButton();
        }

        private void OnContinueClicked()
        {
            Action callback = finished;
            Hide();
            callback?.Invoke();
        }

        private void RefreshButton()
        {
            if (continueLabel != null) continueLabel.text = AllClaimed ? continueText : skipText;
        }

        private void ClearItems()
        {
            foreach (Reward reward in rewards)
            {
                reward.Claimed -= OnRewardClaimed;
            }
            rewards.Clear();

            foreach (RewardListItemView item in items)
            {
                if (item != null) Destroy(item.gameObject);
            }
            items.Clear();
        }
    }
}
