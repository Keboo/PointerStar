using PointerStar.Shared;

namespace PointerStar.Client.ViewModels;

public partial class VotingOptionsDialogViewModel : ViewModelBase
{
    [ObservableProperty]
    private List<string> _voteOptions = new(VotingPresets.Fibonacci);

    public bool IsValid => VoteOptions.Count > 0 && VoteOptions.All(opt => !string.IsNullOrWhiteSpace(opt));

    public void SelectPreset(string[] preset)
    {
        VoteOptions = new List<string>(preset);
    }

    public void AddOption()
    {
        VoteOptions.Add("");
        OnPropertyChanged(nameof(VoteOptions));
    }

    public void RemoveOption(int index)
    {
        if (VoteOptions.Count > 1 && index >= 0 && index < VoteOptions.Count)
        {
            VoteOptions.RemoveAt(index);
            OnPropertyChanged(nameof(VoteOptions));
        }
    }
}
