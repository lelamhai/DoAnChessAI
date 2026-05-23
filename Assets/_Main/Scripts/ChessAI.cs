using System.Collections.Generic;
using UnityEngine;

// Public wrapper for AI move
public struct AIMove
{
    public Vector2Int From;
    public Vector2Int To;

    public AIMove(Vector2Int from, Vector2Int to)
    {
        From = from;
        To = to;
    }
}

public class ChessAI
{
    public enum PieceType { None, Pawn, Knight, Bishop, Rook, Queen, King }
    public enum PieceColor { White, Black }

    public struct Piece
    {
        public PieceType Type;
        public PieceColor Color;
        public bool HasMoved;

        public bool IsEmpty => Type == PieceType.None;

        public static Piece Empty => new Piece { Type = PieceType.None };
    }

    private struct Move
    {
        public Vector2Int From;
        public Vector2Int To;

        public Move(Vector2Int from, Vector2Int to)
        {
            From = from;
            To = to;
        }
    }

    private struct EvaluationScore
    {
        public Move BestMove;
        public int Score;
        public int NodesEvaluated;
    }

    private const int CHECKMATE_SCORE = 100000;
    private const int STALEMATE_SCORE = 0;

    // Piece value scores
    private readonly int[] _pieceValues = new int[(int)PieceType.King + 1];

    // Transposition table for memoization
    private Dictionary<string, int> _transpositionTable = new Dictionary<string, int>();
    private int _nodesEvaluated = 0;

    public ChessAI()
    {
        InitializePieceValues();
    }

    private void InitializePieceValues()
    {
        _pieceValues[(int)PieceType.None] = 0;
        _pieceValues[(int)PieceType.Pawn] = 100;
        _pieceValues[(int)PieceType.Knight] = 320;
        _pieceValues[(int)PieceType.Bishop] = 330;
        _pieceValues[(int)PieceType.Rook] = 500;
        _pieceValues[(int)PieceType.Queen] = 900;
        _pieceValues[(int)PieceType.King] = 20000;
    }

    /// <summary>
    /// Find best move using NegaMax with Alpha-Beta Pruning
    /// </summary>
    public AIMove FindBestMove(Piece[,] board, PieceColor aiColor, int depth = 4)
    {
        _nodesEvaluated = 0;
        _transpositionTable.Clear();

        Move bestMove = new Move(Vector2Int.zero, Vector2Int.zero);
        int bestScore = int.MinValue;

        // Generate all legal moves for AI
        List<Move> legalMoves = GetAllLegalMoves(board, aiColor);

        if (legalMoves.Count == 0)
        {
            // No legal moves - should not happen in normal gameplay
            return new AIMove(bestMove.From, bestMove.To);
        }

        // Sort moves by heuristic for better alpha-beta pruning
        SortMovesByHeuristic(board, legalMoves, aiColor);

        int alpha = int.MinValue;
        int beta = int.MaxValue;

        for (int i = 0; i < legalMoves.Count; i++)
        {
            Move move = legalMoves[i];
            Piece[,] simulated = CloneBoard(board);
            ApplyMove(simulated, move);

            int score = -NegaMax(simulated, depth - 1, -beta, -alpha, OpponentColor(aiColor));

            if (score > bestScore)
            {
                bestScore = score;
                bestMove = move;
                alpha = Mathf.Max(alpha, score);
            }
        }

        Debug.Log($"AI evaluated {_nodesEvaluated} nodes. Best move: {bestMove.From} -> {bestMove.To}, Score: {bestScore}");

        return new AIMove(bestMove.From, bestMove.To);
    }

    private int NegaMax(Piece[,] board, int depth, int alpha, int beta, PieceColor color)
    {
        _nodesEvaluated++;

        // Terminal node evaluation
        if (depth == 0)
        {
            return EvaluatePosition(board, color);
        }

        // Check for checkmate/stalemate
        bool inCheck = IsKingInCheck(color, board);
        List<Move> legalMoves = GetAllLegalMoves(board, color);

        if (legalMoves.Count == 0)
        {
            if (inCheck)
            {
                return -CHECKMATE_SCORE + (4 - depth); // Prefer earlier checkmate
            }
            else
            {
                return STALEMATE_SCORE;
            }
        }

        // Sort moves for better alpha-beta pruning
        SortMovesByHeuristic(board, legalMoves, color);

        int maxScore = int.MinValue;

        for (int i = 0; i < legalMoves.Count; i++)
        {
            Move move = legalMoves[i];
            Piece[,] simulated = CloneBoard(board);
            ApplyMove(simulated, move);

            int score = -NegaMax(simulated, depth - 1, -beta, -alpha, OpponentColor(color));

            maxScore = Mathf.Max(maxScore, score);
            alpha = Mathf.Max(alpha, score);

            // Beta cutoff
            if (alpha >= beta)
            {
                break;
            }
        }

        return maxScore;
    }

