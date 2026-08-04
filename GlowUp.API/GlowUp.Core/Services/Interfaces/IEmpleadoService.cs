using GlowUpRD.API.DTOs.Empleados;

namespace GlowUpRD.API.Services.Interfaces;

public interface IEmpleadoService
{
    Task<MaintenanceResult<IReadOnlyList<EmpleadoResponse>>> BuscarAsync(long usuarioId, long negocioId, bool incluirInactivos, CancellationToken cancellationToken = default);
    Task<MaintenanceResult<EmpleadoResponse>> ObtenerAsync(long usuarioId, long id, CancellationToken cancellationToken = default);
    Task<MaintenanceResult<EmpleadoResponse>> CrearAsync(long usuarioId, GuardarEmpleadoRequest request, CancellationToken cancellationToken = default);
    Task<MaintenanceResult<EmpleadoResponse>> ActualizarAsync(long usuarioId, long id, GuardarEmpleadoRequest request, CancellationToken cancellationToken = default);
    Task<MaintenanceResult<bool>> EliminarAsync(long usuarioId, long id, CancellationToken cancellationToken = default);
    Task<int> CompletarHorariosPendientesAsync(CancellationToken cancellationToken = default);
}
