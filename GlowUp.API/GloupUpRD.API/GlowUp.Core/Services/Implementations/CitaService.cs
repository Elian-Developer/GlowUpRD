using GloupUpRD.API.DTOs.Citas;
using GloupUpRD.API.Models;
using GloupUpRD.API.Repositories.Interfaces;
using GloupUpRD.API.Services.Interfaces;

namespace GloupUpRD.API.Services.Implementations;

public sealed class CitaService : ICitaService
{
    private static readonly HashSet<string> ValidStatuses = ["pending", "confirmed", "completed", "cancelled", "no_show"];
    private readonly ICitaRepository _repository;

    public CitaService(ICitaRepository repository) => _repository = repository;

    public async Task<MaintenanceResult<IReadOnlyList<CitaResponse>>> BuscarAsync(ulong usuarioId, ulong negocioId, DateOnly desde, DateOnly hasta, ulong? sucursalId, ulong? empleadoId, CancellationToken cancellationToken = default)
    {
        if (hasta < desde || hasta.DayNumber - desde.DayNumber > 62)
            return MaintenanceResult<IReadOnlyList<CitaResponse>>.Fail(MaintenanceStatus.Invalid, "El rango debe estar entre 1 y 63 días.");
        if (!await _repository.UsuarioTieneAccesoAsync(usuarioId, negocioId, cancellationToken))
            return MaintenanceResult<IReadOnlyList<CitaResponse>>.Fail(MaintenanceStatus.Forbidden, "No tienes acceso a este negocio.");

        var citas = await _repository.BuscarAsync(negocioId, desde, hasta, sucursalId, empleadoId, cancellationToken);
        return MaintenanceResult<IReadOnlyList<CitaResponse>>.Ok(citas.Select(Map).ToList());
    }

    public async Task<MaintenanceResult<CitaResponse>> ObtenerAsync(ulong usuarioId, ulong id, CancellationToken cancellationToken = default)
    {
        var cita = await _repository.ObtenerDetalleAsync(id, cancellationToken);
        if (cita is null) return MaintenanceResult<CitaResponse>.Fail(MaintenanceStatus.NotFound, "La cita no existe.");
        if (!await _repository.UsuarioTieneAccesoAsync(usuarioId, cita.BusinessId, cancellationToken))
            return MaintenanceResult<CitaResponse>.Fail(MaintenanceStatus.Forbidden, "No tienes acceso a esta cita.");
        return MaintenanceResult<CitaResponse>.Ok(Map(cita));
    }

    public async Task<MaintenanceResult<CitaResponse>> CrearAsync(ulong usuarioId, GuardarCitaRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await ValidateAsync(usuarioId, request, null, cancellationToken);
        if (validation.Error is not null) return MaintenanceResult<CitaResponse>.Fail(validation.Status, validation.Error);

        var cita = new Appointment
        {
            BusinessId = request.NegocioId,
            BranchId = request.SucursalId,
            CustomerId = request.ClienteId,
            BusinessCustomerId = validation.BusinessCustomer!.Id,
            EmployeeId = request.EmpleadoId,
            AppointmentDate = DateOnly.FromDateTime(request.Inicio),
            StartsAt = request.Inicio,
            EndsAt = validation.EndsAt,
            Status = request.Estado,
            CancellationReason = Normalize(request.MotivoCancelacion),
            Notes = Normalize(request.Notas),
            TotalAmount = validation.Services!.Sum(item => item.Price),
            CreatedAt = DateTime.UtcNow,
            AppointmentServices = BuildServices(validation.Services!)
        };

        await _repository.AgregarAsync(cita, cancellationToken);
        await _repository.GuardarCambiosAsync(cancellationToken);
        return await ReloadAsync(cita.Id, cancellationToken);
    }

