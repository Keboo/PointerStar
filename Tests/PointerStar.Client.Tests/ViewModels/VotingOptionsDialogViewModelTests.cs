using PointerStar.Client.ViewModels;
using PointerStar.Shared;

namespace PointerStar.Client.Tests.ViewModels;

[ConstructorTests(typeof(VotingOptionsDialogViewModel))]
public partial class VotingOptionsDialogViewModelTests
{
    [Fact]
    public void VoteOptions_NewInstance_HasFibonacciDefault()
    {
        AutoMocker mocker = new();
        VotingOptionsDialogViewModel viewModel = mocker.CreateInstance<VotingOptionsDialogViewModel>();

        Assert.Equal(VotingPresets.Fibonacci, viewModel.VoteOptions);
    }

    [Fact]
    public void IsValid_WithValidOptions_ReturnsTrue()
    {
        AutoMocker mocker = new();
        VotingOptionsDialogViewModel viewModel = mocker.CreateInstance<VotingOptionsDialogViewModel>();
        viewModel.VoteOptions = new List<string> { "1", "2", "3" };

        Assert.True(viewModel.IsValid);
    }

    [Fact]
    public void IsValid_WithEmptyList_ReturnsFalse()
    {
        AutoMocker mocker = new();
        VotingOptionsDialogViewModel viewModel = mocker.CreateInstance<VotingOptionsDialogViewModel>();
        viewModel.VoteOptions = new List<string>();

        Assert.False(viewModel.IsValid);
    }

    [Fact]
    public void IsValid_WithEmptyString_ReturnsFalse()
    {
        AutoMocker mocker = new();
        VotingOptionsDialogViewModel viewModel = mocker.CreateInstance<VotingOptionsDialogViewModel>();
        viewModel.VoteOptions = new List<string> { "1", "", "3" };

        Assert.False(viewModel.IsValid);
    }

    [Fact]
    public void SelectPreset_WithPreset_UpdatesVoteOptions()
    {
        AutoMocker mocker = new();
        VotingOptionsDialogViewModel viewModel = mocker.CreateInstance<VotingOptionsDialogViewModel>();
        
        viewModel.SelectPreset(VotingPresets.TShirtSizes);

        Assert.Equal(VotingPresets.TShirtSizes, viewModel.VoteOptions);
    }

    [Fact]
    public void AddOption_AddsEmptyOption()
    {
        AutoMocker mocker = new();
        VotingOptionsDialogViewModel viewModel = mocker.CreateInstance<VotingOptionsDialogViewModel>();
        int initialCount = viewModel.VoteOptions.Count;

        viewModel.AddOption();

        Assert.Equal(initialCount + 1, viewModel.VoteOptions.Count);
        Assert.Equal("", viewModel.VoteOptions[^1]);
    }

    [Fact]
    public void RemoveOption_WithValidIndex_RemovesOption()
    {
        AutoMocker mocker = new();
        VotingOptionsDialogViewModel viewModel = mocker.CreateInstance<VotingOptionsDialogViewModel>();
        viewModel.VoteOptions = new List<string> { "1", "2", "3" };

        viewModel.RemoveOption(1);

        Assert.Equal(2, viewModel.VoteOptions.Count);
        Assert.DoesNotContain("2", viewModel.VoteOptions);
    }

    [Fact]
    public void RemoveOption_WithOnlyOneOption_DoesNotRemove()
    {
        AutoMocker mocker = new();
        VotingOptionsDialogViewModel viewModel = mocker.CreateInstance<VotingOptionsDialogViewModel>();
        viewModel.VoteOptions = new List<string> { "1" };

        viewModel.RemoveOption(0);

        Assert.Single(viewModel.VoteOptions);
    }

    [Fact]
    public void RemoveOption_WithInvalidIndex_DoesNothing()
    {
        AutoMocker mocker = new();
        VotingOptionsDialogViewModel viewModel = mocker.CreateInstance<VotingOptionsDialogViewModel>();
        viewModel.VoteOptions = new List<string> { "1", "2", "3" };

        viewModel.RemoveOption(10);

        Assert.Equal(3, viewModel.VoteOptions.Count);
    }

    [Fact]
    public void MoveOptionUp_WithValidIndex_MovesOption()
    {
        AutoMocker mocker = new();
        VotingOptionsDialogViewModel viewModel = mocker.CreateInstance<VotingOptionsDialogViewModel>();
        viewModel.VoteOptions = new List<string> { "1", "2", "3" };

        viewModel.MoveOptionUp(1);

        Assert.Equal("2", viewModel.VoteOptions[0]);
        Assert.Equal("1", viewModel.VoteOptions[1]);
        Assert.Equal("3", viewModel.VoteOptions[2]);
    }

    [Fact]
    public void MoveOptionUp_WithFirstIndex_DoesNothing()
    {
        AutoMocker mocker = new();
        VotingOptionsDialogViewModel viewModel = mocker.CreateInstance<VotingOptionsDialogViewModel>();
        viewModel.VoteOptions = new List<string> { "1", "2", "3" };

        viewModel.MoveOptionUp(0);

        Assert.Equal("1", viewModel.VoteOptions[0]);
        Assert.Equal("2", viewModel.VoteOptions[1]);
        Assert.Equal("3", viewModel.VoteOptions[2]);
    }

    [Fact]
    public void MoveOptionDown_WithValidIndex_MovesOption()
    {
        AutoMocker mocker = new();
        VotingOptionsDialogViewModel viewModel = mocker.CreateInstance<VotingOptionsDialogViewModel>();
        viewModel.VoteOptions = new List<string> { "1", "2", "3" };

        viewModel.MoveOptionDown(1);

        Assert.Equal("1", viewModel.VoteOptions[0]);
        Assert.Equal("3", viewModel.VoteOptions[1]);
        Assert.Equal("2", viewModel.VoteOptions[2]);
    }

    [Fact]
    public void MoveOptionDown_WithLastIndex_DoesNothing()
    {
        AutoMocker mocker = new();
        VotingOptionsDialogViewModel viewModel = mocker.CreateInstance<VotingOptionsDialogViewModel>();
        viewModel.VoteOptions = new List<string> { "1", "2", "3" };

        viewModel.MoveOptionDown(2);

        Assert.Equal("1", viewModel.VoteOptions[0]);
        Assert.Equal("2", viewModel.VoteOptions[1]);
        Assert.Equal("3", viewModel.VoteOptions[2]);
    }

    [Fact]
    public void MoveOptionUp_WithInvalidIndex_DoesNothing()
    {
        AutoMocker mocker = new();
        VotingOptionsDialogViewModel viewModel = mocker.CreateInstance<VotingOptionsDialogViewModel>();
        viewModel.VoteOptions = new List<string> { "1", "2", "3" };

        viewModel.MoveOptionUp(10);

        Assert.Equal(3, viewModel.VoteOptions.Count);
        Assert.Equal("1", viewModel.VoteOptions[0]);
    }

    [Fact]
    public void MoveOptionDown_WithInvalidIndex_DoesNothing()
    {
        AutoMocker mocker = new();
        VotingOptionsDialogViewModel viewModel = mocker.CreateInstance<VotingOptionsDialogViewModel>();
        viewModel.VoteOptions = new List<string> { "1", "2", "3" };

        viewModel.MoveOptionDown(10);

        Assert.Equal(3, viewModel.VoteOptions.Count);
        Assert.Equal("3", viewModel.VoteOptions[2]);
    }
}
