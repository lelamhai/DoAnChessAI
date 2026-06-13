using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Menu : MonoBehaviour
{
    public void EasyMode()
    {
        ChessGameController.Instance.AISearchDepth = 2;
        ChessGameController.Instance.ResetGame();
        GameManager.Instance.HideAllPanels();
    }

    public void MediumMode()
    {
        ChessGameController.Instance.AISearchDepth = 3;
        ChessGameController.Instance.ResetGame();
        GameManager.Instance.HideAllPanels();
    }

    public void HardMode()
    {
        ChessGameController.Instance.AISearchDepth = 5;
        ChessGameController.Instance.ResetGame();
        GameManager.Instance.HideAllPanels();
    }
}
