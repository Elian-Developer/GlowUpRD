using GlowUpRD.API.Data;
using GlowUpRD.API.Models;
using GlowUpRD.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GlowUpRD.API.Services.Implementations;

public sealed class DemoDataService : IDemoDataService
{
    private readonly GlowUpDbContext _context;

    public DemoDataService(GlowUpDbContext context) => _context = context;

    public async Task<bool> ProvisionForBusinessAsync(long negocioId, CancellationToken cancellationToken = default)
    {
        var negocio = await _context.Negocios
            .Include(item => item.Sucursales)
            .SingleOrDefaultAsync(item => item.Id == negocioId, cancellationToken)
            ?? throw new InvalidOperationException("El negocio que se intentó poblar no existe.");

        if (negocio.Estado != "active") return false;
        if (await HasOperationalDataAsync(negocioId, cancellationToken)) return false;

        var now = DateTime.UtcNow;
        var today = DateTime.Today;
        var sucursal = negocio.Sucursales
            .OrderByDescending(item => item.EsPrincipal)
            .ThenBy(item => item.Id)
            .FirstOrDefault();

        if (sucursal is null)
        {
            sucursal = new Sucursal
            {
                NegocioId = negocioId,
                Nombre = "Sucursal principal",
                Direccion = "Av. Winston Churchill 101",
                Ciudad = "Santo Domingo",
                Provincia = "Distrito Nacional",
                Pais = "República Dominicana",
                EsPrincipal = true,
                Estado = "active",
                CreadoEn = now,
            };
            _context.Sucursales.Add(sucursal);
            await _context.SaveChangesAsync(cancellationToken);
        }

        var hasSchedule = await _context.HorariosNegocios
            .AnyAsync(item => item.SucursalId == sucursal.Id, cancellationToken);
        if (!hasSchedule)
        {
            _context.HorariosNegocios.AddRange(Enumerable.Range(1, 6).Select(day => new HorariosNegocio
            {
                SucursalId = sucursal.Id,
                DiaSemana = (short)day,
                AbreA = new TimeOnly(8, 0),
                CierraA = new TimeOnly(18, 0),
                Cerrado = false,
            }));
        }

        var empleados = new[]
        {
            CreateEmployee("Sofía", "Reyes", "Estilista", "809-555-0111"),
            CreateEmployee("Mateo", "Díaz", "Barbero", "809-555-0112"),
            CreateEmployee("Laura", "Gómez", "Colorista", "809-555-0113"),
        };
        _context.Empleados.AddRange(empleados);

        var categorias = new[]
        {
            CreateCategory("Corte y estilo", "Cortes, peinados y barbería", 1),
            CreateCategory("Color", "Coloración y mechas", 2),
            CreateCategory("Tratamientos", "Cuidado y reparación", 3),
        };
        _context.CategoriasServicios.AddRange(categorias);
        await _context.SaveChangesAsync(cancellationToken);

        var servicios = new[]
        {
            CreateService(categorias[0], "Corte clásico", "Corte y lavado", 45, 750m),
            CreateService(categorias[0], "Peinado", "Peinado para cualquier ocasión", 45, 500m),
            CreateService(categorias[0], "Corte y barba", "Corte, perfilado y acabado", 60, 900m),
            CreateService(categorias[1], "Color completo", "Aplicación de color y brillo", 120, 2200m),
            CreateService(categorias[2], "Hidratación profunda", "Tratamiento nutritivo", 60, 1200m),
        };
        _context.Servicios.AddRange(servicios);
        await _context.SaveChangesAsync(cancellationToken);

        _context.ServiciosEmpleados.AddRange(
            Assign(empleados[0], servicios[0]), Assign(empleados[0], servicios[1]), Assign(empleados[0], servicios[4]),
            Assign(empleados[1], servicios[0]), Assign(empleados[1], servicios[2]),
            Assign(empleados[2], servicios[3]), Assign(empleados[2], servicios[4]));

        var clientes = new[]
        {
            CreateClient("Camila", "Martínez", "809-555-0201"), CreateClient("Valentina", "Pérez", "809-555-0202"),
            CreateClient("Daniel", "Santos", "809-555-0203"), CreateClient("María", "Ramos", "809-555-0204"),
            CreateClient("José", "Castillo", "809-555-0205"), CreateClient("Elena", "Vega", "809-555-0206"),
            CreateClient("Andrés", "Torres", "809-555-0207"), CreateClient("Lucía", "Méndez", "809-555-0208"),
        };
        _context.Clientes.AddRange(clientes);
        await _context.SaveChangesAsync(cancellationToken);

        var relaciones = clientes.Select((cliente, index) => new ClientesNegocio
        {
            NegocioId = negocioId,
            ClienteId = cliente.Id,
            PrimeraVisitaEn = today.AddDays(-(index + 14)),
            UltimaVisitaEn = today.AddDays(-(index % 6)),
            TotalVisitas = index + 1,
            Estado = "active",
            CreadoEn = now,
        }).ToArray();
        _context.ClientesNegocios.AddRange(relaciones);
        await _context.SaveChangesAsync(cancellationToken);

        _context.Citas.AddRange(
            CreateAppointment(0, 0, 0, "confirmed", 9),
            CreateAppointment(1, 1, 2, "pending", 10),
            CreateAppointment(2, 2, 3, "completed", 11),
            CreateAppointment(3, 0, 1, "confirmed", 13),
            CreateAppointment(4, 1, 0, "pending", 15));
        await _context.SaveChangesAsync(cancellationToken);

        return true;

        Empleado CreateEmployee(string nombre, string apellido, string puesto, string telefono) => new()
        {
            NegocioId = negocioId, SucursalId = sucursal.Id, Nombre = nombre, Apellido = apellido,
            Correo = $"{nombre.ToLowerInvariant()}@demo.glowup", Telefono = telefono, Puesto = puesto,
            Estado = "active", CreadoEn = now,
        };

        CategoriasServicio CreateCategory(string nombre, string descripcion, int orden) => new()
        {
            NegocioId = negocioId, Nombre = nombre, Descripcion = descripcion, Orden = orden, Activo = true,
        };

        Servicio CreateService(CategoriasServicio categoria, string nombre, string descripcion, int duracion, decimal precio) => new()
        {
            NegocioId = negocioId, CategoriaId = categoria.Id, Nombre = nombre, Descripcion = descripcion,
            DuracionMinutos = duracion, Precio = precio, Activo = true, CreadoEn = now,
        };

        ServiciosEmpleado Assign(Empleado empleado, Servicio servicio) => new()
        {
            EmpleadoId = empleado.Id, ServicioId = servicio.Id, CreadoEn = now,
        };

        Cliente CreateClient(string nombre, string apellido, string telefono) => new()
        {
            Nombre = nombre, Apellido = apellido, Correo = $"{nombre.ToLowerInvariant()}@demo.glowup",
            Telefono = telefono, CreadoEn = now,
        };

        Cita CreateAppointment(int customerIndex, int employeeIndex, int serviceIndex, string status, int hour)
        {
            var service = servicios[serviceIndex];
            var start = today.AddHours(hour);
            return new Cita
            {
                NegocioId = negocioId,
                SucursalId = sucursal.Id,
                ClienteId = clientes[customerIndex].Id,
                ClienteNegocioId = relaciones[customerIndex].Id,
                EmpleadoId = empleados[employeeIndex].Id,
                FechaCita = DateOnly.FromDateTime(today),
                Inicio = start,
                Fin = start.AddMinutes(service.DuracionMinutos),
                Estado = status,
                Notas = "Cita de demostración",
                Total = service.Precio,
                CreadoEn = now,
                ServiciosCita =
                [
                    new ServicioCita
                    {
                        ServicioId = service.Id,
                        NombreServicio = service.Nombre,
                        DuracionMinutos = service.DuracionMinutos,
                        Precio = service.Precio,
                    },
                ],
            };
        }
    }

    public async Task<int> ProvisionEmptyBusinessesAsync(CancellationToken cancellationToken = default)
    {
        var businessIds = await _context.Negocios.AsNoTracking()
            .Where(item => item.Estado == "active")
            .OrderBy(item => item.Id)
            .Select(item => item.Id)
            .ToListAsync(cancellationToken);

        var provisioned = 0;
        foreach (var businessId in businessIds)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                if (await ProvisionForBusinessAsync(businessId, cancellationToken)) provisioned++;
                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
            finally
            {
                _context.ChangeTracker.Clear();
            }
        }

        return provisioned;
    }

    private async Task<bool> HasOperationalDataAsync(long negocioId, CancellationToken cancellationToken) =>
        await _context.CategoriasServicios.AnyAsync(item => item.NegocioId == negocioId, cancellationToken)
        || await _context.Servicios.AnyAsync(item => item.NegocioId == negocioId, cancellationToken)
        || await _context.Empleados.AnyAsync(item => item.NegocioId == negocioId, cancellationToken)
        || await _context.ClientesNegocios.AnyAsync(item => item.NegocioId == negocioId, cancellationToken)
        || await _context.Citas.AnyAsync(item => item.NegocioId == negocioId, cancellationToken);
}
