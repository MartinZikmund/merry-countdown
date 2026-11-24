using MerryCountdown.Core.Services;
using MerryCountdown.Core.Tests.Fakes;
using FluentAssertions;

namespace MerryCountdown.Core.Tests.ViewModels;

[TestClass]
public class ScreenWakeTests
{
    private readonly FakeDisplayRequests _requests = new();
    private readonly FakeDisplayMode _display = new();
    private readonly FakeAppPreferences _preferences = new();

    private ScreenWake Create() => new(_display, _preferences, _requests);

    [TestMethod]
    public void Created_WithSettingOff_HoldsNothing()
    {
        using var wake = Create();

        _requests.Held.Should().Be(0);
    }

    [TestMethod]
    public void Created_WithSettingOn_HoldsTheScreen()
    {
        _preferences.KeepScreenOn = true;
        using var wake = Create();

        _requests.Held.Should().Be(1);
    }

    [TestMethod]
    public void SettingOn_HoldsTheScreen_AndSavesTheChoice()
    {
        using var wake = Create();

        wake.KeepScreenOn = true;

        _requests.Held.Should().Be(1);
        _preferences.KeepScreenOn.Should().BeTrue();
    }

    [TestMethod]
    public void SettingOff_ReleasesTheScreen()
    {
        _preferences.KeepScreenOn = true;
        using var wake = Create();

        wake.KeepScreenOn = false;

        _requests.Held.Should().Be(0);
    }

    [TestMethod]
    public void SettingOnTwice_TakesOnlyOneRequest()
    {
        using var wake = Create();

        wake.KeepScreenOn = true;
        wake.KeepScreenOn = true;

        _requests.Takes.Should().Be(1);
    }

    [TestMethod]
    public void FamilyDisplay_HoldsTheScreen_WhateverTheSetting()
    {
        using var wake = Create();

        _display.Enter();
        _requests.Held.Should().Be(1);

        _display.Exit();
        _requests.Held.Should().Be(0);
    }

    [TestMethod]
    public void LeavingFamilyDisplay_WithSettingOn_KeepsTheScreen()
    {
        using var wake = Create();
        wake.KeepScreenOn = true;
        _display.Enter();

        _display.Exit();

        _requests.Held.Should().Be(1);
    }

    [TestMethod]
    public void SettingOff_DuringFamilyDisplay_KeepsTheScreen()
    {
        using var wake = Create();
        wake.KeepScreenOn = true;
        _display.Enter();

        wake.KeepScreenOn = false;

        _requests.Held.Should().Be(1);
    }

    [TestMethod]
    public void FamilyDisplayAndSetting_NeverTakeTwoRequests()
    {
        using var wake = Create();

        wake.KeepScreenOn = true;
        _display.Enter();

        _requests.Held.Should().Be(1);
        _requests.Takes.Should().Be(1);
    }

    [TestMethod]
    public void Minimized_ReleasesTheScreen_AndComingBackTakesItAgain()
    {
        using var wake = Create();
        wake.KeepScreenOn = true;

        wake.SetWindowVisible(false);
        _requests.Held.Should().Be(0);

        wake.SetWindowVisible(true);
        _requests.Held.Should().Be(1);
    }

    [TestMethod]
    public void Minimized_ReleasesTheScreenEvenInFamilyDisplay()
    {
        using var wake = Create();
        _display.Enter();

        wake.SetWindowVisible(false);

        _requests.Held.Should().Be(0);
    }

    [TestMethod]
    public void Hidden_WithNothingToHold_StaysQuietWhenItReturns()
    {
        using var wake = Create();

        wake.SetWindowVisible(false);
        wake.SetWindowVisible(true);

        _requests.Takes.Should().Be(0);
    }

    [TestMethod]
    public void Disposed_ReleasesTheScreenAndStopsListening()
    {
        var wake = Create();
        wake.KeepScreenOn = true;

        wake.Dispose();
        _display.Enter();

        _requests.Held.Should().Be(0);
    }
}
