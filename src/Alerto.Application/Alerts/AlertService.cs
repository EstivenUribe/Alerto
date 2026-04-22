using System.Text.Json;
using Alerto.Application.Common.Exceptions;
using Alerto.Application.Common.Interfaces;
using Alerto.Application.Common.Models;
using Alerto.Domain.Entities;
using Alerto.Domain.Exceptions;
using AutoMapper;
using FluentValidation;

namespace Alerto.Application.Alerts;

public sealed class AlertService : IAlertService
{
    private const string AlertCachePrefix = "alerts:";
    private static readonly TimeSpan AlertCacheTtl = TimeSpan.FromMinutes(5);

    private readonly IAlertRepository _alertRepository;
    private readonly IAuditTrailRepository _auditTrailRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IGeofenceRepository _geofenceRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IAppCache _cache;
    private readonly IMapper _mapper;
    private readonly IValidator<CreateAlertRequest> _createValidator;
    private readonly IValidator<UpdateAlertRequest> _updateValidator;
    private readonly IValidator<ApproveAlertRequest> _approveValidator;
    private readonly IValidator<RejectAlertRequest> _rejectValidator;
    private readonly IValidator<CancelAlertRequest> _cancelValidator;
    private readonly IValidator<DispatchAlertRequest> _dispatchValidator;
    private readonly IValidator<AlertQueryRequest> _queryValidator;

    public AlertService(
        IAlertRepository alertRepository,
        IAuditTrailRepository auditTrailRepository,
        ICurrentUserService currentUserService,
        IGeofenceRepository geofenceRepository,
        IUnitOfWork unitOfWork,
        IClock clock,
        IAppCache cache,
        IMapper mapper,
        IValidator<CreateAlertRequest> createValidator,
        IValidator<UpdateAlertRequest> updateValidator,
        IValidator<ApproveAlertRequest> approveValidator,
        IValidator<RejectAlertRequest> rejectValidator,
        IValidator<CancelAlertRequest> cancelValidator,
        IValidator<DispatchAlertRequest> dispatchValidator,
        IValidator<AlertQueryRequest> queryValidator)
    {
        _alertRepository = alertRepository;
        _auditTrailRepository = auditTrailRepository;
        _currentUserService = currentUserService;
        _geofenceRepository = geofenceRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _cache = cache;
        _mapper = mapper;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _approveValidator = approveValidator;
        _rejectValidator = rejectValidator;
        _cancelValidator = cancelValidator;
        _dispatchValidator = dispatchValidator;
        _queryValidator = queryValidator;
    }

