using MerryCountdown.Core.Navigation;
using MerryCountdown.Core.ViewModels;

namespace MerryCountdown.Views;

[NavigationInfo(NavigationSection.Settings)]
public partial class ParentZoneViewBase : ViewBase<ParentZoneViewModel> { }

public sealed partial class ParentZoneView : ParentZoneViewBase
{
    private const double LockedFeatureOpacity = 0.5;

    public ParentZoneView() => InitializeComponent();

    public static double LockedOpacity(bool isFull) => isFull ? 1 : LockedFeatureOpacity;

    public static object BoxDoor(int door) => door;

    public void SetMessageDoor(object value)
    {
        if (value is int door && ViewModel is not null)
        {
            ViewModel.MessageDoor = door;
        }
    }

    private void RemoveKid_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: KidRowViewModel kid })
        {
            ViewModel?.RemoveKidCommand.Execute(kid);
        }
    }

    private void RemovePhoto_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: PhotoRowViewModel photo })
        {
            ViewModel?.RemovePhotoCommand.Execute(photo);
        }
    }

    private void RemoveEvent_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: FamilyEventRowViewModel familyEvent })
        {
            ViewModel?.RemoveEventCommand.Execute(familyEvent);
        }
    }
}
