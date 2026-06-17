using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

public class Login : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_InputField usernameTMPInput;
    [SerializeField] private TMP_InputField passwordTMPInput;


    private string baseUrl = "https://lelamhai-001-site1.ftempurl.com";
    private string login = "/api/ControllerUser/login";
    
    public void OnLoginButtonPressed()
    {
        var username = usernameTMPInput != null ? usernameTMPInput.text : string.Empty;
        var password = passwordTMPInput != null ? passwordTMPInput.text : string.Empty;

        StartCoroutine(LoginCoroutine(username, password));
    }

    private IEnumerator LoginCoroutine(string username, string password)
    {
        // The API (Swagger) shows username/password are expected as query parameters.
        var url = string.Format("{0}{1}?username={2}&password={3}",
            baseUrl.TrimEnd('/'),
            login,
            UnityWebRequest.EscapeURL(username),
            UnityWebRequest.EscapeURL(password));

        using (var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
        {
            request.downloadHandler = new DownloadHandlerBuffer();
            request.certificateHandler = new BypassCertificate();
            // no body required; server expects query params
            request.SetRequestHeader("Content-Type", "application/x-www-form-urlencoded");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Login successful: " + request.downloadHandler.text);
                GameManager.Instance.HideAllPanels();
                GameManager.Instance.ShowMenuPanel();
            }
            else
            {
                Debug.Log("Login failed: " + request.error + " - " + request.downloadHandler.text);
            }
        }
    }

    // Accept any certificate (useful for localhost). Do NOT use in production.
    private class BypassCertificate : CertificateHandler
    {
        protected override bool ValidateCertificate(byte[] certificateData)
        {
            return true;
        }
    }

    public void OnRegisterButtonPressed()
    {
        GameManager.Instance.HideAllPanels();
        GameManager.Instance.ShowRegisterPanel();
    }
}
