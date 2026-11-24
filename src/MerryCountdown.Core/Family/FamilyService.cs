using System.Globalization;

namespace MerryCountdown.Core.Family;

public interface IFamilyService
{
    IReadOnlyList<Kid> Kids { get; }

    Kid ActiveKid { get; }

    FamilySettings Settings { get; }

    IReadOnlyList<FamilyEvent> Events { get; }

    event EventHandler? Changed;

    Kid AddKid(string name);

    bool RemoveKid(Kid kid);

    void RenameKid(Kid kid, string name);

    void SetKidSticker(Kid kid, string sticker);

    void SetActiveKid(Kid kid);

    Wish? AddWish(string text, string? sticker);

    void RemoveWish(Wish wish);

    /// <summary>Puts a removed wish back at <paramref name="index"/>, or last if the list got shorter.</summary>
    void RestoreWish(Wish wish, int index);

    bool SendLetter(int year);

    void ReopenLetter();

    void OpenDoor(Kid kid, int year, int door);

    bool IsDoorOpened(Kid kid, int year, int door);

    void SetDoorMessage(Kid kid, int door, string? message);

    FamilyEvent AddEvent(string name, int month, int day, string sticker);

    void RemoveEvent(FamilyEvent familyEvent);

    /// <summary>Saves and notifies after a change to <see cref="Settings"/>.</summary>
    void SaveSettings();
}

public sealed class FamilyService : IFamilyService
{
    private readonly IFamilyStore _store;
    private readonly FamilyData _data;

    public FamilyService(IFamilyStore store, string cultureName)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _data = store.Load();
        _data.Settings ??= FamilySettings.ForCulture(cultureName);

        if (_data.Kids.Count == 0)
        {
            _data.Kids.Add(new Kid());
        }
    }

    public FamilyService(IFamilyStore store)
        : this(store, CultureInfo.CurrentUICulture.Name)
    {
    }

    public IReadOnlyList<Kid> Kids => _data.Kids;

    public Kid ActiveKid => _data.Kids.FirstOrDefault(k => k.Id == _data.ActiveKidId) ?? _data.Kids[0];

    public FamilySettings Settings => _data.Settings!;

    public IReadOnlyList<FamilyEvent> Events => _data.Events;

    public bool IsFullVersionCached
    {
        get => _data.IsFullVersionCached;
        set
        {
            _data.IsFullVersionCached = value;
            _store.Save(_data);
        }
    }

    public event EventHandler? Changed;

    public Kid AddKid(string name)
    {
        Kid kid = new() { Name = name.Trim(), Sticker = NextSticker() };
        _data.Kids.Add(kid);
        Commit();
        return kid;
    }

    public bool RemoveKid(Kid kid)
    {
        if (_data.Kids.Count <= 1 || !_data.Kids.Remove(kid))
        {
            return false;
        }

        Commit();
        return true;
    }

    public void RenameKid(Kid kid, string name)
    {
        kid.Name = name.Trim();
        Commit();
    }

    public void SetKidSticker(Kid kid, string sticker)
    {
        kid.Sticker = sticker;
        Commit();
    }

    public void SetActiveKid(Kid kid)
    {
        _data.ActiveKidId = kid.Id;
        Commit();
    }

    public Wish? AddWish(string text, string? sticker)
    {
        text = text.Trim();
        if (text.Length == 0 && string.IsNullOrEmpty(sticker))
        {
            return null;
        }

        Wish wish = new() { Text = text, Sticker = sticker };
        ActiveKid.Wishes.Add(wish);
        ActiveKid.LetterSentYear = null;
        Commit();
        return wish;
    }

    public void RemoveWish(Wish wish)
    {
        ActiveKid.Wishes.Remove(wish);
        Commit();
    }

    public void RestoreWish(Wish wish, int index)
    {
        ActiveKid.Wishes.Insert(Math.Clamp(index, 0, ActiveKid.Wishes.Count), wish);
        Commit();
    }

    public bool SendLetter(int year)
    {
        if (ActiveKid.Wishes.Count == 0)
        {
            return false;
        }

        ActiveKid.LetterSentYear = year;
        Commit();
        return true;
    }

    public void ReopenLetter()
    {
        ActiveKid.LetterSentYear = null;
        Commit();
    }

    public void OpenDoor(Kid kid, int year, int door)
    {
        var key = YearKey(year);
        if (!kid.OpenedDoors.TryGetValue(key, out var doors))
        {
            doors = [];
            kid.OpenedDoors[key] = doors;
        }

        if (!doors.Contains(door))
        {
            doors.Add(door);
            Commit();
        }
    }

    public bool IsDoorOpened(Kid kid, int year, int door) =>
        kid.OpenedDoors.TryGetValue(YearKey(year), out var doors) && doors.Contains(door);

    public void SetDoorMessage(Kid kid, int door, string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            kid.DoorMessages.Remove(door);
        }
        else
        {
            kid.DoorMessages[door] = message.Trim();
        }

        Commit();
    }

    public FamilyEvent AddEvent(string name, int month, int day, string sticker)
    {
        FamilyEvent familyEvent = new() { Name = name.Trim(), Month = month, Day = day, Sticker = sticker };
        _data.Events.Add(familyEvent);
        Commit();
        return familyEvent;
    }

    public void RemoveEvent(FamilyEvent familyEvent)
    {
        _data.Events.Remove(familyEvent);
        Commit();
    }

    public void SaveSettings() => Commit();

    private void Commit()
    {
        _store.Save(_data);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private string NextSticker()
    {
        string[] stickers = ["star", "snowman", "reindeer", "bell", "mitten", "tree", "gingerbread", "bauble"];
        return stickers[_data.Kids.Count % stickers.Length];
    }

    private static string YearKey(int year) => year.ToString(CultureInfo.InvariantCulture);
}
