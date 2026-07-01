using System.Collections.Generic;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

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
    [SerializeField] private TMP_Text txtUserName;
    [SerializeField] private TMP_Text txtUserElo;

    [Header("Server")]
    [SerializeField] private string baseUrl = "https://localhost:7131";
    [SerializeField] private string updateEloEndpoint = "/api/ControllerUser/update";

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

    private void Start()
    {
        UserManager.Instance.LoadUserData();
        UpdateUserInfoDisplay();
    }

    public void UpdateUserInfoDisplay()
    {
        UserData user = UserManager.Instance.GetCurrentUser();
        if (user != null)
        {
            if (txtUserName != null)
            {
                txtUserName.text = user.userName;
            }
            if (txtUserElo != null)
            {
                txtUserElo.text = "Điểm Elo: " +user.elo.ToString();
            }
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
        int point = int.Parse(txtPlayer.text);
        txtPlayer.text = (point + 1).ToString();
        ShowPanel(panelWin);

        UpdateUserEloAfterMatch(10);
    }

    public void ContinueGame()
    {        
        ChessGameController.Instance.ResetGame();
        HideAllPanels();
    }

    public void Home()
    {
        HideAllPanels();
        ShowMenuPanel();
    }

    public void ShowLosePanel()
    {
        int point = int.Parse(txtAI.text);
        txtAI.text = (point + 1).ToString();
        ShowPanel(panelLose);

        UpdateUserEloAfterMatch(-10);
    }

    public void ShowDrawPanel()
    {
        ShowPanel(panelDraw);

        UpdateUserEloAfterMatch(0);
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

    private void UpdateUserEloAfterMatch(int delta)
    {
        if (UserManager.Instance == null)
        {
            return;
        }

        UserData user = UserManager.Instance.GetCurrentUser();
        if (user == null || string.IsNullOrEmpty(user.userName))
        {
            return;
        }

        int newElo = Mathf.Max(0, user.elo + delta);
        StartCoroutine(UpdateEloCoroutine(user.userName, newElo));
    }

    private IEnumerator UpdateEloCoroutine(string username, int newElo)
    {
        string url = string.Format("{0}{1}?username={2}&elo={3}",
            baseUrl.TrimEnd('/'),
            updateEloEndpoint,
            UnityWebRequest.EscapeURL(username),
            newElo);

        using (var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPUT))
        {
            request.downloadHandler = new DownloadHandlerBuffer();
            request.certificateHandler = new BypassCertificate();
            request.SetRequestHeader("Content-Type", "application/x-www-form-urlencoded");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                UserManager.Instance.UpdateCurrentUserElo(newElo);
                UpdateUserInfoDisplay();
                Debug.Log("Update elo successful: " + request.downloadHandler.text);
            }
            else
            {
                Debug.LogError("Update elo failed: " + request.error + " - " + request.downloadHandler.text);
            }
        }
    }

    private class BypassCertificate : CertificateHandler
    {
        protected override bool ValidateCertificate(byte[] certificateData)
        {
            return true;
        }
    }
}
