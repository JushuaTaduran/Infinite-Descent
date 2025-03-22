using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChangePanelButton : MonoBehaviour
{
    [SerializeField] private GameObject _currentPanel;
    [SerializeField] private GameObject _goToPanel;
    public void ChangePanel()
    {
        if (_currentPanel != null)
        {
            _currentPanel.SetActive(false);
        }

        if (_goToPanel != null)
        {
            _goToPanel.SetActive(true);
        }
    }
}