using System.Collections.Generic;
using System.Text;
using UnityEngine;
using TMPro;

public class ChessGameController : MonoBehaviour
{
    public static ChessGameController Instance { get; private set; }

    private enum PieceType { None, Pawn, Knight, Bishop, Rook, Queen, King }
    private enum PieceColor { White, Black }

    [Header("AI Settings")]
    [SerializeField] private bool _playerIsWhite = true;
    [SerializeField] private int _aiSearchDepth = 4;
    [SerializeField] private float _aiMoveDelay = 0.5f;

    private ChessAI _chessAI;
    private float _aiMoveTimer = 0f;
    private bool _waitingForAIMove = false;

    public int AISearchDepth {get => _aiSearchDepth; set => _aiSearchDepth = value;}

    private struct Piece
    {
        public PieceType Type;
        public PieceColor Color;
        public bool HasMoved;

        public bool IsEmpty => Type == PieceType.None;

        public static Piece Empty => new Piece { Type = PieceType.None };
    }

    [Header("Board")]
    [SerializeField] private float squareSize = 1f;
    [SerializeField] private Vector2 boardCenter = Vector2.zero;
    [SerializeField] private Color lightSquareColor = new Color(0.93f, 0.89f, 0.78f, 1f);
    [SerializeField] private Color darkSquareColor = new Color(0.37f, 0.47f, 0.27f, 1f);
    [SerializeField] private Color selectedSquareColor = new Color(0.95f, 0.83f, 0.25f, 1f);
    [SerializeField] private Color legalMoveColor = new Color(0.33f, 0.71f, 0.93f, 1f);

    [Header("Optional Board Sprite")]
    [SerializeField] private Sprite boardSprite;
    [SerializeField] private bool showBoardSprite = false;

    [Header("Piece Sprites")]
    [SerializeField] private Sprite whitePawn;
    [SerializeField] private Sprite whiteKnight;
    [SerializeField] private Sprite whiteBishop;
    [SerializeField] private Sprite whiteRook;
    [SerializeField] private Sprite whiteQueen;
    [SerializeField] private Sprite whiteKing;
    [SerializeField] private Sprite blackPawn;
    [SerializeField] private Sprite blackKnight;
    [SerializeField] private Sprite blackBishop;
    [SerializeField] private Sprite blackRook;
    [SerializeField] private Sprite blackQueen;
    [SerializeField] private Sprite blackKing;

    

    private Piece[,] _board = new Piece[8, 8];
    private ChessSquare[,] _squares = new ChessSquare[8, 8];
    private readonly Dictionary<string, int> _repetitionCounts = new Dictionary<string, int>();

    private readonly List<GameObject> _pieceViews = new List<GameObject>();

    private PieceColor _turn = PieceColor.White;
    private bool _hasSelection;
    private Vector2Int _selectedPos;
    private List<Vector2Int> _selectedLegalMoves = new List<Vector2Int>();

