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

    public async Task<MaintenanceResult<IReadOnlyList<CitaResponse>>> BuscarAsync(long usuarioId, long negocioId, DateOnly desde, DateOnly hasta, long? sucursalId, long? empleadoId, CancellationToken cancellationToken = default)
    {
        if (hasta < desde || hasta.DayNumber - desde.DayNumber > 62)
            return MaintenanceResult<IReadOnlyList<CitaResponse>>.Fail(MaintenanceStatus.Invalid, "El rango debe estar entre 1 y 63 días.");
        if (!await _repository.UsuarioTieneAccesoAsync(usuarioId, negocioId, cancellationToken))
            return MaintenanceResult<IReadOnlyList<CitaResponse>>.Fail(MaintenanceStatus.Forbidden, "No tienes acceso a este negocio.");

        var citas = await _repository.BuscarAsync(negocioId, desde, hasta, sucursalId, empleadoId, cancellationToken);
        return MaintenanceResult<IReadOnlyList<CitaResponse>>.Ok(citas.Select(Map).ToList());
    }

    public async Task<MaintenanceResult<CitaResponse>> ObtenerAsync(long usuarioId, long id, CancellationToken cancellationToken = default)
    {
        var cita = await _repository.ObtenerDetalleAsync(id, cancellationToken);
        if (cita is null) return MaintenanceResult<CitaResponse>.Fail(MaintenanceStatus.NotFound, "La cita no existe.");
        if (!await _repository.UsuarioTieneAccesoAsync(usuarioId, cita.NegocioId, cancellationToken))
            return MaintenanceResult<CitaResponse>.Fail(MaintenanceStatus.Forbidden, "No tienes acceso a esta cita.");
        return MaintenanceResult<CitaResponse>.Ok(Map(cita));
    }

    public async Task<MaintenanceResult<CitaResponse>> CrearAsync(long usuarioId, GuardarCitaRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await ValidateAsync(usuarioId, request, null, cancellationToken);
        if (validation.Error is not null) return MaintenanceResult<CitaResponse>.Fail(validation.Status, validation.Error);

        var cita = new Cita
        {
            NegocioId = request.NegocioId,
            SucursalId = request.SucursalId,
            ClienteId = request.ClienteId,
            ClienteNegocioId = validation.ClienteNegocio!.Id,
            EmpleadoId = request.EmpleadoId,
            FechaCita = DateOnly.FromDateTime(request.Inicio),
            Inicio = request.Inicio,
            Fin = validation.Fin,
            Estado = request.Estado,
            MotivoCancelacion = Normalize(request.MotivoCancelacion),
            Notas = Normalize(request.Notas),
            Total = validation.Servicios!.Sum(item => item.Precio),
            CreadoEn = DateTime.UtcNow,
            ServiciosCita = BuildServices(validation.Servicios!)
        };

        await _repository.AgregarAsync(cita, cancellationToken);
        await _repository.GuardarCambiosAsync(cancellationToken);
        return await ReloadAsync(cita.Id, cancellationToken);
    }

    public async Task<MaintenanceResult<CitaResponse>> ActualizarAsync(long usuarioId, long id, GuardarCitaRequest request, CancellationToken cancellationToken = default)
    {
        var cita = await _repository.ObtenerParaEditarAsync(id, cancellationToken);
        if (cita is null) return MaintenanceResult<CitaResponse>.Fail(MaintenanceStatus.NotFound, "La cita no existe.");
        if (cita.NegocioId != request.NegocioId)
            return MaintenanceResult<CitaResponse>.Fail(MaintenanceStatus.Invalid, "No se puede mover una cita a otro negocio.");

        var validation = await ValidateAsync(usuarioId, request, id, cancellationToken);
        if (validation.Error is not null) return MaintenanceResult<CitaResponse>.Fail(validation.Status, validation.Error);

        cita.SucursalId = request.SucursalId;
        cita.ClienteId = request.ClienteId;
        cita.ClienteNegocioId = validation.ClienteNegocio!.Id;
        cita.EmpleadoId = request.EmpleadoId;
        cita.FechaCita = DateOnly.FromDateTime(request.Inicio);
        cita.Inicio = request.Inicio;
        cita.Fin = validation.Fin;
        cita.Estado = request.Estado;
        cita.MotivoCancelacion = Normalize(request.MotivoCancelacion);
        cita.Notas = Normalize(request.Notas);
        cita.Total = validation.Servicios!.Sum(item => item.Precio);
        cita.ActualizadoEn = DateTime.UtcNow;
        _repository.ReemplazarServicios(cita, BuildServices(validation.Servicios!));

        await _repository.GuardarCambiosAsync(cancellationToken);
        return await ReloadAsync(cita.Id, cancellationToken);
    }

    public async Task<MaintenanceResult<bool>> EliminarAsync(long usuarioId, long id, CancellationToken cancellationToken = default)
    {
        var cita = await _repository.ObtenerParaEditarAsync(id, cancellationToken);
        if (cita is null) return MaintenanceResult<bool>.Fail(MaintenanceStatus.NotFound, "La cita no existe.");
        if (!await _repository.UsuarioTieneAccesoAsync(usuarioId, cita.NegocioId, cancellationToken))
            return MaintenanceResult<bool>.Fail(MaintenanceStatus.Forbidden, "No tienes acceso a esta cita.");

        cita.Estado = "cancelled";
        cita.MotivoCancelacion ??= "Cancelada desde el mantenimiento";
        cita.ActualizadoEn = DateTime.UtcNow;
        await _repository.GuardarCambiosAsync(cancellationToken);
        return MaintenanceResult<bool>.Ok(true);
    }

    public async Task<MaintenanceResult<CatalogoCitasResponse>> ObtenerCatalogosAsync(long usuarioId, long negocioId, CancellationToken cancellationToken = default)
    {
        if (!await _repository.UsuarioTieneAccesoAsync(usuarioId, negocioId, cancellationToken))
            return MaintenanceResult<CatalogoCitasResponse>.Fail(MaintenanceStatus.Forbidden, "No tienes acceso a este negocio.");

        var sucursales = await _repository.ObtenerSucursalesAsync(negocioId, cancellationToken);
        var clientes = await _repository.ObtenerClientesAsync(negocioId, cancellationToken);
        var empleados = await _repository.ObtenerEmpleadosAsync(negocioId, cancellationToken);
        var servicios = await _repository.ObtenerServiciosActivosAsync(negocioId, cancellationToken);

        return MaintenanceResult<CatalogoCitasResponse>.Ok(new(
            sucursales.Select(item => new CatalogoItemResponse(item.Id, item.Nombre, item.Ciudad)).ToList(),
            clientes.Select(item => new CatalogoItemResponse(item.ClienteId, $"{item.Cliente.Nombre} {item.Cliente.Apellido}", item.Cliente.Telefono)).ToList(),
            empleados.Select(item => new CatalogoItemResponse(item.Id, $"{item.Nombre} {item.Apellido}", item.Puesto)).ToList(),
            servicios.Select(item => new CatalogoServicioResponse(item.Id, item.Nombre, item.DuracionMinutos, item.Precio)).ToList()));
    }

    public async Task<IReadOnlyList<NegocioResumenResponse>> ObtenerNegociosAsync(long usuarioId, CancellationToken cancellationToken = default) =>
        (await _repository.ObtenerNegociosAsync(usuarioId, cancellationToken))
            .Select(item => new NegocioResumenResponse(item.Id, item.Nombre, item.TipoNegocio)).ToList();

    private async Task<ValidationData> ValidateAsync(long usuarioId, GuardarCitaRequest request, long? excludeId, CancellationToken cancellationToken)
    {
        if (!ValidStatuses.Contains(request.Estado)) return ValidationData.Fail(MaintenanceStatus.Invalid, "El estado de la cita no es válido.");
        if (request.Inicio == default) return ValidationData.Fail(MaintenanceStatus.Invalid, "Debes indicar la fecha y hora de inicio.");
        if (request.ServicioIds.Count == 0 || request.ServicioIds.Distinct().Count() != request.ServicioIds.Count)
            return ValidationData.Fail(MaintenanceStatus.Invalid, "Debes seleccionar servicios válidos y sin duplicados.");
        if (!await _repository.UsuarioTieneAccesoAsync(usuarioId, request.NegocioId, cancellationToken))
            return ValidationData.Fail(MaintenanceStatus.Forbidden, "No tienes acceso a este negocio.");

        var sucursal = await _repository.ObtenerSucursalAsync(request.NegocioId, request.SucursalId, cancellationToken);
        if (sucursal is null) return ValidationData.Fail(MaintenanceStatus.Invalid, "La sucursal no pertenece al negocio o está inactiva.");
        var empleado = await _repository.ObtenerEmpleadoAsync(request.NegocioId, request.EmpleadoId, cancellationToken);
        if (empleado is null || (empleado.SucursalId.HasValue && empleado.SucursalId != request.SucursalId))
            return ValidationData.Fail(MaintenanceStatus.Invalid, "El empleado no pertenece a la sucursal seleccionada.");
        if (await _repository.ObtenerClienteAsync(request.ClienteId, cancellationToken) is null)
            return ValidationData.Fail(MaintenanceStatus.Invalid, "El cliente no existe.");
        var clienteNegocio = await _repository.ObtenerClienteNegocioAsync(request.NegocioId, request.ClienteId, cancellationToken);
        if (clienteNegocio is null) return ValidationData.Fail(MaintenanceStatus.Invalid, "El cliente no está asociado a este negocio.");

        var servicios = await _repository.ObtenerServiciosAsync(request.NegocioId, request.ServicioIds, cancellationToken);
        if (servicios.Count != request.ServicioIds.Count) return ValidationData.Fail(MaintenanceStatus.Invalid, "Uno o más servicios no pertenecen al negocio o están inactivos.");
        var fin = request.Inicio.AddMinutes(servicios.Sum(item => item.DuracionMinutos));
        if (request.Estado is not "cancelled" and not "no_show" && await _repository.ExisteConflictoAsync(request.EmpleadoId, request.Inicio, fin, excludeId, cancellationToken))
            return ValidationData.Fail(MaintenanceStatus.Conflict, "El empleado ya tiene una cita en ese horario.");

        return new(MaintenanceStatus.Success, null, servicios, clienteNegocio, fin);
    }

    private async Task<MaintenanceResult<CitaResponse>> ReloadAsync(long id, CancellationToken cancellationToken)
    {
        var saved = await _repository.ObtenerDetalleAsync(id, cancellationToken);
        return saved is null ? MaintenanceResult<CitaResponse>.Fail(MaintenanceStatus.NotFound, "No se pudo recargar la cita.") : MaintenanceResult<CitaResponse>.Ok(Map(saved));
    }

    private static List<ServicioCita> BuildServices(IEnumerable<Servicio> servicios) => servicios.Select(item => new ServicioCita { ServicioId = item.Id, NombreServicio = item.Nombre, DuracionMinutos = item.DuracionMinutos, Precio = item.Precio }).ToList();
    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static CitaResponse Map(Cita cita) => new(cita.Id, cita.NegocioId, cita.SucursalId, cita.Sucursal.Nombre, cita.ClienteId, $"{cita.Cliente.Nombre} {cita.Cliente.Apellido}", cita.EmpleadoId, $"{cita.Empleado.Nombre} {cita.Empleado.Apellido}", cita.FechaCita, cita.Inicio, cita.Fin, cita.Estado, cita.MotivoCancelacion, cita.Notas, cita.Total, cita.ServiciosCita.Select(item => new CitaServicioResponse(item.Id, item.ServicioId, item.NombreServicio, item.DuracionMinutos, item.Precio)).ToList());

    private sealed record ValidationData(MaintenanceStatus Status, string? Error, List<Servicio>? Servicios = null, ClientesNegocio? ClienteNegocio = null, DateTime Fin = default)
    {
        public static ValidationData Fail(MaintenanceStatus status, string error) => new(status, error);
    }
}
