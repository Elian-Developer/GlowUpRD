namespace GlowUpRD.API.Services.Interfaces;

public interface IDemoDataService
{
    Task<bool> ProvisionForBusinessAsync(long negocioId, CancellationToken cancellationToken = default);

    Task<int> ProvisionEmptyBusinessesAsync(CancellationToken cancellationToken = default);
}
