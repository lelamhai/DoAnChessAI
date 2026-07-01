using UnityEngine;
using System;

// Class để deserialize JSON response từ server
[System.Serializable]
public class UserData
{
    public string userName;
    public int elo;
}

[System.Serializable]
public class LoginResponse
{
    public string message;
    public UserData user;
}

// Manager để lưu và quản lý thông tin user
public class UserManager : MonoBehaviour
{
    public static UserManager Instance { get; private set; }

    private UserData _currentUser;

    public string UserName => _currentUser?.userName;
    public int Elo => _currentUser?.elo ?? 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Lưu thông tin user khi login thành công
    /// </summary>
    public void SaveUserData(string jsonResponse)
    {
        try
        {
            LoginResponse response = JsonUtility.FromJson<LoginResponse>(jsonResponse);
            if (response?.user != null)
            {
                _currentUser = response.user;
                
                // Lưu vào PlayerPrefs
                PlayerPrefs.SetString("UserName", _currentUser.userName);
                PlayerPrefs.SetInt("UserElo", _currentUser.elo);
                PlayerPrefs.Save();
                
                Debug.Log($"User data saved: {_currentUser.userName}, Elo: {_currentUser.elo}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to parse user data: {e.Message}");
        }
    }

    /// <summary>
    /// Tải thông tin user từ PlayerPrefs
    /// </summary>
    public void LoadUserData()
    {
        if (PlayerPrefs.HasKey("UserName"))
        {
            _currentUser = new UserData
            {
                userName = PlayerPrefs.GetString("UserName"),
                elo = PlayerPrefs.GetInt("UserElo", 0)
            };
            Debug.Log($"User data loaded: {_currentUser.userName}, Elo: {_currentUser.elo}");
        }
    }

    /// <summary>
    /// Xóa thông tin user (logout)
    /// </summary>
    public void ClearUserData()
    {
        _currentUser = null;
        PlayerPrefs.DeleteKey("UserName");
        PlayerPrefs.DeleteKey("UserElo");
        PlayerPrefs.Save();
        Debug.Log("User data cleared");
    }

    /// <summary>
    /// Cập nhật Elo hiện tại của user và lưu lại vào máy
    /// </summary>
    public void UpdateCurrentUserElo(int newElo)
    {
        if (_currentUser == null)
        {
            return;
        }

        _currentUser.elo = newElo;
        PlayerPrefs.SetInt("UserElo", _currentUser.elo);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Lấy user data hiện tại
    /// </summary>
    public UserData GetCurrentUser()
    {
        return _currentUser;
    }
}
