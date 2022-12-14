namespace PointerStar.Client.ViewModels;

public partial class CounterViewModel : ViewModelBase
{
    [ObservableProperty]
    private int _currentCount;
    
    public void IncrementCount()
    {
        CurrentCount++;
    }
}
