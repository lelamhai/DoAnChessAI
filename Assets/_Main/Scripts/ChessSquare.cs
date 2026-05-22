using UnityEngine;

public class ChessSquare : MonoBehaviour
{
    public Vector2Int BoardPosition { get; private set; }

    private SpriteRenderer _renderer;
    private Color _baseColor;

    public void Initialize(Vector2Int boardPosition, SpriteRenderer renderer, Color baseColor)
    {
        BoardPosition = boardPosition;
        _renderer = renderer;
        _baseColor = baseColor;
        _renderer.color = _baseColor;
    }

    public void SetBaseColor(Color color)
    {
        _baseColor = color;
        if (_renderer != null)
        {
            _renderer.color = _baseColor;
        }
    }

    public void SetTint(Color color)
    {
        if (_renderer != null)
        {
            _renderer.color = color;
        }
    }

    public void ResetColor()
    {
        if (_renderer != null)
        {
            _renderer.color = _baseColor;
        }
    }
}
