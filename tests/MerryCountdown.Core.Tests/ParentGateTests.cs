using FluentAssertions;
using MerryCountdown.Core.Family;
using MerryCountdown.Core.Parents;
using MerryCountdown.Core.Tests.Fakes;

namespace MerryCountdown.Core.Tests;

[TestClass]
public class ParentGateTests
{
    private static (ParentGatePass Pass, FamilyService Family, FakeClock Clock) CreatePass()
    {
        FamilyService family = new(new FakeFamilyStore(), "en-US");
        FakeClock clock = new(new DateTime(2026, 12, 1, 18, 0, 0));
        return (new ParentGatePass(family, clock), family, clock);
    }

    [TestMethod]
    public void Pass_BeforeAnyAnswer_IsClosed() => CreatePass().Pass.IsOpen.Should().BeFalse();

    [TestMethod]
    public void Pass_StaysOpenForFifteenMinutesAfterAnAnswer()
    {
        var (pass, _, clock) = CreatePass();
        pass.Remember();

        clock.Now += TimeSpan.FromMinutes(14);
        pass.IsOpen.Should().BeTrue();

        clock.Now += TimeSpan.FromMinutes(1);
        pass.IsOpen.Should().BeFalse();
    }

    [TestMethod]
    public void Pass_ClockSetBack_IsClosed()
    {
        var (pass, _, clock) = CreatePass();
        pass.Remember();

        clock.Now -= TimeSpan.FromHours(1);

        pass.IsOpen.Should().BeFalse();
    }

    [TestMethod]
    public void Pass_KeptUnlocked_IsAlwaysOpen()
    {
        var (pass, family, _) = CreatePass();

        family.Settings.KeepGrownUpsUnlocked = true;

        pass.IsOpen.Should().BeTrue();
    }

    [TestMethod]
    public void Next_MultipliesTwoSingleDigitsAboveTheEasyOnes()
    {
        ParentGate gate = new(new Random(42));

        for (var i = 0; i < 50; i++)
        {
            var challenge = gate.Next();
            challenge.Left.Should().BeInRange(3, 9);
            challenge.Right.Should().BeInRange(3, 9);
        }
    }

    [TestMethod]
    public void Check_RightAnswerPasses_WrongAndGarbageFail()
    {
        ParentChallenge challenge = new(6, 7);

        ParentGate.Check(challenge, " 42 ").Should().BeTrue();
        ParentGate.Check(challenge, "41").Should().BeFalse();
        ParentGate.Check(challenge, "forty-two").Should().BeFalse();
        ParentGate.Check(challenge, null).Should().BeFalse();
    }
}
