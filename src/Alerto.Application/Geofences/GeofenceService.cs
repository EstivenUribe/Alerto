using System.Text.Json;
using Alerto.Application.Common.Exceptions;
using Alerto.Application.Common.Interfaces;
using Alerto.Application.Common.Models;
using Alerto.Domain.Entities;
using AutoMapper;
using FluentValidation;

namespace Alerto.Application.Geofences;

public sealed class GeofenceService : IGeofenceService
{
    private const string GeofenceActiveCatalogCacheKey = "geofences:active-catalog";

    private readonly IGeofenceRepository _geofenceRepository;
    private readonly IAuditTrailRepository _auditTrailRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IAppCache _cache;
    private readonly IMapper _mapper;
    private readonly IValidator<CreateGeofenceRequest> _createValidator;
    private readonly IValidator<UpdateGeofenceRequest> _updateValidator;
    private readonly IValidator<GeofenceQueryRequest> _queryValidator;
    private readonly IValidator<ChangeGeofenceStatusRequest> _statusValidator;

    public GeofenceService(
        IGeofenceRepository geofenceRepository,
        IAuditTrailRepository auditTrailRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork,
        IClock clock,
        IAppCache cache,
        IMapper mapper,
        IValidator<CreateGeofenceRequest> createValidator,
        IValidator<UpdateGeofenceRequest> updateValidator,
        IValidator<GeofenceQueryRequest> queryValidator,
        IValidator<ChangeGeofenceStatusRequest> statusValidator)
    {
        _geofenceRepository = geofenceRepository;
        _auditTrailRepository = auditTrailRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _cache = cache;
        _mapper = mapper;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _queryValidator = queryValidator;
        _statusValidator = statusValidator;
    }

    public async Task<GeofenceListResponse> SearchAsync(GeofenceQueryRequest request, CancellationToken cancellationToken)
    {
        await _queryValidator.ValidateAndThrowAsync(request, cancellationToken);

        var geofences = await _geofenceRepository.SearchAsync(request, cancellationToken);
        var items = _mapper.Map<IReadOnlyCollection<GeofenceResponse>>(geofences.Items);
        return new GeofenceListResponse(
            items,
            geofences.PageNumber,
            geofences.PageSize,
            geofences.TotalCount,
            geofences.TotalPages,
            geofences.HasPreviousPage,
            geofences.HasNextPage);
    }

    public async Task<GeofenceResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var geofence = await _geofenceRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"No existe una geocerca con id '{id}'.");

        return _mapper.Map<GeofenceResponse>(geofence);
    }

    public async Task<GeofenceResponse> CreateAsync(CreateGeofenceRequest request, CancellationToken cancellationToken)
    {
        await _createValidator.ValidateAndThrowAsync(request, cancellationToken);

        var existing = await _geofenceRepository.GetByCodeAsync(request.Code, cancellationToken);
        if (existing is not null)
        {
            throw new ConflictException("Ya existe una geocerca con el mismo codigo.");
        }

        var geofence = Geofence.Create(
            request.Code,
            request.Name,
            request.PolygonWkt,
            request.Neighborhood,
            _clock.UtcNow);

        await _geofenceRepository.AddAsync(geofence, cancellationToken);
        await AppendAuditAsync("GeofenceCreated", geofence.Id, request, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await InvalidateActiveCatalogAsync(cancellationToken);

        return _mapper.Map<GeofenceResponse>(geofence);
    }

    public async Task<GeofenceResponse> UpdateAsync(Guid id, UpdateGeofenceRequest request, CancellationToken cancellationToken)
    {
        await _updateValidator.ValidateAndThrowAsync(request, cancellationToken);
        var geofence = await _geofenceRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"No existe una geocerca con id '{id}'.");

        EnsureExpectedVersion(geofence.Version, request.ExpectedVersion);
        geofence.Update(request.Name, request.PolygonWkt, request.Neighborhood, geofence.IsActive, _clock.UtcNow);

        await AppendAuditAsync("GeofenceUpdated", geofence.Id, request, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await InvalidateActiveCatalogAsync(cancellationToken);

        return _mapper.Map<GeofenceResponse>(geofence);
    }

    public async Task<GeofenceResponse> ActivateAsync(Guid id, ChangeGeofenceStatusRequest request, CancellationToken cancellationToken)
    {
        await _statusValidator.ValidateAndThrowAsync(request, cancellationToken);
        var geofence = await _geofenceRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"No existe una geocerca con id '{id}'.");

        EnsureExpectedVersion(geofence.Version, request.ExpectedVersion);
        if (geofence.IsActive)
        {
            throw new ConflictException("La geocerca ya se encuentra activa.");
        }

        geofence.Update(geofence.Name, geofence.PolygonWkt, geofence.Neighborhood, true, _clock.UtcNow);
        await AppendAuditAsync("GeofenceActivated", geofence.Id, request, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await InvalidateActiveCatalogAsync(cancellationToken);

        return _mapper.Map<GeofenceResponse>(geofence);
    }

    public async Task<GeofenceResponse> DeactivateAsync(Guid id, ChangeGeofenceStatusRequest request, CancellationToken cancellationToken)
    {
        await _statusValidator.ValidateAndThrowAsync(request, cancellationToken);
        var geofence = await _geofenceRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"No existe una geocerca con id '{id}'.");

        EnsureExpectedVersion(geofence.Version, request.ExpectedVersion);
        if (!geofence.IsActive)
        {
            throw new ConflictException("La geocerca ya se encuentra inactiva.");
        }

        geofence.Update(geofence.Name, geofence.PolygonWkt, geofence.Neighborhood, false, _clock.UtcNow);
        await AppendAuditAsync("GeofenceDeactivated", geofence.Id, request, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await InvalidateActiveCatalogAsync(cancellationToken);

        return _mapper.Map<GeofenceResponse>(geofence);
    }

    private async Task AppendAuditAsync(string action, Guid entityId, object details, CancellationToken cancellationToken)
    {
        var actorId = _currentUserService.UserId ?? Guid.Empty;
        var audit = AuditLog.Create(
            actorId,
            action,
            nameof(Geofence),
            entityId,
            JsonSerializer.Serialize(details),
            _currentUserService.TraceId,
            _clock.UtcNow);

        await _auditTrailRepository.AddAsync(audit, cancellationToken);
    }

    private async Task InvalidateActiveCatalogAsync(CancellationToken cancellationToken)
    {
        await _cache.RemoveAsync(GeofenceActiveCatalogCacheKey, cancellationToken);
    }

    private static void EnsureExpectedVersion(int currentVersion, int expectedVersion)
    {
        if (currentVersion != expectedVersion)
        {
            throw new ConflictException("La geocerca fue modificada por otro operador. Refresque la vista y reintente.");
        }
    }

}