    private int EvaluatePosition(Piece[,] board, PieceColor perspective)
    {
        int score = 0;

        // Material count
        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                Piece piece = board[x, y];
                if (!piece.IsEmpty)
                {
                    int value = _pieceValues[(int)piece.Type];
                    if (piece.Color == perspective)
                    {
                        score += value;
                    }
                    else
                    {
                        score -= value;
                    }
                }
            }
        }

        // Position bonuses
        score += EvaluatePiecePositions(board, perspective);

        return score;
    }

    private int EvaluatePiecePositions(Piece[,] board, PieceColor perspective)
    {
        int score = 0;

        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                Piece piece = board[x, y];
                if (piece.IsEmpty)
                {
                    continue;
                }

                int bonus = 0;
                if (piece.Type == PieceType.Pawn)
                {
                    // Pawns advance closer to promotion
                    int distance = perspective == PieceColor.White ? y : 7 - y;
                    bonus = distance * 5;

                    // Central pawns are better
                    if (x >= 2 && x <= 5)
                    {
                        bonus += 10;
                    }
                }
                else if (piece.Type == PieceType.Knight || piece.Type == PieceType.Bishop)
                {
                    // Centralize minor pieces
                    float centerDistance = Mathf.Abs(x - 3.5f) + Mathf.Abs(y - 3.5f);
                    bonus = (int)(14 - centerDistance) * 2;
                }
                else if (piece.Type == PieceType.Rook)
                {
                    // Rooks on open files are good
                    int pieceCount = 0;
                    for (int ty = 0; ty < 8; ty++)
                    {
                        if (!board[x, ty].IsEmpty)
                        {
                            pieceCount++;
                        }
                    }
                    if (pieceCount <= 2) // Only rook on file
                    {
                        bonus = 15;
                    }
                }

                if (piece.Color == perspective)
                {
                    score += bonus;
                }
                else
                {
                    score -= bonus;
                }
            }
        }

        return score;
    }

    private void SortMovesByHeuristic(Piece[,] board, List<Move> moves, PieceColor color)
    {
        // Simple heuristic: captures and checks first
        moves.Sort((a, b) =>
        {
            int scoreA = GetMoveScore(board, a, color);
            int scoreB = GetMoveScore(board, b, color);
            return scoreB.CompareTo(scoreA); // Higher score first
        });
    }

    private int GetMoveScore(Piece[,] board, Move move, PieceColor color)
    {
        int score = 0;

        Piece target = board[move.To.x, move.To.y];
        if (!target.IsEmpty)
        {
            // Capture is good
            score += _pieceValues[(int)target.Type] * 10;

            // Prioritize capturing high-value pieces
            Piece moving = board[move.From.x, move.From.y];
            if (_pieceValues[(int)moving.Type] < _pieceValues[(int)target.Type])
            {
                score += 5000; // Good trade
            }
        }

        // Check for checks
        Piece[,] simulated = CloneBoard(board);
        ApplyMove(simulated, move);
        if (IsKingInCheck(OpponentColor(color), simulated))
        {
            score += 200;
        }

        return score;
    }

    private List<Move> GetAllLegalMoves(Piece[,] board, PieceColor color)
    {
        List<Move> legalMoves = new List<Move>();

        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                Piece piece = board[x, y];
                if (piece.IsEmpty || piece.Color != color)
                {
                    continue;
                }

                Vector2Int from = new Vector2Int(x, y);
                List<Vector2Int> pseudo = GetPseudoLegalMoves(from, piece, board);

                for (int i = 0; i < pseudo.Count; i++)
                {
                    Vector2Int to = pseudo[i];
                    Piece[,] simulated = CloneBoard(board);
                    ApplyMove(simulated, from, to);

                    if (!IsKingInCheck(color, simulated))
                    {
                        legalMoves.Add(new Move(from, to));
                    }
                }
            }
        }

        return legalMoves;
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

        PieceColor attacker = OpponentColor(kingColor);
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

    private void ApplyMove(Piece[,] state, Move move)
    {
        ApplyMove(state, move.From, move.To);
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

    private PieceColor OpponentColor(PieceColor c)
    {
        return c == PieceColor.White ? PieceColor.Black : PieceColor.White;
    }
}
