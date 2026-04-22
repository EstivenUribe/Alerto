using Alerto.Domain.Entities;
using Alerto.Domain.Enums;
using Alerto.Domain.Exceptions;

namespace Alerto.DomainTests;

public sealed class AlertTests
{
    private static readonly DateTime UtcNow = new DateTime(2026, 4, 21, 10, 0, 0, DateTimeKind.Utc);
    private static readonly Guid ActorId = Guid.NewGuid();
    private static readonly Guid GeofenceId = Guid.NewGuid();

    private static Alert CreateValidAlert(DateTime? utcNow = null)
        => Alert.Create(
            "Inundación sector norte",
            "Se reporta inundación en la calle 50 con carrera 30",
            Severity.Severe,
            "SistemaAlerta",
            "Calle 50 # 30-10",
            6.25m,
            -75.57m,
            GeofenceId,
            ActorId,
            utcNow ?? UtcNow);

    // ── Create ──────────────────────────────────────────────────────────────

    [Fact]
    public void Create_WithValidData_ShouldHavePendingStatus()
    {
        var alert = CreateValidAlert();

        alert.Status.Should().Be(AlertStatus.Pending);
        alert.Version.Should().Be(0);
        alert.Dispatches.Should().BeEmpty();
        alert.ApprovalRecords.Should().BeEmpty();
    }

    [Fact]
    public void Create_ShouldSetApprovalDeadline3MinutesFromCreation()
    {
        var alert = CreateValidAlert();

        alert.ApprovalDeadlineUtc.Should().Be(UtcNow.AddMinutes(3));
    }

