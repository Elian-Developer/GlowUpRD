using GlowUpRD.API.DTOs.Ausencias;
using GlowUpRD.API.Models;
using GlowUpRD.API.Repositories.Interfaces;
using GlowUpRD.API.Services.Interfaces;
using GlowUpRD.API.Validation;

namespace GlowUpRD.API.Services.Implementations;

public sealed class AusenciaService : IAusenciaService
{
    private static readonly HashSet<string> TiposValidos = ["vacation", "permission", "absence"];
    private readonly IAusenciaRepository _ausencias;
    private readonly INegocioRepository _negocios;

    public AusenciaService(IAusenciaRepository ausencias, INegocioRepository negocios)
    {
        _ausencias = ausencias;
        _negocios = negocios;
    }

    public async Task<MaintenanceResult<IReadOnlyList<AusenciaResponse>>> BuscarAsync(long usuarioId, long negocioId, DateOnly desde, DateOnly hasta, long? empleadoId, bool incluirCanceladas, CancellationToken cancellationToken = default)
    {
        if (hasta < desde || hasta.DayNumber - desde.DayNumber > 366)
            return MaintenanceResult<IReadOnlyList<AusenciaResponse>>.Fail(MaintenanceStatus.Invalid, "El rango debe estar entre 1 y 367 días.");
        if (!await _negocios.UsuarioTieneAccesoAsync(usuarioId, negocioId, cancellationToken))
            return MaintenanceResult<IReadOnlyList<AusenciaResponse>>.Fail(MaintenanceStatus.Forbidden, "No tienes acceso a este negocio.");

        var items = await _ausencias.BuscarAsync(negocioId, desde.ToDateTime(TimeOnly.MinValue), hasta.AddDays(1).ToDateTime(TimeOnly.MinValue), empleadoId, incluirCanceladas, cancellationToken);
        return MaintenanceResult<IReadOnlyList<AusenciaResponse>>.Ok(items.Select(Map).ToList());
    }

    public async Task<MaintenanceResult<AusenciaResponse>> CrearAsync(long usuarioId, GuardarAusenciaRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await ValidarAsync(usuarioId, request, null, cancellationToken);
        if (validation.Error is not null) return MaintenanceResult<AusenciaResponse>.Fail(validation.Status, validation.Error);

        var ausencia = new AusenciasEmpleado
        {
            EmpleadoId = request.EmpleadoId,
            IniciaEn = request.IniciaEn,
            TerminaEn = request.TerminaEn,
            Tipo = request.Tipo,
            Motivo = Normalize(request.Motivo),
            Estado = "scheduled",
            CreadoEn = DateTime.UtcNow,
        };
        await _ausencias.AgregarAsync(ausencia, cancellationToken);
        await _ausencias.GuardarCambiosAsync(cancellationToken);
        return MaintenanceResult<AusenciaResponse>.Ok(Map(ausencia, validation.Empleado!));
    }

    public async Task<MaintenanceResult<AusenciaResponse>> ActualizarAsync(long usuarioId, long id, GuardarAusenciaRequest request, CancellationToken cancellationToken = default)
    {
        var ausencia = await _ausencias.ObtenerAsync(id, true, cancellationToken);
        if (ausencia is null) return MaintenanceResult<AusenciaResponse>.Fail(MaintenanceStatus.NotFound, "La ausencia no existe.");
        if (ausencia.Empleado.NegocioId != request.NegocioId)
            return MaintenanceResult<AusenciaResponse>.Fail(MaintenanceStatus.Invalid, "No se puede mover una ausencia a otro negocio.");
        if (ausencia.Estado == "cancelled")
            return MaintenanceResult<AusenciaResponse>.Fail(MaintenanceStatus.Invalid, "No se puede editar una ausencia cancelada.");

        var validation = await ValidarAsync(usuarioId, request, id, cancellationToken);
        if (validation.Error is not null) return MaintenanceResult<AusenciaResponse>.Fail(validation.Status, validation.Error);

        ausencia.EmpleadoId = request.EmpleadoId;
        ausencia.IniciaEn = request.IniciaEn;
        ausencia.TerminaEn = request.TerminaEn;
        ausencia.Tipo = request.Tipo;
        ausencia.Motivo = Normalize(request.Motivo);
        await _ausencias.GuardarCambiosAsync(cancellationToken);
        return MaintenanceResult<AusenciaResponse>.Ok(Map(ausencia, validation.Empleado!));
    }

