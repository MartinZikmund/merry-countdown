using FluentAssertions;
using MerryCountdown.Core.Store;

namespace MerryCountdown.Core.Tests;

[TestClass]
public class FullVersionTests
{
    private sealed class FakeStore(bool billing = true) : IStoreService
    {
        public bool HasBilling => billing;

        public bool Owned { get; set; }

        public bool ThrowOnQuery { get; set; }

        public PurchaseResult NextPurchase { get; set; } = PurchaseResult.Succeeded;

        public Task<string?> GetPriceAsync(CancellationToken ct = default) => Task.FromResult<string?>("€3.99");

        public Task<bool> IsOwnedAsync(CancellationToken ct = default) =>
            ThrowOnQuery ? throw new InvalidOperationException() : Task.FromResult(Owned);

        public Task<PurchaseResult> PurchaseAsync(CancellationToken ct = default) => Task.FromResult(NextPurchase);
    }

    private sealed class FakeCache : IEntitlementCache
    {
        public bool IsFull { get; set; }
    }

    [TestMethod]
    public void NoBillingPlatform_HasEverythingUnlockedForFree()
    {
        FullVersionService service = new(new FakeStore(billing: false), new FakeCache());

        service.IsFull.Should().BeTrue();
        service.CanPurchase.Should().BeFalse();
    }

    [TestMethod]
    public void BillingPlatform_StartsFree()
    {
        FullVersionService service = new(new FakeStore(), new FakeCache());

        service.IsFull.Should().BeFalse();
        service.CanPurchase.Should().BeTrue();
    }

    [TestMethod]
    public async Task Purchase_Success_UnlocksRemembersAndNotifies()
    {
        FakeCache cache = new();
        FullVersionService service = new(new FakeStore(), cache);
        var raised = false;
        service.Changed += (_, _) => raised = true;

        (await service.PurchaseAsync()).Should().Be(PurchaseResult.Succeeded);

        service.IsFull.Should().BeTrue();
        cache.IsFull.Should().BeTrue();
        raised.Should().BeTrue();
        service.CanPurchase.Should().BeFalse();
    }

    [TestMethod]
    public async Task Purchase_AlreadyOwned_AlsoUnlocks()
    {
        FullVersionService service = new(new FakeStore { NextPurchase = PurchaseResult.AlreadyOwned }, new FakeCache());

        await service.PurchaseAsync();

        service.IsFull.Should().BeTrue();
    }

    [TestMethod]
    public async Task Purchase_Cancelled_StaysFree()
    {
        FullVersionService service = new(new FakeStore { NextPurchase = PurchaseResult.Cancelled }, new FakeCache());

        await service.PurchaseAsync();

        service.IsFull.Should().BeFalse();
    }

    [TestMethod]
    public async Task Refresh_StoreSaysOwned_RestoresThePurchase()
    {
        FullVersionService service = new(new FakeStore { Owned = true }, new FakeCache());

        await service.RefreshAsync();

        service.IsFull.Should().BeTrue();
    }

    [TestMethod]
    public async Task Refresh_StoreSaysNotOwned_RevokesARefund()
    {
        FullVersionService service = new(new FakeStore { Owned = false }, new FakeCache { IsFull = true });

        await service.RefreshAsync();

        service.IsFull.Should().BeFalse();
    }

    [TestMethod]
    public async Task Refresh_StoreUnreachable_KeepsWhatWeKnew()
    {
        FullVersionService service = new(new FakeStore { ThrowOnQuery = true }, new FakeCache { IsFull = true });

        await service.RefreshAsync();

        service.IsFull.Should().BeTrue();
    }
}
