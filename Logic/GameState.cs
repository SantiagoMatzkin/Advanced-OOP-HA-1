using System;
using System.Collections.Generic;

namespace TerritoryExpansionGame.Logic;


public sealed class GameState
{
    private readonly int[,] _board;
    private readonly bool[] _hasMoved = new bool[3];

    public int Height { get; }

    public int Width { get; }

    public int CurrentPlayer { get; private set; }

    public GameOutcome Outcome { get; private set; }

    public bool IsGameOver
    {
        get
        {
            return Outcome != GameOutcome.Ongoing;
        }
    }

    public GameState(int height, int width)
    {
        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height), "Height must be a positive number.");
        }

        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Width must be a positive number.");
        }

        Height = height;
        Width = width;
        _board = new int[height, width];
        CurrentPlayer = 1;
        Outcome = GameOutcome.Ongoing;
    }

    private GameState(int[,] board, int currentPlayer, bool player1HasMoved, bool player2HasMoved)
    {
        _board = board;
        Height = board.GetLength(0);
        Width = board.GetLength(1);
        CurrentPlayer = currentPlayer;
        _hasMoved[1] = player1HasMoved;
        _hasMoved[2] = player2HasMoved;
        Outcome = GameOutcome.Ongoing;

        EvaluateLoadedState();
    }

    public int GetCellOwner(int row, int col)
    {
        EnsureInside(row, col);
        return _board[row, col];
    }

    public bool TryMakeMove(int row, int col, out string message)
    {
        if (IsGameOver)
        {
            message = "The game already ended. Start a new game or load a save file.";
            return false;
        }

        if (!IsInside(row, col))
        {
            message = "Move is outside of the board.";
            return false;
        }

        if (!IsLegalMove(CurrentPlayer, row, col))
        {
            message = BuildInvalidMoveMessage(CurrentPlayer, row, col);
            return false;
        }

        _board[row, col] = CurrentPlayer;
        _hasMoved[CurrentPlayer] = true;
        var playerWhoMoved = CurrentPlayer;

        if (IsBoardFull())
        {
            Outcome = GameOutcome.Draw;
            message = "Board is full. The game ended in a draw.";
            return true;
        }

        CurrentPlayer = GetOtherPlayer(CurrentPlayer);

        if (!HasAnyLegalMoves(CurrentPlayer))
        {
            Outcome = CurrentPlayer == 1 ? GameOutcome.Player2Wins : GameOutcome.Player1Wins;
            message = Outcome == GameOutcome.Player1Wins
                ? "Blue wins. Red has no legal moves left."
                : "Red wins. Blue has no legal moves left.";
            return true;
        }

        message = $"Player {playerWhoMoved} captured ({row + 1}, {col + 1}).";
        return true;
    }

    public bool IsLegalMove(int player, int row, int col)
    {
        if (player is < 1 or > 2)
        {
            throw new ArgumentOutOfRangeException(nameof(player), "Player must be either 1 or 2.");
        }

        if (!IsInside(row, col) || _board[row, col] != 0)
        {
            return false;
        }

        if (!_hasMoved[player])
        {
            return true;
        }

        for (var r = row - 1; r <= row + 1; r++)
        {
            for (var c = col - 1; c <= col + 1; c++)
            {
                if ((r == row && c == col) || !IsInside(r, c))
                {
                    continue;
                }

                if (_board[r, c] == player)
                {
                    return true;
                }
            }
        }

        return false;
    }

    public bool HasAnyLegalMoves(int player)
    {
        for (var row = 0; row < Height; row++)
        {
            for (var col = 0; col < Width; col++)
            {
                if (IsLegalMove(player, row, col))
                {
                    return true;
                }
            }
        }

        return false;
    }

    public bool IsBoardFull()
    {
        for (var row = 0; row < Height; row++)
        {
            for (var col = 0; col < Width; col++)
            {
                if (_board[row, col] == 0)
                {
                    return false;
                }
            }
        }

        return true;
    }

    public int CountTerritories(int player)
    {
        if (player is < 1 or > 2)
        {
            throw new ArgumentOutOfRangeException(nameof(player), "Player must be either 1 or 2.");
        }

        var count = 0;

        for (var row = 0; row < Height; row++)
        {
            for (var col = 0; col < Width; col++)
            {
                if (_board[row, col] == player)
                {
                    count++;
                }
            }
        }

        return count;
    }

    public IEnumerable<int> EnumerateFlattenedBoard()
    {
        for (var row = 0; row < Height; row++)
        {
            for (var col = 0; col < Width; col++)
            {
                yield return _board[row, col];
            }
        }
    }

    public static GameState FromFlattenedBoard(int height, int width, IReadOnlyList<int> values)
    {
        if (height <= 0)
        {
            throw new FormatException("Board height must be a positive integer.");
        }

        if (width <= 0)
        {
            throw new FormatException("Board width must be a positive integer.");
        }

        if (values.Count != height * width)
        {
            throw new FormatException("Cell count does not match board dimensions.");
        }

        var board = new int[height, width];
        var player1Count = 0;
        var player2Count = 0;

        var index = 0;

        for (var row = 0; row < height; row++)
        {
            for (var col = 0; col < width; col++)
            {
                var value = values[index++];

                if (value is < 0 or > 2)
                {
                    throw new FormatException("Board values must be 0, 1 or 2.");
                }

                board[row, col] = value;

                if (value == 1)
                {
                    player1Count++;
                }
                else if (value == 2)
                {
                    player2Count++;
                }
            }
        }

        int currentPlayer;

        if (player1Count == player2Count)
        {
            currentPlayer = 1;
        }
        else if (player1Count == player2Count + 1)
        {
            currentPlayer = 2;
        }
        else
        {
            throw new FormatException("The save file contains an invalid turn order.");
        }

        return new GameState(board, currentPlayer, player1Count > 0, player2Count > 0);
    }

    private void EvaluateLoadedState()
    {
        if (IsBoardFull())
        {
            Outcome = GameOutcome.Draw;
            return;
        }

        if (!HasAnyLegalMoves(CurrentPlayer))
        {
            Outcome = CurrentPlayer == 1 ? GameOutcome.Player2Wins : GameOutcome.Player1Wins;
        }
    }

    private bool IsInside(int row, int col)
    {
        return row >= 0 && row < Height && col >= 0 && col < Width;
    }

    private static int GetOtherPlayer(int player)
    {
        return player == 1 ? 2 : 1;
    }

    private string BuildInvalidMoveMessage(int player, int row, int col)
    {
        if (!IsInside(row, col))
        {
            return "Move is outside of the board.";
        }

        if (_board[row, col] != 0)
        {
            return "That tile is already occupied.";
        }

        if (!_hasMoved[player])
        {
            return "Your first move can be on any empty tile.";
        }

        return "Move must be adjacent (including diagonal) to one of your own tiles.";
    }

    private void EnsureInside(int row, int col)
    {
        if (!IsInside(row, col))
        {
            throw new ArgumentOutOfRangeException($"Cell ({row}, {col}) is outside the board bounds.");
        }
    }
}