    public async Task<MaintenanceResult<bool>> CancelarAsync(long usuarioId, long id, CancellationToken cancellationToken = default)
    {
        var ausencia = await _ausencias.ObtenerAsync(id, true, cancellationToken);
        if (ausencia is null) return MaintenanceResult<bool>.Fail(MaintenanceStatus.NotFound, "La ausencia no existe.");
        if (!await _negocios.UsuarioTieneAccesoAsync(usuarioId, ausencia.Empleado.NegocioId, cancellationToken))
            return MaintenanceResult<bool>.Fail(MaintenanceStatus.Forbidden, "No tienes acceso a esta ausencia.");
        if (ausencia.Estado == "cancelled") return MaintenanceResult<bool>.Ok(true);

        ausencia.Estado = "cancelled";
        await _ausencias.GuardarCambiosAsync(cancellationToken);
        return MaintenanceResult<bool>.Ok(true);
    }

    private async Task<ValidationData> ValidarAsync(long usuarioId, GuardarAusenciaRequest request, long? excluirId, CancellationToken cancellationToken)
    {
        if (request.IniciaEn == default || request.TerminaEn <= request.IniciaEn)
            return ValidationData.Fail(MaintenanceStatus.Invalid, "La hora de fin debe ser posterior a la hora de inicio.");
        if (!TiposValidos.Contains(request.Tipo))
            return ValidationData.Fail(MaintenanceStatus.Invalid, "El tipo de ausencia no es válido.");
        if (!await _negocios.UsuarioTieneAccesoAsync(usuarioId, request.NegocioId, cancellationToken))
            return ValidationData.Fail(MaintenanceStatus.Forbidden, "No tienes acceso a este negocio.");

        var empleado = await _ausencias.ObtenerEmpleadoAsync(request.NegocioId, request.EmpleadoId, cancellationToken);
        if (empleado is null) return ValidationData.Fail(MaintenanceStatus.Invalid, "El empleado no pertenece al negocio o está inactivo.");
        if (await _ausencias.ExisteSolapamientoAsync(request.EmpleadoId, request.IniciaEn, request.TerminaEn, excluirId, cancellationToken))
            return ValidationData.Fail(MaintenanceStatus.Conflict, "La ausencia se solapa con otra ausencia activa del empleado.");

        var citas = await _ausencias.ObtenerCitasBloqueantesAsync(request.EmpleadoId, request.IniciaEn, request.TerminaEn, cancellationToken);
        if (citas.Any(cita => InicioBloqueado(cita) < request.TerminaEn && request.IniciaEn < FinBloqueado(cita)))
            return ValidationData.Fail(MaintenanceStatus.Conflict, "La ausencia coincide con una cita activa o sus buffers. Reprograma o cancela la cita primero.");

        return new(MaintenanceStatus.Success, null, empleado);
    }

    private static DateTime InicioBloqueado(Cita cita) => cita.Inicio.AddMinutes(-cita.ServiciosCita.Sum(item => item.BufferAntesMinutos));
    private static DateTime FinBloqueado(Cita cita) => cita.Fin.AddMinutes(cita.ServiciosCita.Sum(item => item.BufferDespuesMinutos));
    private static string? Normalize(string? value) => InputNormalizer.OptionalText(value);
    private static AusenciaResponse Map(AusenciasEmpleado item) => Map(item, item.Empleado);
    private static AusenciaResponse Map(AusenciasEmpleado item, Empleado empleado) => new(item.Id, empleado.NegocioId, item.EmpleadoId, $"{empleado.Nombre} {empleado.Apellido}", item.IniciaEn, item.TerminaEn, item.Tipo, item.Motivo, item.Estado, item.CreadoEn);

    private sealed record ValidationData(MaintenanceStatus Status, string? Error, Empleado? Empleado = null)
    {
        public static ValidationData Fail(MaintenanceStatus status, string error) => new(status, error);
    }
}
