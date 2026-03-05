using System;
using Avalonia;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace TerritoryExpansionGame.ViewModels;

public partial class CellViewModel : ViewModelBase
{
    private static readonly IBrush EmptyColor = new SolidColorBrush(Color.Parse("#DCE3EC"));
    private static readonly IBrush Player1Color = new SolidColorBrush(Color.Parse("#3A62FF"));
    private static readonly IBrush Player2Color = new SolidColorBrush(Color.Parse("#FF2D3A"));
    private static readonly IBrush DefaultBorderColor = new SolidColorBrush(Color.Parse("#64748B"));
    private static readonly IBrush LegalMoveBorderColor = new SolidColorBrush(Color.Parse("#0F172A"));

    private readonly Action<int, int> _onClick;

    public int Row { get; }

    public int Column { get; }

    [ObservableProperty]
    private IBrush _background = EmptyColor;

    [ObservableProperty]
    private IBrush _borderBrush = DefaultBorderColor;

    [ObservableProperty]
    private Thickness _borderThickness = new(1);

    [ObservableProperty]
    private bool _isInteractable;

    public IRelayCommand ClickCommand { get; }

    public CellViewModel(int row, int column, Action<int, int> onClick)
    {
        Row = row;
        Column = column;
        _onClick = onClick;
        ClickCommand = new RelayCommand(HandleClick);
    }

    public void UpdateVisualState(int owner, bool isLegalMove, bool isGameOver)
    {
        if (owner == 1)
        {
            Background = Player1Color;
        }
        else if (owner == 2)
        {
            Background = Player2Color;
        }
        else
        {
            Background = EmptyColor;
        }

        BorderBrush = isLegalMove && owner == 0 && !isGameOver
            ? LegalMoveBorderColor
            : DefaultBorderColor;

        BorderThickness = isLegalMove && owner == 0 && !isGameOver
            ? new Thickness(2)
            : new Thickness(1);

        IsInteractable = owner == 0 && isLegalMove && !isGameOver;
    }

    private void HandleClick()
    {
        _onClick(Row, Column);
    }
}
