using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject panelWin;
    [SerializeField] private GameObject panelLose;
    [SerializeField] private GameObject panelDraw;
    [SerializeField] private GameObject panelMenu;
    [SerializeField] private GameObject panelLogin;
    [SerializeField] private GameObject panelRegister;
    [SerializeField] private TMP_Text txtPlayer;
    [SerializeField] private TMP_Text txtAI;
    public static GameManager Instance { get; private set; }


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    public void HideAllPanels()
    {
        SetPanelActive(panelWin, false);
        SetPanelActive(panelLose, false);
        SetPanelActive(panelDraw, false);
        SetPanelActive(panelMenu, false);
        SetPanelActive(panelLogin, false);
        SetPanelActive(panelRegister, false);
    }

    public void ShowPanel(GameObject panel)
    {
        HideAllPanels();
        SetPanelActive(panel, true);
    }

    public void ShowWinPanel()
    {
        ShowPanel(panelWin);
    }

    public void ShowLosePanel()
    {
        ShowPanel(panelLose);
    }

    public void ShowDrawPanel()
    {
        ShowPanel(panelDraw);
    }

    public void ShowMenuPanel()
    {
        ShowPanel(panelMenu);
    }

    public void ShowLoginPanel()
    {
        ShowPanel(panelLogin);
    }

    public void ShowRegisterPanel()
    {
        ShowPanel(panelRegister);
    }

    public void HidePanel(GameObject panel)
    {
        SetPanelActive(panel, false);
    }

    private void SetPanelActive(GameObject panel, bool active)
    {
        if (panel != null)
        {
            panel.SetActive(active);
        }
    }

    
}
