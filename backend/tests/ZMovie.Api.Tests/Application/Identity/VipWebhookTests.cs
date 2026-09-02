using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using Xunit;
using ZMovie.Application.Identity;
using ZMovie.Domain.Identity;

namespace ZMovie.Api.Tests.Application.Identity;

public sealed class VipWebhookTests
{
    private static readonly DateTimeOffset InitialTime = new(2026, 9, 3, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void VerifyPayOsSignature_returns_true_for_valid_hmac()
    {
        var checksumKey = "secret_checksum_key_123";
        var data = new PayOsWebhookData(1001, 49000, "ZMVIP_f81d4fae-7dec-11d0-a765-00a0c91e6bf6_VIP1", null, "REF1001", null, "VND", null, "00", "success");

        var sortedPairs = new SortedDictionary<string, string>
        {
            ["amount"] = "49000",
            ["cancelUrl"] = "",
            ["description"] = "ZMVIP_f81d4fae-7dec-11d0-a765-00a0c91e6bf6_VIP1",
            ["orderCode"] = "1001",
            ["returnUrl"] = ""
        };
        var rawData = string.Join("&", sortedPairs.Where(p => !string.IsNullOrEmpty(p.Value)).Select(p => $"{p.Key}={p.Value}"));
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(checksumKey));
        var validSig = Convert.ToHexStringLower(hmac.ComputeHash(Encoding.UTF8.GetBytes(rawData)));

        var result = VipWebhookSecurity.VerifyPayOsSignature(data, validSig, checksumKey);
        result.Should().BeTrue();

        var tampered = VipWebhookSecurity.VerifyPayOsSignature(data, "invalid_sig_12345", checksumKey);
        tampered.Should().BeFalse();
    }

    [Fact]
    public async Task PayOsWebhookHandler_is_idempotent_when_reference_already_exists()
    {
        var existingSub = VipSubscription.Create(
            VipSubscriptionId.New(),
            new UserId(Guid.NewGuid()),
            "VIP1",
            49000,
            "PayOS",
            "REF999",
            InitialTime,
            InitialTime.AddMonths(1));

        var vipRepo = new FakeVipSubscriptionRepository(existingSub);
        var userRepo = new FakeUserRepository();
        var time = new FixedTimeProvider(InitialTime);

        var handler = new ProcessPayOsWebhookHandler(vipRepo, userRepo, time);
        var payload = new PayOsWebhookPayload(
            "00",
            "success",
            true,
            new PayOsWebhookData(999, 49000, "ZMVIP_abc", null, "REF999", null, null, null, null, null),
            null);

        var result = await handler.Handle(new ProcessPayOsWebhookCommand(payload, null), CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Should().BeTrue();
        userRepo.FindByIdCalls.Should().Be(0);
    }

    [Fact]
    public async Task PayOsWebhookHandler_fulfills_and_extends_user_vip()
    {
        var userId = UserId.New();
        var user = User.Create(userId, new ExternalIdentity("google-vip"), "vip@test.com", "VIP User", null, Role.Member, InitialTime);
        var userRepo = new FakeUserRepository(user);
        var vipRepo = new FakeVipSubscriptionRepository();
        var time = new FixedTimeProvider(InitialTime);

        var handler = new ProcessPayOsWebhookHandler(vipRepo, userRepo, time);
        var payload = new PayOsWebhookPayload(
            "00",
            "success",
            true,
            new PayOsWebhookData(1001, 49000, $"ZMVIP_{userId.Value}_VIP1", null, "TX1001", null, null, null, null, null),
            null);

        var result = await handler.Handle(new ProcessPayOsWebhookCommand(payload, null), CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Should().BeTrue();
        user.IsVipActive(InitialTime).Should().BeTrue();
        user.SubscriptionTier.Should().Be("VIP1");
        vipRepo.StoredSubscription.Should().NotBeNull();
        vipRepo.StoredSubscription!.TransactionReference.Should().Be("TX1001");
    }

    [Fact]
    public async Task SePayWebhookHandler_rejects_unauthorized_token()
    {
        var vipRepo = new FakeVipSubscriptionRepository();
        var userRepo = new FakeUserRepository();
        var time = new FixedTimeProvider(InitialTime);

        var handler = new ProcessSePayWebhookHandler(vipRepo, userRepo, time);
        var payload = new SePayWebhookPayload(
            123,
            "MBBank",
            "2026-09-03",
            "123456",
            null,
            49000,
            0,
            100000,
            "ZMVIP",
            "ZMVIP test",
            "REF123",
            null);

        var result = await handler.Handle(
            new ProcessSePayWebhookCommand(payload, "Apikey wrong_key", "secret_key_123"),
            CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("sepay.auth.invalid");
    }

    private sealed class FakeVipSubscriptionRepository(VipSubscription? initial = null) : IVipSubscriptionRepository
    {
        public VipSubscription? StoredSubscription { get; set; } = initial;

        public Task<VipSubscription?> FindByGatewayReferenceAsync(string gateway, string reference, CancellationToken ct) =>
            Task.FromResult(StoredSubscription?.PaymentGateway == gateway && StoredSubscription?.TransactionReference == reference
                ? StoredSubscription
                : null);

        public Task AddAsync(VipSubscription subscription, CancellationToken ct)
        {
            StoredSubscription = subscription;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<VipSubscription>> ListByUserIdAsync(UserId userId, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<VipSubscription>>(StoredSubscription != null ? [StoredSubscription] : []);
    }

    private sealed class FakeUserRepository(User? initial = null) : IUserRepository
    {
        public User? StoredUser { get; set; } = initial;
        public int FindByIdCalls { get; private set; }

        public Task<User?> FindByIdAsync(UserId id, CancellationToken ct)
        {
            FindByIdCalls++;
            return Task.FromResult(StoredUser?.Id == id ? StoredUser : null);
        }

        public Task<User?> FindByExternalIdentityAsync(ExternalIdentity externalIdentity, CancellationToken ct) =>
            Task.FromResult(StoredUser?.ExternalIdentity == externalIdentity ? StoredUser : null);

        public void Add(User user) => StoredUser = user;

        public Task SaveChangesAsync(CancellationToken ct) => Task.CompletedTask;

        public Task<SetRoleOutcome> ChangeRoleWithLastAdminGuardAsync(UserId userId, Role newRole, bool guardLastAdmin, CancellationToken ct) =>
            Task.FromResult(SetRoleOutcome.Updated);
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