    private Transform _boardRoot;
    private Transform _piecesRoot;
    private Sprite _squareSprite;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        CreateRuntimeSquareSprite();
        BuildBoardVisuals();
        _chessAI = new ChessAI();
        // ResetGame();
    }

    public void ResetGame()
    {
        enabled = true;

        _waitingForAIMove = false;
        _aiMoveTimer = 0f;

        _turn = PieceColor.White;
        _hasSelection = false;
        _selectedLegalMoves.Clear();

        // Tính AI depth dựa trên Elo của user
        // CalculateAIDepthFromElo();

        SetupInitialBoard();
        _repetitionCounts.Clear();
        RegisterCurrentPosition();
        RefreshPieceViews();
        ClearHighlights();
        LogTurn();

        Debug.Log("AI Search Depth: " + _aiSearchDepth);
    }

    private void Update()
    {
        if (_waitingForAIMove)
        {
            _aiMoveTimer -= Time.deltaTime;
            if (_aiMoveTimer <= 0f)
            {
                ExecuteAIMove();
            }
        }
        else
        {
            HandleMouseInput();
        }
    }

    private void CreateRuntimeSquareSprite()
    {
        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        _squareSprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
    }

    private void BuildBoardVisuals()
    {
        _boardRoot = new GameObject("BoardRoot").transform;
        _boardRoot.SetParent(transform, false);

        _piecesRoot = new GameObject("PiecesRoot").transform;
        _piecesRoot.SetParent(transform, false);

        if (showBoardSprite && boardSprite != null)
        {
            GameObject board = new GameObject("BoardSprite");
            board.transform.SetParent(_boardRoot, false);
            board.transform.position = BoardToWorld(new Vector2Int(3, 3)) + new Vector3(squareSize * 0.5f, squareSize * 0.5f, 0f);

            var sr = board.AddComponent<SpriteRenderer>();
            sr.sprite = boardSprite;
            sr.sortingOrder = -2;

            Vector2 spriteSize = sr.sprite.bounds.size;
            board.transform.localScale = new Vector3((8f * squareSize) / spriteSize.x, (8f * squareSize) / spriteSize.y, 1f);
        }

        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                Vector2Int pos = new Vector2Int(x, y);
                GameObject squareObj = new GameObject($"Square_{x}_{y}");
                squareObj.transform.SetParent(_boardRoot, false);
                squareObj.transform.position = BoardToWorld(pos);

                SpriteRenderer sr = squareObj.AddComponent<SpriteRenderer>();
                sr.sprite = _squareSprite;
                sr.sortingOrder = -1;
                squareObj.transform.localScale = Vector3.one * squareSize;

                Color baseColor = ((x + y) % 2 == 0) ? lightSquareColor : darkSquareColor;
                if (showBoardSprite && boardSprite != null)
                {
                    baseColor = new Color(1f, 1f, 1f, 0.02f);
                }
                sr.color = baseColor;

                BoxCollider2D collider2D = squareObj.AddComponent<BoxCollider2D>();
                collider2D.size = Vector2.one;

                ChessSquare square = squareObj.AddComponent<ChessSquare>();
                square.Initialize(pos, sr, baseColor);

                _squares[x, y] = square;
            }
        }
    }

    private void SetupInitialBoard()
    {
        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                _board[x, y] = Piece.Empty;
            }
        }

        for (int x = 0; x < 8; x++)
        {
            _board[x, 1] = new Piece { Type = PieceType.Pawn, Color = PieceColor.White };
            _board[x, 6] = new Piece { Type = PieceType.Pawn, Color = PieceColor.Black };
        }

        SetBackRank(PieceColor.White, 0);
        SetBackRank(PieceColor.Black, 7);
    }

    private void SetBackRank(PieceColor color, int y)
    {
        _board[0, y] = new Piece { Type = PieceType.Rook, Color = color };
        _board[1, y] = new Piece { Type = PieceType.Knight, Color = color };
        _board[2, y] = new Piece { Type = PieceType.Bishop, Color = color };
        _board[3, y] = new Piece { Type = PieceType.Queen, Color = color };
        _board[4, y] = new Piece { Type = PieceType.King, Color = color };
        _board[5, y] = new Piece { Type = PieceType.Bishop, Color = color };
        _board[6, y] = new Piece { Type = PieceType.Knight, Color = color };
        _board[7, y] = new Piece { Type = PieceType.Rook, Color = color };
    }

    private void HandleMouseInput()
    {
        if (!Input.GetMouseButtonDown(0))
        {
            return;
        }

        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 mouse2D = new Vector2(mouseWorld.x, mouseWorld.y);

        RaycastHit2D hit = Physics2D.Raycast(mouse2D, Vector2.zero);
        if (!hit.collider)
        {
            return;
        }

        ChessSquare square = hit.collider.GetComponent<ChessSquare>();
        if (square == null)
        {
            return;
        }

        OnSquareClicked(square.BoardPosition);
    }

    private void OnSquareClicked(Vector2Int pos)
    {
        Piece clicked = _board[pos.x, pos.y];

        if (!_hasSelection)
        {
            if (!clicked.IsEmpty && clicked.Color == _turn)
            {
                SelectPiece(pos);
            }
            return;
        }

        if (pos == _selectedPos)
        {
            ClearSelection();
            return;
        }

        if (ContainsMove(_selectedLegalMoves, pos))
        {
            MovePiece(_selectedPos, pos);
            ClearSelection();
            EndTurnAndEvaluateState();
            return;
        }

        if (!clicked.IsEmpty && clicked.Color == _turn)
        {
            SelectPiece(pos);
        }
        else
        {
            ClearSelection();
        }
    }

    private void SelectPiece(Vector2Int pos)
    {
        _hasSelection = true;
        _selectedPos = pos;
        _selectedLegalMoves = GetLegalMoves(pos);
        HighlightSelection();
    }

    private void ClearSelection()
    {
        _hasSelection = false;
        _selectedLegalMoves.Clear();
        ClearHighlights();
    }

    private void HighlightSelection()
    {
        ClearHighlights();

        _squares[_selectedPos.x, _selectedPos.y].SetTint(selectedSquareColor);
        for (int i = 0; i < _selectedLegalMoves.Count; i++)
        {
            Vector2Int move = _selectedLegalMoves[i];
            _squares[move.x, move.y].SetTint(legalMoveColor);
        }
    }

    private void ClearHighlights()
    {
        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                _squares[x, y].ResetColor();
            }
        }
    }

    private void MovePiece(Vector2Int from, Vector2Int to)
    {
        Piece moving = _board[from.x, from.y];
        moving.HasMoved = true;

        // Auto-promotion to Queen
        if (moving.Type == PieceType.Pawn && (to.y == 7 || to.y == 0))
        {
            moving.Type = PieceType.Queen;
        }

        _board[to.x, to.y] = moving;
        _board[from.x, from.y] = Piece.Empty;

        RefreshPieceViews();
    }

    private void EndTurnAndEvaluateState()
    {
        _turn = Opponent(_turn);
        RegisterCurrentPosition();

        bool inCheck = IsKingInCheck(_turn, _board);
        bool hasMove = PlayerHasAnyLegalMove(_turn);

        if (!hasMove && inCheck)
        {
            enabled = false;
            if(Opponent(_turn) == PieceColor.White)
            {
                GameManager.Instance.HideAllPanels();
                GameManager.Instance.ShowWinPanel();
            }
            else
            {
                GameManager.Instance.HideAllPanels();
                GameManager.Instance.ShowLosePanel();
            }
            Debug.Log($"Checkmate! {Opponent(_turn)} wins.");
            return;
        }

        if (!hasMove)
        {
            Debug.Log("Stalemate! Draw.");
            enabled = false;
            GameManager.Instance.HideAllPanels();
            GameManager.Instance.ShowDrawPanel();
            return;
        }

        // Check insufficient material
        if (!HasSufficientMaterial(_board))
        {
            Debug.Log("Draw! Insufficient material.");
            enabled = false;
            GameManager.Instance.HideAllPanels();
            GameManager.Instance.ShowDrawPanel();
            return;
        }

        if (HasThreefoldRepetition())
        {
            Debug.Log("Draw! Threefold repetition.");
            enabled = false;
            GameManager.Instance.HideAllPanels();
            GameManager.Instance.ShowDrawPanel();
            return;
        }

        if (inCheck)
        {
            Debug.Log($"{_turn} king is in check.");
        }

        LogTurn();

        // Check if it's AI's turn
        bool isAITurn = (_playerIsWhite && _turn == PieceColor.Black) || (!_playerIsWhite && _turn == PieceColor.White);
        if (isAITurn)
        {
            _waitingForAIMove = true;
            _aiMoveTimer = _aiMoveDelay;
        }
    }

    private void LogTurn()
    {
        Debug.Log($"Turn: {_turn}");
    }

    /// <summary>
    /// Tính AI search depth dựa trên Elo của user
    /// Linear Mapping: Elo < 1000 (D2), 1000-1400 (D3), 1400-1800 (D4), > 1800 (D5+)
    /// </summary>
    public void CalculateAIDepthFromElo()
    {
        int userElo = UserManager.Instance.Elo;

        if (userElo < 1000)
        {
            _aiSearchDepth = 2; // Easy - AI yếu
        }
        else if (userElo < 1400)
        {
            _aiSearchDepth = 3; // Normal
        }
        else if (userElo < 1800)
        {
            _aiSearchDepth = 4; // Hard
        }
        else
        {
            _aiSearchDepth = 5; // Very Hard
        }

        Debug.Log($"User Elo: {userElo} → AI Depth: {_aiSearchDepth}");
    }

    private void ExecuteAIMove()
    {
        _waitingForAIMove = false;

        // Convert internal board to ChessAI format
        ChessAI.Piece[,] aiBoard = new ChessAI.Piece[8, 8];
        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                Piece piece = _board[x, y];
                aiBoard[x, y] = new ChessAI.Piece
                {
                    Type = (ChessAI.PieceType)piece.Type,
                    Color = (ChessAI.PieceColor)piece.Color,
                    HasMoved = piece.HasMoved
                };
            }
        }

        // Get AI move
        ChessAI.PieceColor aiColor = _playerIsWhite ? ChessAI.PieceColor.Black : ChessAI.PieceColor.White;
        AIMove aiMove = _chessAI.FindBestMove(aiBoard, aiColor, _aiSearchDepth);

        // Execute the move
        if (aiMove.From != aiMove.To)
        {
            MovePiece(aiMove.From, aiMove.To);
            ClearSelection();
            EndTurnAndEvaluateState();
        }
    }

    private bool PlayerHasAnyLegalMove(PieceColor color)
    {
        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                Piece piece = _board[x, y];
                if (!piece.IsEmpty && piece.Color == color)
                {
                    if (GetLegalMoves(new Vector2Int(x, y)).Count > 0)
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }

    private List<Vector2Int> GetLegalMoves(Vector2Int from)
    {
        Piece piece = _board[from.x, from.y];
        List<Vector2Int> pseudo = GetPseudoLegalMoves(from, piece, _board);
        List<Vector2Int> legal = new List<Vector2Int>();

        for (int i = 0; i < pseudo.Count; i++)
        {
            Vector2Int to = pseudo[i];
            Piece[,] simulated = CloneBoard(_board);
            ApplyMove(simulated, from, to);

            if (!IsKingInCheck(piece.Color, simulated))
            {
                legal.Add(to);
            }
        }

        return legal;
    }

    private List<Vector2Int> GetPseudoLegalMoves(Vector2Int from, Piece piece, Piece[,] state)
    {
        List<Vector2Int> moves = new List<Vector2Int>();
        if (piece.IsEmpty)
        {
            return moves;
        }

        switch (piece.Type)
        {
            case PieceType.Pawn:
                AddPawnMoves(from, piece.Color, state, moves);
                break;
            case PieceType.Knight:
                AddKnightMoves(from, piece.Color, state, moves);
                break;
            case PieceType.Bishop:
                AddSlidingMoves(from, piece.Color, state, moves, new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(-1, -1));
                break;
            case PieceType.Rook:
                AddSlidingMoves(from, piece.Color, state, moves, new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1));
                break;
            case PieceType.Queen:
                AddSlidingMoves(from, piece.Color, state, moves, new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(-1, -1), new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1));
                break;
            case PieceType.King:
                AddKingMoves(from, piece.Color, state, moves);
                break;
        }

        return moves;
    }

    private void AddPawnMoves(Vector2Int from, PieceColor color, Piece[,] state, List<Vector2Int> moves)
    {
        int dir = color == PieceColor.White ? 1 : -1;
        int startRow = color == PieceColor.White ? 1 : 6;

        Vector2Int oneStep = new Vector2Int(from.x, from.y + dir);
        if (IsInside(oneStep) && state[oneStep.x, oneStep.y].IsEmpty)
        {
            moves.Add(oneStep);

            Vector2Int twoStep = new Vector2Int(from.x, from.y + 2 * dir);
            if (from.y == startRow && IsInside(twoStep) && state[twoStep.x, twoStep.y].IsEmpty)
            {
                moves.Add(twoStep);
            }
        }

        Vector2Int captureLeft = new Vector2Int(from.x - 1, from.y + dir);
        Vector2Int captureRight = new Vector2Int(from.x + 1, from.y + dir);

        if (IsInside(captureLeft) && !state[captureLeft.x, captureLeft.y].IsEmpty && state[captureLeft.x, captureLeft.y].Color != color)
        {
            moves.Add(captureLeft);
        }
        if (IsInside(captureRight) && !state[captureRight.x, captureRight.y].IsEmpty && state[captureRight.x, captureRight.y].Color != color)
        {
            moves.Add(captureRight);
        }
    }

    private void AddKnightMoves(Vector2Int from, PieceColor color, Piece[,] state, List<Vector2Int> moves)
    {
        int[] dx = { 1, 2, 2, 1, -1, -2, -2, -1 };
        int[] dy = { 2, 1, -1, -2, -2, -1, 1, 2 };

        for (int i = 0; i < 8; i++)
        {
            Vector2Int p = new Vector2Int(from.x + dx[i], from.y + dy[i]);
            if (!IsInside(p))
            {
                continue;
            }

            Piece target = state[p.x, p.y];
            if (target.IsEmpty || target.Color != color)
            {
                moves.Add(p);
            }
        }
    }

    private void AddKingMoves(Vector2Int from, PieceColor color, Piece[,] state, List<Vector2Int> moves)
    {
        for (int y = -1; y <= 1; y++)
        {
            for (int x = -1; x <= 1; x++)
            {
                if (x == 0 && y == 0)
                {
                    continue;
                }

                Vector2Int p = new Vector2Int(from.x + x, from.y + y);
                if (!IsInside(p))
                {
                    continue;
                }

                Piece target = state[p.x, p.y];
                if (target.IsEmpty || target.Color != color)
                {
                    moves.Add(p);
                }
            }
        }
    }

    private void AddSlidingMoves(Vector2Int from, PieceColor color, Piece[,] state, List<Vector2Int> moves, params Vector2Int[] directions)
    {
        for (int i = 0; i < directions.Length; i++)
        {
            Vector2Int dir = directions[i];
            Vector2Int p = from + dir;

            while (IsInside(p))
            {
                Piece target = state[p.x, p.y];
                if (target.IsEmpty)
                {
                    moves.Add(p);
                }
                else
                {
                    if (target.Color != color)
                    {
                        moves.Add(p);
                    }
                    break;
                }

                p += dir;
            }
        }
    }

    private bool IsKingInCheck(PieceColor kingColor, Piece[,] state)
    {
        Vector2Int kingPos = new Vector2Int(-1, -1);

        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                Piece piece = state[x, y];
                if (!piece.IsEmpty && piece.Type == PieceType.King && piece.Color == kingColor)
                {
                    kingPos = new Vector2Int(x, y);
                    break;
                }
            }

            if (kingPos.x >= 0)
            {
                break;
            }
        }

        if (kingPos.x < 0)
        {
            return true;
        }

        PieceColor attacker = Opponent(kingColor);
        return IsSquareAttacked(kingPos, attacker, state);
    }

    private bool IsSquareAttacked(Vector2Int square, PieceColor byColor, Piece[,] state)
    {
        int pawnDir = byColor == PieceColor.White ? 1 : -1;
        Vector2Int pawnA = new Vector2Int(square.x - 1, square.y - pawnDir);
        Vector2Int pawnB = new Vector2Int(square.x + 1, square.y - pawnDir);

        if (IsInside(pawnA))
        {
            Piece p = state[pawnA.x, pawnA.y];
            if (!p.IsEmpty && p.Color == byColor && p.Type == PieceType.Pawn)
            {
                return true;
            }
        }

        if (IsInside(pawnB))
        {
            Piece p = state[pawnB.x, pawnB.y];
            if (!p.IsEmpty && p.Color == byColor && p.Type == PieceType.Pawn)
            {
                return true;
            }
        }

        int[] kx = { 1, 2, 2, 1, -1, -2, -2, -1 };
        int[] ky = { 2, 1, -1, -2, -2, -1, 1, 2 };
        for (int i = 0; i < 8; i++)
        {
            Vector2Int p = new Vector2Int(square.x + kx[i], square.y + ky[i]);
            if (!IsInside(p))
            {
                continue;
            }

            Piece attacker = state[p.x, p.y];
            if (!attacker.IsEmpty && attacker.Color == byColor && attacker.Type == PieceType.Knight)
            {
                return true;
            }
        }

        if (IsAttackedBySliding(square, byColor, state, PieceType.Bishop, PieceType.Queen, new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(-1, -1)))
        {
            return true;
        }

        if (IsAttackedBySliding(square, byColor, state, PieceType.Rook, PieceType.Queen, new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1)))
        {
            return true;
        }

        for (int y = -1; y <= 1; y++)
        {
            for (int x = -1; x <= 1; x++)
            {
                if (x == 0 && y == 0)
                {
                    continue;
                }

                Vector2Int p = new Vector2Int(square.x + x, square.y + y);
                if (!IsInside(p))
                {
                    continue;
                }

                Piece attacker = state[p.x, p.y];
                if (!attacker.IsEmpty && attacker.Color == byColor && attacker.Type == PieceType.King)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private bool IsAttackedBySliding(Vector2Int square, PieceColor byColor, Piece[,] state, PieceType directType, PieceType mixedType, params Vector2Int[] dirs)
    {
        for (int i = 0; i < dirs.Length; i++)
        {
            Vector2Int p = square + dirs[i];

            while (IsInside(p))
            {
                Piece piece = state[p.x, p.y];
                if (piece.IsEmpty)
                {
                    p += dirs[i];
                    continue;
                }

                if (piece.Color == byColor && (piece.Type == directType || piece.Type == mixedType))
                {
                    return true;
                }

                break;
            }
        }

        return false;
    }

    private void ApplyMove(Piece[,] state, Vector2Int from, Vector2Int to)
    {
        Piece moving = state[from.x, from.y];
        moving.HasMoved = true;

        if (moving.Type == PieceType.Pawn && (to.y == 7 || to.y == 0))
        {
            moving.Type = PieceType.Queen;
        }

        state[to.x, to.y] = moving;
        state[from.x, from.y] = Piece.Empty;
    }

    private Piece[,] CloneBoard(Piece[,] source)
    {
        Piece[,] clone = new Piece[8, 8];
        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                clone[x, y] = source[x, y];
            }
        }
        return clone;
    }

    private bool IsInside(Vector2Int p)
    {
        return p.x >= 0 && p.x < 8 && p.y >= 0 && p.y < 8;
    }

    private PieceColor Opponent(PieceColor c)
    {
        return c == PieceColor.White ? PieceColor.Black : PieceColor.White;
    }

    private void RegisterCurrentPosition()
    {
        string key = GetPositionKey(_turn);
        if (_repetitionCounts.ContainsKey(key))
        {
            _repetitionCounts[key]++;
        }
        else
        {
            _repetitionCounts[key] = 1;
        }
    }

    private bool HasThreefoldRepetition()
    {
        string key = GetPositionKey(_turn);
        return _repetitionCounts.TryGetValue(key, out int count) && count >= 3;
    }

    private string GetPositionKey(PieceColor sideToMove)
    {
        StringBuilder sb = new StringBuilder(256);
        sb.Append((int)sideToMove).Append('|');

        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                Piece piece = _board[x, y];
                sb.Append((int)piece.Type).Append(',')
                  .Append((int)piece.Color).Append(',')
                  .Append(piece.HasMoved ? 1 : 0).Append(';');
            }
        }

        return sb.ToString();
    }

    private bool HasSufficientMaterial(Piece[,] board)
    {
        // Insufficient material if neither side has: Pawn, Rook, or Queen
        // These are the only pieces that can deliver checkmate
        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                Piece piece = board[x, y];
                if (piece.IsEmpty)
                    continue;

                // If there's a Pawn, Rook, or Queen, there's sufficient material
                if (piece.Type == PieceType.Pawn || piece.Type == PieceType.Rook || piece.Type == PieceType.Queen)
                    return true;
            }
        }

        // Only kings, knights, and/or bishops remain - insufficient material
        return false;
    }

    private bool ContainsMove(List<Vector2Int> moves, Vector2Int target)
    {
        for (int i = 0; i < moves.Count; i++)
        {
            if (moves[i] == target)
            {
                return true;
            }
        }
        return false;
    }

    private void RefreshPieceViews()
    {
        for (int i = 0; i < _pieceViews.Count; i++)
        {
            if (_pieceViews[i] != null)
            {
                Destroy(_pieceViews[i]);
            }
        }
        _pieceViews.Clear();

        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                Piece piece = _board[x, y];
                if (piece.IsEmpty)
                {
                    continue;
                }

                Sprite sprite = GetSprite(piece);
                if (sprite == null)
                {
                    continue;
                }

                GameObject pieceObj = new GameObject($"Piece_{piece.Color}_{piece.Type}_{x}_{y}");
                pieceObj.transform.SetParent(_piecesRoot, false);
                pieceObj.transform.position = BoardToWorld(new Vector2Int(x, y));

                SpriteRenderer sr = pieceObj.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.sortingOrder = 5;

                float maxSide = Mathf.Max(sr.sprite.bounds.size.x, sr.sprite.bounds.size.y);
                if (maxSide > 0f)
                {
                    float scale = squareSize * 0.9f / maxSide;
                    pieceObj.transform.localScale = Vector3.one * scale;
                }

                _pieceViews.Add(pieceObj);
            }
        }
    }

    private Sprite GetSprite(Piece piece)
    {
        if (piece.Color == PieceColor.White)
        {
            switch (piece.Type)
            {
                case PieceType.Pawn: return whitePawn;
                case PieceType.Knight: return whiteKnight;
                case PieceType.Bishop: return whiteBishop;
                case PieceType.Rook: return whiteRook;
                case PieceType.Queen: return whiteQueen;
                case PieceType.King: return whiteKing;
            }
        }
        else
        {
            switch (piece.Type)
            {
                case PieceType.Pawn: return blackPawn;
                case PieceType.Knight: return blackKnight;
                case PieceType.Bishop: return blackBishop;
                case PieceType.Rook: return blackRook;
                case PieceType.Queen: return blackQueen;
                case PieceType.King: return blackKing;
            }
        }

        return null;
    }

    private Vector3 BoardToWorld(Vector2Int boardPos)
    {
        float left = boardCenter.x - (4f * squareSize) + (squareSize * 0.5f);
        float bottom = boardCenter.y - (4f * squareSize) + (squareSize * 0.5f);

        return new Vector3(left + boardPos.x * squareSize, bottom + boardPos.y * squareSize, 0f);
    }
}