    public async Task<AlertResponse> CreateAsync(CreateAlertRequest request, CancellationToken cancellationToken)
    {
        await _createValidator.ValidateAndThrowAsync(request, cancellationToken);
        var actorId = GetRequiredActorId();

        if (!await _geofenceRepository.ExistsActiveAsync(request.GeofenceId, cancellationToken))
        {
            throw new NotFoundException("La geocerca indicada no existe o se encuentra inactiva.");
        }

        var alert = Alert.Create(
            request.Title,
            request.Description,
            request.Severity,
            request.SourceSystem,
            request.Address,
            request.Latitude,
            request.Longitude,
            request.GeofenceId,
            actorId,
            _clock.UtcNow);

        await _alertRepository.AddAsync(alert, cancellationToken);
        await AppendAuditAsync(actorId, "AlertCreated", alert.Id, request, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await MapAndCacheAsync(alert, cancellationToken);
    }

    public async Task<AlertResponse> UpdateAsync(Guid id, UpdateAlertRequest request, CancellationToken cancellationToken)
    {
        await _updateValidator.ValidateAndThrowAsync(request, cancellationToken);
        var alert = await GetAlertOrThrowAsync(id, cancellationToken);
        EnsureVersion(alert.Version, request.ExpectedVersion);

        if (!await _geofenceRepository.ExistsActiveAsync(request.GeofenceId, cancellationToken))
        {
            throw new NotFoundException("La geocerca indicada no existe o se encuentra inactiva.");
        }

        alert.UpdateDraft(
            request.Title,
            request.Description,
            request.Severity,
            request.SourceSystem,
            request.Address,
            request.Latitude,
            request.Longitude,
            request.GeofenceId,
            _clock.UtcNow);

        await AppendAuditAsync(GetRequiredActorId(), "AlertUpdated", alert.Id, request, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await MapAndCacheAsync(alert, cancellationToken);
    }

    public async Task<AlertResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var cacheKey = BuildAlertKey(id);
        var cached = await _cache.GetAsync<AlertResponse>(cacheKey, cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        var alert = await GetAlertOrThrowAsync(id, cancellationToken);
        return await MapAndCacheAsync(alert, cancellationToken);
    }

    public async Task<AlertListResponse> SearchAsync(AlertQueryRequest request, CancellationToken cancellationToken)
    {
        await _queryValidator.ValidateAndThrowAsync(request, cancellationToken);

        var page = await _alertRepository.SearchAsync(request, cancellationToken);
        return new AlertListResponse(
            page.Items.Select(MapResponse).ToArray(),
            page.PageNumber,
            page.PageSize,
            page.TotalCount,
            page.TotalPages,
            page.HasPreviousPage,
            page.HasNextPage);
    }

    public async Task<AlertResponse> ApproveAsync(Guid id, ApproveAlertRequest request, CancellationToken cancellationToken)
    {
        await _approveValidator.ValidateAndThrowAsync(request, cancellationToken);
        var alert = await GetAlertOrThrowAsync(id, cancellationToken);
        EnsureVersion(alert.Version, request.ExpectedVersion);

        if (alert.Status == Domain.Enums.AlertStatus.Approved)
        {
            throw new ConflictException("La alerta ya fue aprobada por otro operador.");
        }

        alert.Approve(GetRequiredActorId(), _clock.UtcNow);

        await AppendAuditAsync(GetRequiredActorId(), "AlertApproved", alert.Id, request, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await MapAndCacheAsync(alert, cancellationToken);
    }

    public async Task<AlertResponse> RejectAsync(Guid id, RejectAlertRequest request, CancellationToken cancellationToken)
    {
        await _rejectValidator.ValidateAndThrowAsync(request, cancellationToken);
        var alert = await GetAlertOrThrowAsync(id, cancellationToken);
        EnsureVersion(alert.Version, request.ExpectedVersion);

        if (alert.Status == Domain.Enums.AlertStatus.Approved)
        {
            throw new ConflictException("La alerta ya fue aprobada por otro operador y no puede rechazarse.");
        }

        alert.Reject(GetRequiredActorId(), request.Reason, _clock.UtcNow);

        await AppendAuditAsync(GetRequiredActorId(), "AlertRejected", alert.Id, request, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await MapAndCacheAsync(alert, cancellationToken);
    }

    public async Task<AlertResponse> CancelAsync(Guid id, CancelAlertRequest request, CancellationToken cancellationToken)
    {
        await _cancelValidator.ValidateAndThrowAsync(request, cancellationToken);
        var alert = await GetAlertOrThrowAsync(id, cancellationToken);
        EnsureVersion(alert.Version, request.ExpectedVersion);

        alert.Cancel(GetRequiredActorId(), request.Reason, _clock.UtcNow);

        await AppendAuditAsync(GetRequiredActorId(), "AlertCancelled", alert.Id, request, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await MapAndCacheAsync(alert, cancellationToken);
    }

    public async Task<AlertResponse> DispatchAsync(Guid id, DispatchAlertRequest request, CancellationToken cancellationToken)
    {
        await _dispatchValidator.ValidateAndThrowAsync(request, cancellationToken);
        var alert = await GetAlertOrThrowAsync(id, cancellationToken);
        EnsureVersion(alert.Version, request.ExpectedVersion);

        alert.Dispatch(
            request.Channel,
            request.Destination,
            request.ProviderReference,
            GetRequiredActorId(),
            _clock.UtcNow);

        await AppendAuditAsync(GetRequiredActorId(), "AlertDispatched", alert.Id, request, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await MapAndCacheAsync(alert, cancellationToken);
    }

    private Guid GetRequiredActorId()
    {
        if (!_currentUserService.IsAuthenticated || _currentUserService.UserId is null)
        {
            throw new UnauthorizedAccessException("La operación requiere un usuario autenticado.");
        }

        return _currentUserService.UserId.Value;
    }

    private async Task<Alert> GetAlertOrThrowAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _alertRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"No existe una alerta con id '{id}'.");
    }

    private void EnsureVersion(int currentVersion, int expectedVersion)
    {
        if (currentVersion != expectedVersion)
        {
            throw new ConflictException("La alerta fue modificada por otro proceso. Recargue y reintente.");
        }
    }

    private async Task AppendAuditAsync(
        Guid actorId,
        string action,
        Guid entityId,
        object details,
        CancellationToken cancellationToken)
    {
        var audit = AuditLog.Create(
            actorId,
            action,
            nameof(Alert),
            entityId,
            JsonSerializer.Serialize(details),
            _currentUserService.TraceId,
            _clock.UtcNow);

        await _auditTrailRepository.AddAsync(audit, cancellationToken);
    }

    private async Task<AlertResponse> MapAndCacheAsync(Alert alert, CancellationToken cancellationToken)
    {
        var response = MapResponse(alert);

        await _cache.SetAsync(BuildAlertKey(alert.Id), response, AlertCacheTtl, cancellationToken);
        return response;
    }

    private static string BuildAlertKey(Guid id) => $"{AlertCachePrefix}{id}";

    private static AlertResponse MapResponse(Alert alert)
    {
        return new AlertResponse(
            alert.Id,
            alert.Title,
            alert.Description,
            alert.Severity.ToString(),
            alert.Status.ToString(),
            alert.SourceSystem,
            alert.Address,
            alert.Latitude,
            alert.Longitude,
            alert.GeofenceId,
            alert.CreatedByUserId,
            alert.ApprovedByUserId,
            alert.CreatedAtUtc,
            alert.UpdatedAtUtc,
            alert.ApprovalDeadlineUtc,
            alert.Version,
            alert.Dispatches.Select(dispatch => new AlertDispatchResponse(
                dispatch.Id,
                dispatch.Channel.ToString(),
                dispatch.Destination,
                dispatch.ProviderReference,
                dispatch.DispatchedByUserId,
                dispatch.CreatedAtUtc)).ToArray());
    }
}
