using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

public class Register : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_InputField usernameTMPInput;
    [SerializeField] private TMP_InputField passwordTMPInput;

    private string baseUrl = "https://lelamhai-001-site1.ftempurl.com";
    private string register = "/api/ControllerUser/register";
    
    public void OnRegisterButtonPressed()
    {
        var username = usernameTMPInput != null ? usernameTMPInput.text : string.Empty;
        var password = passwordTMPInput != null ? passwordTMPInput.text : string.Empty;

        StartCoroutine(RegisterCoroutine(username, password));
    }

    private IEnumerator RegisterCoroutine(string username, string password)
    {
        // The API (Swagger) shows username/password are expected as query parameters.
        var url = string.Format("{0}{1}?username={2}&password={3}",
            baseUrl.TrimEnd('/'),
            register,
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

    public void OnLoginButtonPressed()
    {
        GameManager.Instance.HideAllPanels();
        GameManager.Instance.ShowLoginPanel();
    }
}
