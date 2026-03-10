using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Othello.Contract;

namespace Othello.AI.AB;

public class NegaMaxAB : IOthelloAI
{
    public string Name => "NegamaxAB";

    public async Task<Move?> GetMoveAsync(BoardState board, DiscColor yourColor, CancellationToken ct)
    {
        await Task.Delay(new System.Random().Next(100, 1000), ct);

        var validMoves = GetValidMoves(board, yourColor);
        if (validMoves.Count == 0) return null;

        Move bestMove = validMoves[0];
        int bestScore = int.MinValue;
        int Depth = 5;

        foreach (var move in validMoves)
        {
            var newBoard = ApplyMove(board, move, yourColor);

            int score = -NegaMax(newBoard, Depth, int.MinValue, int.MaxValue, Opposite(yourColor));

            if (score > bestScore)
            {
                bestScore = score;
                bestMove = move;
            }
        }

        return bestMove;
    }

    private int NegaMax(BoardState board, int depth, int alpha, int beta, DiscColor color)
    {
        if (depth == 0)
            return Score(board, color);

        var moves = GetValidMoves(board, color);

        if (moves.Count == 0)
            return Score(board, color);

        int bestScore = int.MinValue;

        foreach (var move in moves)
        {
            var newBoard = ApplyMove(board, move, color);

            int score = -NegaMax(newBoard, depth - 1, -beta, -alpha, Opposite(color));

            bestScore = Math.Max(bestScore, score);
            alpha = Math.Max(alpha, score);

            if (alpha >= beta)
                break;
        }
        return bestScore;
    }

    private int Score(BoardState board, DiscColor color)
    {
        int myScore = 0;
        int opponentScore = 0;

        DiscColor opponent = Opposite(color);

        for (int r = 0; r < 8; r++)
        {
            for (int c = 0; c < 8; c++)
            {
                if (board.Grid[r, c] == color)
                    myScore++;
                else if (board.Grid[r, c] == opponent)
                    opponentScore++;
            }
        }

        // Corners +score
        int[,] corners = { { 0, 0 }, { 0, 7 }, { 7, 0 }, { 7, 7 } };

        for (int i = 0; i < 4; i++)
        {
            int r = corners[i, 0];
            int c = corners[i, 1];

            if (board.Grid[r, c] == color)
                myScore += 30;
            else if (board.Grid[r, c] == opponent)
                opponentScore += 30;
        }

        // Edges +score
        for (int i = 1; i < 7; i++)
        {
            // top
            if (board.Grid[0, i] == color) myScore += 5;
            else if (board.Grid[0, i] == opponent) opponentScore += 5;

            // bottom
            if (board.Grid[7, i] == color) myScore += 5;
            else if (board.Grid[7, i] == opponent) opponentScore += 5;

            // left
            if (board.Grid[i, 0] == color) myScore += 5;
            else if (board.Grid[i, 0] == opponent) opponentScore += 5;

            // right
            if (board.Grid[i, 7] == color) myScore += 5;
            else if (board.Grid[i, 7] == opponent) opponentScore += 5;
        }

        // X and C squares  -score
        int[,] xcSquares = 
        { 
            { 1, 1 }, { 1, 6 }, { 6, 1 }, { 6, 6 }, // X Squares
            { 0, 1 }, { 1, 0 }, // C Squares 
            { 0, 6 }, { 1, 7 }, // C Squares
            { 6, 0 }, { 7, 1 }, // C Squares
            { 6, 7 }, { 7, 6 }  // C Squares 
        };

        for (int i = 0; i < 12; i++)
        {
            int r = xcSquares[i, 0];
            int c = xcSquares[i, 1];

            if (board.Grid[r, c] == color)
                myScore -= 10;
            else if (board.Grid[r, c] == opponent)
                opponentScore -= 10;
        }
       
        return myScore - opponentScore;
    }

    private DiscColor Opposite(DiscColor color)
    {
        return color == DiscColor.Black ? DiscColor.White : DiscColor.Black;
    }

    private BoardState ApplyMove(BoardState board, Move move, DiscColor color)
    {
        var newBoard = board.Clone();

        newBoard.Grid[move.Row, move.Column] = color;

        int[] dr = { -1, -1, -1, 0, 0, 1, 1, 1 };
        int[] dc = { -1, 0, 1, -1, 1, -1, 0, 1 };

        DiscColor opponent = Opposite(color);

        for (int i = 0; i < 8; i++)
        {
            int r = move.Row + dr[i];
            int c = move.Column + dc[i];

            List<(int, int)> captured = new();

            while (r >= 0 && r < 8 && c >= 0 && c < 8 && newBoard.Grid[r, c] == opponent)
            {
                captured.Add((r, c));
                r += dr[i];
                c += dc[i];
            }

            if (r >= 0 && r < 8 && c >= 0 && c < 8 && newBoard.Grid[r, c] == color)
            {
                foreach (var (cr, cc) in captured)
                    newBoard.Grid[cr, cc] = color;
            }
        }

        return newBoard;
    }
    private List<Move> GetValidMoves(BoardState board, DiscColor color)
    {
        var moves = new List<Move>();
        for (int r = 0; r < 8; r++)
        {
            for (int c = 0; c < 8; c++)
            {
                if (IsValidMove(board, new Move(r, c), color))
                {
                    moves.Add(new Move(r, c));
                }
            }
        }
        return moves;
    }

    private bool IsValidMove(BoardState board, Move move, DiscColor color)
    {
        if (board.Grid[move.Row, move.Column] != DiscColor.None) return false;
        
        int[] dr = { -1, -1, -1, 0, 0, 1, 1, 1 };
        int[] dc = { -1, 0, 1, -1, 1, -1, 0, 1 };
        DiscColor opponent = color == DiscColor.Black ? DiscColor.White : DiscColor.Black;

        for (int i = 0; i < 8; i++)
        {
            int r = move.Row + dr[i];
            int c = move.Column + dc[i];
            int count = 0;

            while (r >= 0 && r < 8 && c >= 0 && c < 8 && board.Grid[r, c] == opponent)
            {
                r += dr[i];
                c += dc[i];
                count++;
            }

            if (r >= 0 && r < 8 && c >= 0 && c < 8 && board.Grid[r, c] == color && count > 0)
            {
                return true;
            }
        }
        return false;
    }
}
