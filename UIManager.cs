using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;




public class UIManager : MonoBehaviour
{
    public GameObject menuPanel;
    private bool isMenuOpen = false;

    public void OpenMenu()
    {
        menuPanel.SetActive(true);
        isMenuOpen = true;
        Time.timeScale = 0f;
    }

    public void CloseMenu()
    {
        menuPanel.SetActive(false);
        isMenuOpen = false;
        Time.timeScale = 1f;
    }

    private void Start()
    {
        menuPanel.SetActive(false);
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleMenu();
        }
    }
    public void ToggleMenu()
    {
        isMenuOpen = !isMenuOpen;
        menuPanel.SetActive(isMenuOpen);
        Time.timeScale = isMenuOpen ? 0f : 1f;
    }
}


