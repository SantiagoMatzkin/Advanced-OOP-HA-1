using System;
using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TerritoryExpansionGame.Logic;
using TerritoryExpansionGame.Data;

namespace TerritoryExpansionGame.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private const int DefaultBoardHeight = 6;
    private const int DefaultBoardWidth = 6;
    private const int MaxBoardSize = 40;

    private GameState _gameState;

    [ObservableProperty]
    private string _boardHeightText = DefaultBoardHeight.ToString();

    [ObservableProperty]
    private string _boardWidthText = DefaultBoardWidth.ToString();

    [ObservableProperty]
    private string _savePath = "game.save.txt";

    [ObservableProperty]
    private string _turnText = string.Empty;

    [ObservableProperty]
    private string _scoreText = string.Empty;

    [ObservableProperty]
    private string _statusText = string.Empty;

    public ObservableCollection<BoardRowViewModel> BoardRows { get; } = new();

    public IRelayCommand NewGameCommand { get; }

    public IRelayCommand SaveGameCommand { get; }

    public IRelayCommand LoadGameCommand { get; }

    public MainWindowViewModel()
    {
        _gameState = new GameState(DefaultBoardHeight, DefaultBoardWidth);

        NewGameCommand = new RelayCommand(StartNewGame);
        SaveGameCommand = new RelayCommand(SaveGame);
        LoadGameCommand = new RelayCommand(LoadGame);

        BuildBoardRows();
        RefreshUi("New game started.");
    }

    private void StartNewGame()
    {
        if (!TryReadBoardDimensions(out var height, out var width))
        {
            return;
        }

        _gameState = new GameState(height, width);
        BuildBoardRows();
        RefreshUi($"Started a new {height}x{width} game.");
    }

    private void SaveGame()
    {
        try
        {
            var normalizedPath = NormalizeSavePath(SavePath);
            SavePath = normalizedPath;
            SaveFileService.Save(normalizedPath, _gameState);
            RefreshUi($"Game saved to '{Path.GetFullPath(normalizedPath)}'.");
        }
        catch (Exception ex)
        {
            RefreshUi($"Save failed: {ex.Message}");
        }
    }

    private void LoadGame()
    {
        var normalizedPath = NormalizeSavePath(SavePath);
        SavePath = normalizedPath;

        try
        {
            _gameState = SaveFileService.Load(normalizedPath);

            BoardHeightText = _gameState.Height.ToString();
            BoardWidthText = _gameState.Width.ToString();

            BuildBoardRows();
            RefreshUi($"Game loaded from '{Path.GetFullPath(normalizedPath)}'.");
        }
        catch (FileNotFoundException)
        {
            // Recreate a missing save file from the current in-memory game state.
            SaveFileService.Save(normalizedPath, _gameState);
            RefreshUi($"Save file was missing and has been recreated at '{Path.GetFullPath(normalizedPath)}'.");
        }
        catch (Exception ex)
        {
            RefreshUi($"Load failed: {ex.Message}");
        }
    }

    private void HandleCellClick(int row, int col)
    {
        _gameState.TryMakeMove(row, col, out var message);
        RefreshUi(message);
    }

    private void BuildBoardRows()
    {
        BoardRows.Clear();

        for (var row = 0; row < _gameState.Height; row++)
        {
            var cells = new CellViewModel[_gameState.Width];

            for (var col = 0; col < _gameState.Width; col++)
            {
                cells[col] = new CellViewModel(row, col, HandleCellClick);
            }

            BoardRows.Add(new BoardRowViewModel(cells));
        }
    }

    private void RefreshUi(string message)
    {
        StatusText = message;
        ScoreText = $"Blue: {_gameState.CountTerritories(1)}   Red: {_gameState.CountTerritories(2)}";

        if (_gameState.Outcome == GameOutcome.Ongoing)
        {
            if (_gameState.CurrentPlayer == 1)
            {
                TurnText = "Blue to move";
            }
            else
            {
                TurnText = "Red to move";
            }
        }
        else if (_gameState.Outcome == GameOutcome.Draw)
        {
            TurnText = "Draw: board is full.";
        }
        else if (_gameState.Outcome == GameOutcome.Player1Wins)
        {
            TurnText = "Blue wins: Red has no legal moves.";
        }
        else if (_gameState.Outcome == GameOutcome.Player2Wins)
        {
            TurnText = "Red wins: Blue has no legal moves.";
        }
        else
        {
            TurnText = string.Empty;
        }

        UpdateCellVisuals();
    }

    private void UpdateCellVisuals()
    {
        var isGameOver = _gameState.IsGameOver;
        var activePlayer = _gameState.CurrentPlayer;

        for (var row = 0; row < _gameState.Height; row++)
        {
            var rowVm = BoardRows[row];

            for (var col = 0; col < _gameState.Width; col++)
            {
                var owner = _gameState.GetCellOwner(row, col);
                var isLegalMove = !isGameOver && _gameState.IsLegalMove(activePlayer, row, col);
                rowVm.Cells[col].UpdateVisualState(owner, isLegalMove, isGameOver);
            }
        }
    }

    private bool TryReadBoardDimensions(out int height, out int width)
    {
        height = 0;
        width = 0;

        if (!int.TryParse(BoardHeightText, out height) || height < 1 || height > MaxBoardSize)
        {
            RefreshUi($"Height must be an integer between 1 and {MaxBoardSize}.");
            return false;
        }

        if (!int.TryParse(BoardWidthText, out width) || width < 1 || width > MaxBoardSize)
        {
            RefreshUi($"Width must be an integer between 1 and {MaxBoardSize}.");
            return false;
        }

        return true;
    }

    private static string NormalizeSavePath(string rawPath)
    {
        var value = string.IsNullOrWhiteSpace(rawPath) ? "game.save.txt" : rawPath.Trim();

        if (!value.EndsWith(".save.txt", StringComparison.OrdinalIgnoreCase))
        {
            value += ".save.txt";
        }

        return value;
    }
}
