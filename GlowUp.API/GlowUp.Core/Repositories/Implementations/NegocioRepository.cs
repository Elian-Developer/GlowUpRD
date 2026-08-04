using GlowUpRD.API.Data;
using GlowUpRD.API.Models;
using GlowUpRD.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GlowUpRD.API.Repositories.Implementations;

public sealed class NegocioRepository : INegocioRepository
{
    private readonly GlowUpDbContext _context;
    public NegocioRepository(GlowUpDbContext context) => _context = context;

    public Task<bool> SlugExisteAsync(string slug, CancellationToken cancellationToken = default) =>
        _context.Negocios.AnyAsync(negocio => negocio.Slug == slug, cancellationToken);

    public Task<Negocio?> ObtenerAsync(long id, CancellationToken cancellationToken = default) =>
        _context.Negocios.AsNoTracking().FirstOrDefaultAsync(negocio => negocio.Id == id, cancellationToken);

    public Task<Negocio?> ObtenerParaEditarAsync(long id, CancellationToken cancellationToken = default) =>
        _context.Negocios.FirstOrDefaultAsync(negocio => negocio.Id == id, cancellationToken);

    public Task<Negocio?> ObtenerPerfilAsync(long id, CancellationToken cancellationToken = default) =>
        _context.Negocios.AsNoTracking()
            .Include(negocio => negocio.FeriadosNegocios)
            .Include(negocio => negocio.Sucursales)
            .ThenInclude(sucursal => sucursal.HorariosNegocios)
            .FirstOrDefaultAsync(negocio => negocio.Id == id, cancellationToken);

    public Task<Negocio?> ObtenerPerfilParaEditarAsync(long id, CancellationToken cancellationToken = default) =>
        _context.Negocios
            .Include(negocio => negocio.FeriadosNegocios)
            .Include(negocio => negocio.Sucursales)
            .ThenInclude(sucursal => sucursal.HorariosNegocios)
            .FirstOrDefaultAsync(negocio => negocio.Id == id, cancellationToken);

    public Task<bool> UsuarioEsPropietarioAsync(long usuarioId, long negocioId, CancellationToken cancellationToken = default) =>
        _context.Negocios.AnyAsync(negocio => negocio.Id == negocioId && negocio.Estado == "active" && negocio.UsuarioPropietarioId == usuarioId, cancellationToken);

    public Task<bool> UsuarioTieneAccesoAsync(long usuarioId, long negocioId, CancellationToken cancellationToken = default) =>
        _context.Negocios.AnyAsync(negocio => negocio.Id == negocioId && negocio.Estado == "active" &&
            (negocio.UsuarioPropietarioId == usuarioId || negocio.MiembrosNegocios.Any(member => member.UsuarioId == usuarioId && member.Estado == "active")), cancellationToken);

    public Task<bool> TieneCitasBloqueantesEnFechaAsync(long negocioId, DateOnly fecha, CancellationToken cancellationToken = default) =>
        _context.Citas.AnyAsync(cita => cita.NegocioId == negocioId && cita.FechaCita == fecha &&
            (cita.Estado == "pending" || cita.Estado == "confirmed" || cita.Estado == "completed"), cancellationToken);

    public Task<List<MiembrosNegocio>> ObtenerMiembrosAsync(long negocioId, CancellationToken cancellationToken = default) =>
        _context.MiembrosNegocios.AsNoTracking().Include(item => item.Usuario)
            .Where(item => item.NegocioId == negocioId && item.Estado == "active")
            .OrderBy(item => item.Usuario.Nombre).ToListAsync(cancellationToken);

    public async Task RegistrarAsync(Negocio negocio, Usuario propietario, MiembrosNegocio miembro, CancellationToken cancellationToken = default)
    {
        _context.Negocios.Add(negocio);
        _context.Usuarios.Add(propietario);
        _context.MiembrosNegocios.Add(miembro);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task AgregarMiembroAsync(Usuario usuario, MiembrosNegocio miembro, CancellationToken cancellationToken = default)
    {
        _context.Usuarios.Add(usuario);
        _context.MiembrosNegocios.Add(miembro);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public void EliminarFeriados(IEnumerable<FeriadoNegocio> feriados) => _context.FeriadosNegocios.RemoveRange(feriados);

    public async Task GuardarCambiosAsync(CancellationToken cancellationToken = default) => await _context.SaveChangesAsync(cancellationToken);
}
