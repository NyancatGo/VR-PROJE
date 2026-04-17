using System;
using TrainingAnalytics;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class TabContentController : MonoBehaviour
    {
        [Header("Content Panels")]
        [SerializeField] private GameObject[] _contentRoots;
        
        [Header("Active Line Indicators")]
        [SerializeField] private GameObject[] _activeLines;
        
        [Header("Tab Buttons")]
        [SerializeField] private Button[] _tabButtons;

        [SerializeField] private ContentOpenTracker _contentTracker;
        
        [SerializeField] private int _activeIndex;

        private void Awake()
        {
            // Auto-find tab buttons if not assigned
            if (_tabButtons == null || _tabButtons.Length == 0)
            {
                _tabButtons = GetComponentsInChildren<Button>();
            }

            if (_contentTracker == null)
            {
                _contentTracker = GetComponent<ContentOpenTracker>();
                if (_contentTracker == null && Application.isPlaying)
                {
                    _contentTracker = gameObject.AddComponent<ContentOpenTracker>();
                }
            }
            
            // Subscribe to button events
            if (_tabButtons != null)
            {
                for (int i = 0; i < _tabButtons.Length; i++)
                {
                    int tabIndex = i;
                    _tabButtons[i].onClick.RemoveAllListeners();
                    _tabButtons[i].onClick.AddListener(() => SelectTab(tabIndex, true));
                }
            }
        }

        public void SelectTab(int index)
        {
            SelectTab(index, true);
        }

        private void SelectTab(int index, bool trackAnalytics)
        {
            if (_contentRoots == null || _contentRoots.Length == 0)
                return;

            index = Mathf.Clamp(index, 0, _contentRoots.Length - 1);
            _activeIndex = index;

            // Update content panels
            for (int i = 0; i < _contentRoots.Length; i++)
            {
                if (_contentRoots[i] != null)
                    _contentRoots[i].SetActive(i == _activeIndex);
            }

            // Update active line indicators
            if (_activeLines != null)
            {
                for (int i = 0; i < _activeLines.Length; i++)
                {
                    if (_activeLines[i] != null)
                        _activeLines[i].SetActive(i == _activeIndex);
                }
            }

            // Update button colors
            if (_tabButtons != null)
            {
                Color activeColor = new Color(0f, 0.8980392f, 1f, 0.12f);
                Color inactiveColor = new Color(0.0392156877f, 0.08627451f, 0.156862751f, 0.6f);
                
                for (int i = 0; i < _tabButtons.Length; i++)
                {
                    var colors = _tabButtons[i].colors;
                    colors.normalColor = (i == _activeIndex) ? activeColor : inactiveColor;
                    _tabButtons[i].colors = colors;
                }
            }

            if (trackAnalytics && _contentTracker != null)
            {
                _contentTracker.TrackSelection(_activeIndex, _contentRoots, _tabButtons);
            }
        }

        private void OnEnable()
        {
            SelectTab(_activeIndex, false);
        }

        private void OnValidate()
        {
            _activeIndex = Mathf.Max(0, _activeIndex);
        }
    }
}
