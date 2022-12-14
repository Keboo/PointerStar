namespace PointerStar.Client.ViewModels;

public abstract partial class ViewModelBase : ObservableObject
{
    public virtual async Task OnInitializedAsync()
        => await Loaded().ConfigureAwait(true);

    protected virtual void NotifyStateChanged() => OnPropertyChanged("");

    [RelayCommand]
    public virtual Task Loaded()
        => Task.CompletedTask;
}
