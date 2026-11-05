using FluentAssertions;
using MerryCountdown.Core.Parents;

namespace MerryCountdown.Core.Tests;

[TestClass]
public class ParentGateTests
{
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
