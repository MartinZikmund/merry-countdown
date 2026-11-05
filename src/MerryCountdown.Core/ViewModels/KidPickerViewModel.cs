using System.Collections.ObjectModel;
using MerryCountdown.Core.Family;

namespace MerryCountdown.Core.ViewModels;

/// <summary>"Whose turn is it?" Every kid has their own doors and wish list.</summary>
public partial class KidPickerViewModel : ObservableObject
{
    private readonly IFamilyService _family;
    private readonly IStringLocalizer _localizer;

    public KidPickerViewModel(IFamilyService family, IStringLocalizer localizer)
    {
        _family = family;
        _localizer = localizer;
        _family.Changed += (_, _) => Rebuild();
        Rebuild();
    }

    public ObservableCollection<KidChipViewModel> Kids { get; } = [];

    [ObservableProperty]
    public partial bool HasSeveralKids { get; set; }

    private void Rebuild()
    {
        var kids = _family.Kids;
        var unchanged = Kids.Count == kids.Count
            && Kids.Zip(kids).All(p => p.First.Kid == p.Second && p.First.Name == DisplayName(p.Second) && p.First.Sticker == p.Second.Sticker);

        if (!unchanged)
        {
            Kids.Clear();
            foreach (var kid in kids)
            {
                Kids.Add(new KidChipViewModel(kid, DisplayName(kid), _family));
            }
        }

        foreach (var chip in Kids)
        {
            chip.IsActive = chip.Kid == _family.ActiveKid;
        }

        HasSeveralKids = kids.Count > 1;
    }

    private string DisplayName(Kid kid) => kid.Name.Length > 0 ? kid.Name : _localizer["KidNoName"];
}

public partial class KidChipViewModel(Kid kid, string name, IFamilyService family) : ObservableObject
{
    public Kid Kid { get; } = kid;

    public string Name { get; } = name;

    public string Sticker => Kid.Sticker;

    [ObservableProperty]
    public partial bool IsActive { get; set; }

    [RelayCommand]
    private void Select() => family.SetActiveKid(Kid);
}
