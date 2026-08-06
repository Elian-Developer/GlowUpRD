using GlowUpRD.API.Models;

namespace GlowUpRD.API.Repositories.Interfaces;

public interface IAusenciaRepository
{
    Task<List<AusenciasEmpleado>> BuscarAsync(long negocioId, DateTime desde, DateTime hasta, long? empleadoId, bool incluirCanceladas, CancellationToken cancellationToken = default);
    Task<AusenciasEmpleado?> ObtenerAsync(long id, bool tracking, CancellationToken cancellationToken = default);
    Task<Empleado?> ObtenerEmpleadoAsync(long negocioId, long empleadoId, CancellationToken cancellationToken = default);
    Task<bool> ExisteSolapamientoAsync(long empleadoId, DateTime iniciaEn, DateTime terminaEn, long? excluirId, CancellationToken cancellationToken = default);
    Task<List<Cita>> ObtenerCitasBloqueantesAsync(long empleadoId, DateTime iniciaEn, DateTime terminaEn, CancellationToken cancellationToken = default);
    Task AgregarAsync(AusenciasEmpleado ausencia, CancellationToken cancellationToken = default);
    Task GuardarCambiosAsync(CancellationToken cancellationToken = default);
}