    public async Task<MaintenanceResult<CitaResponse>> ActualizarAsync(ulong usuarioId, ulong id, GuardarCitaRequest request, CancellationToken cancellationToken = default)
    {
        var cita = await _repository.ObtenerParaEditarAsync(id, cancellationToken);
        if (cita is null) return MaintenanceResult<CitaResponse>.Fail(MaintenanceStatus.NotFound, "La cita no existe.");
        if (cita.BusinessId != request.NegocioId)
            return MaintenanceResult<CitaResponse>.Fail(MaintenanceStatus.Invalid, "No se puede mover una cita a otro negocio.");

        var validation = await ValidateAsync(usuarioId, request, id, cancellationToken);
        if (validation.Error is not null) return MaintenanceResult<CitaResponse>.Fail(validation.Status, validation.Error);

        cita.BranchId = request.SucursalId;
        cita.CustomerId = request.ClienteId;
        cita.BusinessCustomerId = validation.BusinessCustomer!.Id;
        cita.EmployeeId = request.EmpleadoId;
        cita.AppointmentDate = DateOnly.FromDateTime(request.Inicio);
        cita.StartsAt = request.Inicio;
        cita.EndsAt = validation.EndsAt;
        cita.Status = request.Estado;
        cita.CancellationReason = Normalize(request.MotivoCancelacion);
        cita.Notes = Normalize(request.Notas);
        cita.TotalAmount = validation.Services!.Sum(item => item.Price);
        cita.UpdatedAt = DateTime.UtcNow;
        _repository.ReemplazarServicios(cita, BuildServices(validation.Services!));

        await _repository.GuardarCambiosAsync(cancellationToken);
        return await ReloadAsync(cita.Id, cancellationToken);
    }

    public async Task<MaintenanceResult<bool>> EliminarAsync(ulong usuarioId, ulong id, CancellationToken cancellationToken = default)
    {
        var cita = await _repository.ObtenerParaEditarAsync(id, cancellationToken);
        if (cita is null) return MaintenanceResult<bool>.Fail(MaintenanceStatus.NotFound, "La cita no existe.");
        if (!await _repository.UsuarioTieneAccesoAsync(usuarioId, cita.BusinessId, cancellationToken))
            return MaintenanceResult<bool>.Fail(MaintenanceStatus.Forbidden, "No tienes acceso a esta cita.");

        cita.Status = "cancelled";
        cita.CancellationReason ??= "Cancelada desde el mantenimiento";
        cita.UpdatedAt = DateTime.UtcNow;
        await _repository.GuardarCambiosAsync(cancellationToken);
        return MaintenanceResult<bool>.Ok(true);
    }

    public async Task<MaintenanceResult<CatalogoCitasResponse>> ObtenerCatalogosAsync(ulong usuarioId, ulong negocioId, CancellationToken cancellationToken = default)
    {
        if (!await _repository.UsuarioTieneAccesoAsync(usuarioId, negocioId, cancellationToken))
            return MaintenanceResult<CatalogoCitasResponse>.Fail(MaintenanceStatus.Forbidden, "No tienes acceso a este negocio.");

        var branches = await _repository.ObtenerSucursalesAsync(negocioId, cancellationToken);
        var customers = await _repository.ObtenerClientesAsync(negocioId, cancellationToken);
        var employees = await _repository.ObtenerEmpleadosAsync(negocioId, cancellationToken);
        var services = await _repository.ObtenerServiciosActivosAsync(negocioId, cancellationToken);

        return MaintenanceResult<CatalogoCitasResponse>.Ok(new(
            branches.Select(item => new CatalogoItemResponse(item.Id, item.Name, item.City)).ToList(),
            customers.Select(item => new CatalogoItemResponse(item.CustomerId, $"{item.Customer.FirstName} {item.Customer.LastName}", item.Customer.Phone)).ToList(),
            employees.Select(item => new CatalogoItemResponse(item.Id, $"{item.FirstName} {item.LastName}", item.Position)).ToList(),
            services.Select(item => new CatalogoServicioResponse(item.Id, item.Name, item.DurationMinutes, item.Price)).ToList()));
    }

    public async Task<IReadOnlyList<NegocioResumenResponse>> ObtenerNegociosAsync(ulong usuarioId, CancellationToken cancellationToken = default) =>
        (await _repository.ObtenerNegociosAsync(usuarioId, cancellationToken))
            .Select(item => new NegocioResumenResponse(item.Id, item.Name, item.BusinessType)).ToList();