    [Fact]
    public void Create_ShouldRaiseAlertCreatedDomainEvent()
    {
        var alert = CreateValidAlert();

        alert.DomainEvents.Should().ContainSingle()
            .Which.GetType().Name.Should().Be("AlertCreatedDomainEvent");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyTitle_ShouldThrowEntityValidationException(string title)
    {
        var act = () => Alert.Create(
            title, "desc", Severity.Moderate, "sys", "addr",
            6.25m, -75.57m, GeofenceId, ActorId, UtcNow);

        act.Should().Throw<EntityValidationException>();
    }

    [Fact]
    public void Create_WithTitleExceeding160Chars_ShouldThrowEntityValidationException()
    {
        var longTitle = new string('x', 161);

        var act = () => Alert.Create(
            longTitle, "desc", Severity.Moderate, "sys", "addr",
            6.25m, -75.57m, GeofenceId, ActorId, UtcNow);

        act.Should().Throw<EntityValidationException>();
    }

    // ── Approve ─────────────────────────────────────────────────────────────

    [Fact]
    public void Approve_WithinDeadline_ShouldTransitionToApproved()
    {
        var alert = CreateValidAlert();
        var approveTime = UtcNow.AddMinutes(1);

        alert.Approve(ActorId, approveTime);

        alert.Status.Should().Be(AlertStatus.Approved);
        alert.ApprovedByUserId.Should().Be(ActorId);
        alert.ApprovalRecords.Should().ContainSingle();
        alert.Version.Should().Be(1);
    }

    [Fact]
    public void Approve_AfterDeadline_ShouldThrowApprovalWindowExpiredException()
    {
        var alert = CreateValidAlert();
        var lateApproval = UtcNow.AddMinutes(4);

        var act = () => alert.Approve(ActorId, lateApproval);

        act.Should().Throw<ApprovalWindowExpiredException>();
    }

    [Fact]
    public void Approve_AlreadyRejected_ShouldThrowInvalidAlertStateTransitionException()
    {
        var alert = CreateValidAlert();
        alert.Reject(ActorId, "No procede", UtcNow.AddMinutes(1));

        var act = () => alert.Approve(ActorId, UtcNow.AddMinutes(2));

        act.Should().Throw<InvalidAlertStateTransitionException>();
    }

    // ── Reject ──────────────────────────────────────────────────────────────

    [Fact]
    public void Reject_PendingAlert_ShouldTransitionToRejected()
    {
        var alert = CreateValidAlert();

        alert.Reject(ActorId, "Información insuficiente", UtcNow.AddMinutes(1));

        alert.Status.Should().Be(AlertStatus.Rejected);
        alert.RejectedByUserId.Should().Be(ActorId);
        alert.Version.Should().Be(1);
    }

    [Fact]
    public void Reject_ApprovedAlert_ShouldThrowInvalidAlertStateTransitionException()
    {
        var alert = CreateValidAlert();
        alert.Approve(ActorId, UtcNow.AddMinutes(1));

        var act = () => alert.Reject(ActorId, "razón", UtcNow.AddMinutes(2));

        act.Should().Throw<InvalidAlertStateTransitionException>();
    }

    // ── Cancel ──────────────────────────────────────────────────────────────

    [Fact]
    public void Cancel_PendingAlert_ShouldTransitionToCancelled()
    {
        var alert = CreateValidAlert();

        alert.Cancel(ActorId, "Falsa alarma", UtcNow.AddMinutes(1));

        alert.Status.Should().Be(AlertStatus.Cancelled);
        alert.CancellationReason.Should().Be("Falsa alarma");
    }

    [Fact]
    public void Cancel_ApprovedAlert_ShouldTransitionToCancelled()
    {
        var alert = CreateValidAlert();
        alert.Approve(ActorId, UtcNow.AddMinutes(1));

        alert.Cancel(ActorId, "Situación resuelta", UtcNow.AddMinutes(2));

        alert.Status.Should().Be(AlertStatus.Cancelled);
    }

    [Fact]
    public void Cancel_RejectedAlert_ShouldThrowInvalidAlertStateTransitionException()
    {
        var alert = CreateValidAlert();
        alert.Reject(ActorId, "razón", UtcNow.AddMinutes(1));

        var act = () => alert.Cancel(ActorId, "intento", UtcNow.AddMinutes(2));

        act.Should().Throw<InvalidAlertStateTransitionException>();
    }

    [Fact]
    public void Cancel_AlreadyCancelled_ShouldThrowInvalidAlertStateTransitionException()
    {
        var alert = CreateValidAlert();
        alert.Cancel(ActorId, "primera", UtcNow.AddMinutes(1));

        var act = () => alert.Cancel(ActorId, "segunda", UtcNow.AddMinutes(2));

        act.Should().Throw<InvalidAlertStateTransitionException>();
    }

    // ── Dispatch ─────────────────────────────────────────────────────────────

    [Fact]
    public void Dispatch_ApprovedAlert_ShouldTransitionToBroadcasted()
    {
        var alert = CreateValidAlert();
        alert.Approve(ActorId, UtcNow.AddMinutes(1));

        alert.Dispatch(DispatchChannel.Sms, "+573001234567", "REF-001", ActorId, UtcNow.AddMinutes(2));

        alert.Status.Should().Be(AlertStatus.Broadcasted);
        alert.Dispatches.Should().ContainSingle();
    }

    [Fact]
    public void Dispatch_BroadcastedAlert_ShouldAddAnotherDispatch()
    {
        var alert = CreateValidAlert();
        alert.Approve(ActorId, UtcNow.AddMinutes(1));
        alert.Dispatch(DispatchChannel.Sms, "+573001234567", "REF-001", ActorId, UtcNow.AddMinutes(2));

        alert.Dispatch(DispatchChannel.Radio, "CANAL-1", "REF-002", ActorId, UtcNow.AddMinutes(3));

        alert.Dispatches.Should().HaveCount(2);
    }

    [Fact]
    public void Dispatch_PendingAlert_ShouldThrowInvalidAlertStateTransitionException()
    {
        var alert = CreateValidAlert();

        var act = () => alert.Dispatch(DispatchChannel.Sms, "+573001234567", "REF-001", ActorId, UtcNow);

        act.Should().Throw<InvalidAlertStateTransitionException>();
    }

    // ── Version ──────────────────────────────────────────────────────────────

    [Fact]
    public void Version_ShouldIncrementOnEachMutation()
    {
        var alert = CreateValidAlert();
        alert.Status.Should().Be(AlertStatus.Pending);
        alert.Version.Should().Be(0);

        alert.Approve(ActorId, UtcNow.AddMinutes(1));
        alert.Version.Should().Be(1);

        alert.Dispatch(DispatchChannel.Sms, "+57300", "REF", ActorId, UtcNow.AddMinutes(2));
        alert.Version.Should().Be(2);
    }
}
