using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace TerritoryExpansionGame.ViewModels;

public sealed class BoardRowViewModel
{
    public ObservableCollection<CellViewModel> Cells { get; }

    public BoardRowViewModel(IEnumerable<CellViewModel> cells)
    {
        Cells = new ObservableCollection<CellViewModel>(cells);
    }
}
