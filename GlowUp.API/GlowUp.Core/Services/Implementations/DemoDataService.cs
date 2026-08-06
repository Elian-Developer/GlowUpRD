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

        if (negocio.Estado != "active" || await HasOperationalDataAsync(negocioId, cancellationToken)) return false;

        var now = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(DateTime.Today);
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

        await EnsureBusinessHoursAsync(sucursal.Id, cancellationToken);
        var festivos = GetUpcomingHolidays(today);
        await EnsureHolidaysAsync(negocioId, sucursal.Id, festivos, now, cancellationToken);
        var fechasFestivas = festivos.Select(item => item.Fecha).ToHashSet();

        var empleados = new[]
        {
            CreateEmployee("Sofía", "Reyes", "Estilista", "+18095550111"),
            CreateEmployee("Mateo", "Díaz", "Barbero", "+18095550112"),
            CreateEmployee("Laura", "Gómez", "Colorista", "+18095550113"),
        };
        _context.Empleados.AddRange(empleados);
        await _context.SaveChangesAsync(cancellationToken);

        // El nuevo modelo requiere pertenencia a la sede y turnos explícitos para
        // que el empleado aparezca como disponible al crear una cita.
        _context.EmpleadosSucursales.AddRange(empleados.Select(empleado => new EmpleadoSucursal
        {
            EmpleadoId = empleado.Id,
            SucursalId = sucursal.Id,
            Estado = "active",
            CreadoEn = now,
        }));
        _context.HorariosEmpleados.AddRange(empleados.SelectMany(empleado => CreateEmployeeHours(empleado.Id, sucursal.Id)));

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
            CreateService(categorias[0], "Corte clásico", "Corte y lavado", 45, 750m, 5, 5),
            CreateService(categorias[0], "Peinado", "Peinado para cualquier ocasión", 45, 500m, 0, 5),
            CreateService(categorias[0], "Corte y barba", "Corte, perfilado y acabado", 60, 900m, 5, 10),
            CreateService(categorias[1], "Color completo", "Aplicación de color y brillo", 120, 2200m, 10, 10),
            CreateService(categorias[2], "Hidratación profunda", "Tratamiento nutritivo", 60, 1200m, 5, 5),
        };
        _context.Servicios.AddRange(servicios);
        await _context.SaveChangesAsync(cancellationToken);

        _context.ServiciosEmpleados.AddRange(
            Assign(empleados[0], servicios[0]), Assign(empleados[0], servicios[1]), Assign(empleados[0], servicios[4]),
            Assign(empleados[1], servicios[0]), Assign(empleados[1], servicios[2]),
            Assign(empleados[2], servicios[3]), Assign(empleados[2], servicios[4]));

        var clientes = new[]
        {
            CreateClient("Camila", "Martínez", "+18095550201"), CreateClient("Valentina", "Pérez", "+18095550202"),
            CreateClient("Daniel", "Santos", "+18095550203"), CreateClient("María", "Ramos", "+18095550204"),
            CreateClient("José", "Castillo", "+18095550205"), CreateClient("Elena", "Vega", "+18095550206"),
            CreateClient("Andrés", "Torres", "+18095550207"), CreateClient("Lucía", "Méndez", "+18095550208"),
        };
        _context.Clientes.AddRange(clientes);
        await _context.SaveChangesAsync(cancellationToken);

        var relaciones = clientes.Select((cliente, index) => new ClientesNegocio
        {
            NegocioId = negocioId,
            ClienteId = cliente.Id,
            PrimeraVisitaEn = today.AddDays(-(index + 14)).ToDateTime(new TimeOnly(10, 0)),
            UltimaVisitaEn = today.AddDays(-(index % 6)).ToDateTime(new TimeOnly(11, 0)),
            TotalVisitas = index + 1,
            Estado = "active",
            CreadoEn = now,
        }).ToArray();
        _context.ClientesNegocios.AddRange(relaciones);
        await _context.SaveChangesAsync(cancellationToken);

        var diaAgenda = FindRegularOpenDate(today, -1, fechasFestivas);
        var diaHistoricoUno = FindRegularOpenDate(diaAgenda.AddDays(-1), -1, fechasFestivas);
        var diaHistoricoDos = FindRegularOpenDate(diaHistoricoUno.AddDays(-1), -1, fechasFestivas);
        var diaHistoricoTres = FindRegularOpenDate(diaHistoricoDos.AddDays(-1), -1, fechasFestivas);
        var proximaCita = FindOpenDate(today.AddDays(1), 1, fechasFestivas);

        _context.Citas.AddRange(
            CreateAppointment(diaHistoricoTres, 5, 0, 0, "completed", 10, 0),
            CreateAppointment(diaHistoricoDos, 6, 2, 3, "completed", 11, 0),
            CreateAppointment(diaHistoricoUno, 0, 1, 2, "confirmed", 9, 30),
            CreateAppointment(diaAgenda, 0, 0, 0, "confirmed", 9, 0),
            CreateAppointment(diaAgenda, 1, 1, 2, "pending", 10, 30),
            CreateAppointment(diaAgenda, 2, 2, 3, "completed", 11, 0),
            CreateAppointment(diaAgenda, 3, 0, 1, "confirmed", 13, 0),
            CreateAppointment(proximaCita, 4, 1, 0, "pending", 10, 0));
        await _context.SaveChangesAsync(cancellationToken);

        return true;

        Empleado CreateEmployee(string nombre, string apellido, string puesto, string telefono) => new()
        {
            NegocioId = negocioId,
            SucursalId = sucursal.Id,
            Nombre = nombre,
            Apellido = apellido,
            Correo = $"{nombre.ToLowerInvariant()}.{negocioId}@demo.glowup",
            Telefono = telefono,
            Puesto = puesto,
            Estado = "active",
            CreadoEn = now,
        };

        CategoriasServicio CreateCategory(string nombre, string descripcion, int orden) => new()
        {
            NegocioId = negocioId,
            Nombre = nombre,
            Descripcion = descripcion,
            Orden = orden,
            Activo = true,
        };

        Servicio CreateService(CategoriasServicio categoria, string nombre, string descripcion, int duracion, decimal precio, int bufferAntes, int bufferDespues) => new()
        {
            NegocioId = negocioId,
            CategoriaId = categoria.Id,
            Nombre = nombre,
            Descripcion = descripcion,
            DuracionMinutos = duracion,
            Precio = precio,
            BufferAntesMinutos = bufferAntes,
            BufferDespuesMinutos = bufferDespues,
            Activo = true,
            CreadoEn = now,
        };

        ServiciosEmpleado Assign(Empleado empleado, Servicio servicio) => new()
        {
            EmpleadoId = empleado.Id,
            ServicioId = servicio.Id,
            CreadoEn = now,
        };

        Cliente CreateClient(string nombre, string apellido, string telefono) => new()
        {
            Nombre = nombre,
            Apellido = apellido,
            Correo = $"{nombre.ToLowerInvariant()}.{negocioId}@demo.glowup",
            Telefono = telefono,
            CreadoEn = now,
        };

        Cita CreateAppointment(DateOnly fecha, int customerIndex, int employeeIndex, int serviceIndex, string status, int hour, int minute)
        {
            var service = servicios[serviceIndex];
            var start = fecha.ToDateTime(new TimeOnly(hour, minute));
            return new Cita
            {
                NegocioId = negocioId,
                SucursalId = sucursal.Id,
                ClienteId = clientes[customerIndex].Id,
                ClienteNegocioId = relaciones[customerIndex].Id,
                EmpleadoId = empleados[employeeIndex].Id,
                FechaCita = fecha,
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
                        BufferAntesMinutos = service.BufferAntesMinutos,
                        BufferDespuesMinutos = service.BufferDespuesMinutos,
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

    private async Task EnsureBusinessHoursAsync(long sucursalId, CancellationToken cancellationToken)
    {
        var existingDays = await _context.HorariosNegocios
            .Where(item => item.SucursalId == sucursalId)
            .Select(item => item.DiaSemana)
            .ToListAsync(cancellationToken);

        var missing = Enumerable.Range(0, 7).Where(day => !existingDays.Contains((short)day)).Select(day => new HorariosNegocio
        {
            SucursalId = sucursalId,
            DiaSemana = (short)day,
            AbreA = day is 0 or 6 ? null : new TimeOnly(8, 0),
            CierraA = day is 0 or 6 ? null : day == 5 ? new TimeOnly(12, 0) : new TimeOnly(18, 0),
            Cerrado = day is 0 or 6,
        });
        _context.HorariosNegocios.AddRange(missing);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureHolidaysAsync(long negocioId, long sucursalId, IReadOnlyList<(DateOnly Fecha, string Nombre)> festivos, DateTime now, CancellationToken cancellationToken)
    {
        var existentes = await _context.FeriadosNegocios
            .Where(item => item.SucursalId == sucursalId)
            .Select(item => item.Fecha)
            .ToListAsync(cancellationToken);
        _context.FeriadosNegocios.AddRange(festivos.Where(item => !existentes.Contains(item.Fecha)).Select(item => new FeriadoNegocio
        {
            NegocioId = negocioId,
            SucursalId = sucursalId,
            Fecha = item.Fecha,
            Nombre = item.Nombre,
            CreadoEn = now,
        }));
        await _context.SaveChangesAsync(cancellationToken);
    }

    private static IReadOnlyList<(DateOnly Fecha, string Nombre)> GetUpcomingHolidays(DateOnly today) =>
    [
        (NextOccurrence(today, 2, 27), "Día de la Independencia Nacional"),
        (NextOccurrence(today, 8, 16), "Día de la Restauración"),
    ];

    private static DateOnly NextOccurrence(DateOnly today, int month, int day)
    {
        var date = new DateOnly(today.Year, month, day);
        return date < today ? date.AddYears(1) : date;
    }

    private static DateOnly FindRegularOpenDate(DateOnly start, int direction, ISet<DateOnly> festivos)
    {
        var date = start;
        while (date.DayOfWeek is DayOfWeek.Friday or DayOfWeek.Saturday or DayOfWeek.Sunday || festivos.Contains(date)) date = date.AddDays(direction);
        return date;
    }

    private static DateOnly FindOpenDate(DateOnly start, int direction, ISet<DateOnly> festivos)
    {
        var date = start;
        while (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday || festivos.Contains(date)) date = date.AddDays(direction);
        return date;
    }

    private static IEnumerable<HorariosEmpleado> CreateEmployeeHours(long empleadoId, long sucursalId) =>
        Enumerable.Range(1, 5).SelectMany(day =>
        {
            var cierre = day == 5 ? new TimeOnly(12, 0) : new TimeOnly(18, 0);
            var turnos = new List<HorariosEmpleado>
            {
                new()
                {
                    EmpleadoId = empleadoId,
                    SucursalId = sucursalId,
                    DiaSemana = (short)day,
                    IniciaA = new TimeOnly(8, 0),
                    TerminaA = cierre <= new TimeOnly(12, 0) ? cierre : new TimeOnly(12, 0),
                    Activo = true,
                },
            };
            if (cierre > new TimeOnly(13, 0))
            {
                turnos.Add(new HorariosEmpleado
                {
                    EmpleadoId = empleadoId,
                    SucursalId = sucursalId,
                    DiaSemana = (short)day,
                    IniciaA = new TimeOnly(13, 0),
                    TerminaA = cierre,
                    Activo = true,
                });
            }
            return turnos;
        });

    private async Task<bool> HasOperationalDataAsync(long negocioId, CancellationToken cancellationToken) =>
        await _context.CategoriasServicios.AnyAsync(item => item.NegocioId == negocioId, cancellationToken)
        || await _context.Servicios.AnyAsync(item => item.NegocioId == negocioId, cancellationToken)
        || await _context.Empleados.AnyAsync(item => item.NegocioId == negocioId, cancellationToken)
        || await _context.ClientesNegocios.AnyAsync(item => item.NegocioId == negocioId, cancellationToken)
        || await _context.Citas.AnyAsync(item => item.NegocioId == negocioId, cancellationToken);
}