    private async Task<ValidationData> ValidateAsync(ulong usuarioId, GuardarCitaRequest request, ulong? excludeId, CancellationToken cancellationToken)
    {
        if (!ValidStatuses.Contains(request.Estado)) return ValidationData.Fail(MaintenanceStatus.Invalid, "El estado de la cita no es válido.");
        if (request.Inicio == default) return ValidationData.Fail(MaintenanceStatus.Invalid, "Debes indicar la fecha y hora de inicio.");
        if (request.ServicioIds.Count == 0 || request.ServicioIds.Distinct().Count() != request.ServicioIds.Count)
            return ValidationData.Fail(MaintenanceStatus.Invalid, "Debes seleccionar servicios válidos y sin duplicados.");
        if (!await _repository.UsuarioTieneAccesoAsync(usuarioId, request.NegocioId, cancellationToken))
            return ValidationData.Fail(MaintenanceStatus.Forbidden, "No tienes acceso a este negocio.");

        var branch = await _repository.ObtenerSucursalAsync(request.NegocioId, request.SucursalId, cancellationToken);
        if (branch is null) return ValidationData.Fail(MaintenanceStatus.Invalid, "La sucursal no pertenece al negocio o está inactiva.");
        var employee = await _repository.ObtenerEmpleadoAsync(request.NegocioId, request.EmpleadoId, cancellationToken);
        if (employee is null || (employee.BranchId.HasValue && employee.BranchId != request.SucursalId))
            return ValidationData.Fail(MaintenanceStatus.Invalid, "El empleado no pertenece a la sucursal seleccionada.");
        if (await _repository.ObtenerClienteAsync(request.ClienteId, cancellationToken) is null)
            return ValidationData.Fail(MaintenanceStatus.Invalid, "El cliente no existe.");
        var businessCustomer = await _repository.ObtenerClienteNegocioAsync(request.NegocioId, request.ClienteId, cancellationToken);
        if (businessCustomer is null) return ValidationData.Fail(MaintenanceStatus.Invalid, "El cliente no está asociado a este negocio.");

        var services = await _repository.ObtenerServiciosAsync(request.NegocioId, request.ServicioIds, cancellationToken);
        if (services.Count != request.ServicioIds.Count) return ValidationData.Fail(MaintenanceStatus.Invalid, "Uno o más servicios no pertenecen al negocio o están inactivos.");
        var endsAt = request.Inicio.AddMinutes(services.Sum(item => (long)item.DurationMinutes));
        if (request.Estado is not "cancelled" and not "no_show" && await _repository.ExisteConflictoAsync(request.EmpleadoId, request.Inicio, endsAt, excludeId, cancellationToken))
            return ValidationData.Fail(MaintenanceStatus.Conflict, "El empleado ya tiene una cita en ese horario.");

        return new(MaintenanceStatus.Success, null, services, businessCustomer, endsAt);
    }

    private async Task<MaintenanceResult<CitaResponse>> ReloadAsync(ulong id, CancellationToken cancellationToken)
    {
        var saved = await _repository.ObtenerDetalleAsync(id, cancellationToken);
        return saved is null ? MaintenanceResult<CitaResponse>.Fail(MaintenanceStatus.NotFound, "No se pudo recargar la cita.") : MaintenanceResult<CitaResponse>.Ok(Map(saved));
    }

    private static List<AppointmentService> BuildServices(IEnumerable<Service> services) => services.Select(item => new AppointmentService { ServiceId = item.Id, ServiceName = item.Name, DurationMinutes = item.DurationMinutes, Price = item.Price }).ToList();
    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static CitaResponse Map(Appointment cita) => new(cita.Id, cita.BusinessId, cita.BranchId, cita.Branch.Name, cita.CustomerId, $"{cita.Customer.FirstName} {cita.Customer.LastName}", cita.EmployeeId, $"{cita.Employee.FirstName} {cita.Employee.LastName}", cita.AppointmentDate, cita.StartsAt, cita.EndsAt, cita.Status, cita.CancellationReason, cita.Notes, cita.TotalAmount, cita.AppointmentServices.Select(item => new CitaServicioResponse(item.Id, item.ServiceId, item.ServiceName, item.DurationMinutes, item.Price)).ToList());

    private sealed record ValidationData(MaintenanceStatus Status, string? Error, List<Service>? Services = null, BusinessCustomer? BusinessCustomer = null, DateTime EndsAt = default)
    {
        public static ValidationData Fail(MaintenanceStatus status, string error) => new(status, error);
    }
}
