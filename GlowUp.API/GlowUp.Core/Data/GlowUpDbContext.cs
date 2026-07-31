using System;
using System.Collections.Generic;
using GlowUpRD.API.Models;
using Microsoft.EntityFrameworkCore;

namespace GlowUpRD.API.Data;

public partial class GlowUpDbContext : DbContext
{
    public GlowUpDbContext(DbContextOptions<GlowUpDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AusenciasEmpleado> AusenciasEmpleados { get; set; }

    public virtual DbSet<CategoriasServicio> CategoriasServicios { get; set; }

    public virtual DbSet<Cita> Citas { get; set; }

    public virtual DbSet<Cliente> Clientes { get; set; }

    public virtual DbSet<ClientesNegocio> ClientesNegocios { get; set; }

    public virtual DbSet<Empleado> Empleados { get; set; }

    public virtual DbSet<HorariosEmpleado> HorariosEmpleados { get; set; }

    public virtual DbSet<HorariosNegocio> HorariosNegocios { get; set; }

    public virtual DbSet<MiembrosNegocio> MiembrosNegocios { get; set; }

    public virtual DbSet<Negocio> Negocios { get; set; }

    public virtual DbSet<Notificacion> Notificaciones { get; set; }

    public virtual DbSet<Pago> Pagos { get; set; }

    public virtual DbSet<PlanesSuscripcion> PlanesSuscripcions { get; set; }

    public virtual DbSet<RegistroAuditoria> RegistrosAuditoria { get; set; }

    public virtual DbSet<Resena> Resenas { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Servicio> Servicios { get; set; }

    public virtual DbSet<ServicioCita> ServiciosCita { get; set; }

    public virtual DbSet<ServiciosEmpleado> ServiciosEmpleados { get; set; }

    public virtual DbSet<Sucursal> Sucursales { get; set; }

    public virtual DbSet<SuscripcionesNegocio> SuscripcionesNegocios { get; set; }

    public virtual DbSet<Usuario> Usuarios { get; set; }

    public virtual DbSet<UsuariosRole> UsuariosRoles { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AusenciasEmpleado>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("ausencias_empleado_pkey");

            entity.ToTable("ausencias_empleado");

            entity.HasIndex(e => e.EmpleadoId, "idx_ausencias_empleado_empleado");

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.CreadoEn)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("creado_en");
            entity.Property(e => e.EmpleadoId).HasColumnName("empleado_id");
            entity.Property(e => e.Estado)
                .HasDefaultValueSql("'scheduled'::text")
                .HasColumnName("estado");
            entity.Property(e => e.IniciaEn)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("inicia_en");
            entity.Property(e => e.Motivo)
                .HasMaxLength(255)
                .HasColumnName("motivo");
            entity.Property(e => e.TerminaEn)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("termina_en");

            entity.HasOne(d => d.Empleado).WithMany(p => p.AusenciasEmpleados)
                .HasForeignKey(d => d.EmpleadoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("ausencias_empleado_empleado_id_fkey");
        });

        modelBuilder.Entity<CategoriasServicio>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("categorias_servicio_pkey");

            entity.ToTable("categorias_servicio");

            entity.HasIndex(e => new { e.NegocioId, e.Nombre }, "categorias_servicio_negocio_id_nombre_key").IsUnique();

            entity.HasIndex(e => e.NegocioId, "idx_categorias_servicio_negocio");

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.Activo)
                .HasDefaultValue(true)
                .HasColumnName("activo");
            entity.Property(e => e.Descripcion).HasColumnName("descripcion");
            entity.Property(e => e.NegocioId).HasColumnName("negocio_id");
            entity.Property(e => e.Nombre)
                .HasMaxLength(100)
                .HasColumnName("nombre");
            entity.Property(e => e.Orden)
                .HasDefaultValue(0)
                .HasColumnName("orden");

            entity.HasOne(d => d.Negocio).WithMany(p => p.CategoriasServicios)
                .HasForeignKey(d => d.NegocioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("categorias_servicio_negocio_id_fkey");
        });

        modelBuilder.Entity<Cita>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("citas_pkey");

            entity.ToTable("citas");

            entity.HasIndex(e => e.ClienteId, "idx_citas_cliente");

            entity.HasIndex(e => e.ClienteNegocioId, "idx_citas_cliente_negocio");

            entity.HasIndex(e => e.EmpleadoId, "idx_citas_empleado");

            entity.HasIndex(e => e.Estado, "idx_citas_estado");

            entity.HasIndex(e => e.FechaCita, "idx_citas_fecha");

            entity.HasIndex(e => e.NegocioId, "idx_citas_negocio");

            entity.HasIndex(e => new { e.EmpleadoId, e.Inicio, e.Fin }, "idx_citas_rango_tiempo");

            entity.HasIndex(e => e.SucursalId, "idx_citas_sucursal");

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.ActualizadoEn)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("actualizado_en");
            entity.Property(e => e.ClienteId).HasColumnName("cliente_id");
            entity.Property(e => e.ClienteNegocioId).HasColumnName("cliente_negocio_id");
            entity.Property(e => e.CreadoEn)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("creado_en");
            entity.Property(e => e.EmpleadoId).HasColumnName("empleado_id");
            entity.Property(e => e.Estado)
                .HasDefaultValueSql("'pending'::text")
                .HasColumnName("estado");
            entity.Property(e => e.FechaCita).HasColumnName("fecha_cita");
            entity.Property(e => e.Fin)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("fin");
            entity.Property(e => e.Inicio)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("inicio");
            entity.Property(e => e.MotivoCancelacion)
                .HasMaxLength(255)
                .HasColumnName("motivo_cancelacion");
            entity.Property(e => e.NegocioId).HasColumnName("negocio_id");
            entity.Property(e => e.Notas).HasColumnName("notas");
            entity.Property(e => e.SucursalId).HasColumnName("sucursal_id");
            entity.Property(e => e.Total)
                .HasPrecision(10, 2)
                .HasColumnName("total");

            entity.HasOne(d => d.Cliente).WithMany(p => p.Cita)
                .HasForeignKey(d => d.ClienteId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("citas_cliente_id_fkey");

            entity.HasOne(d => d.ClienteNegocio).WithMany(p => p.Cita)
                .HasForeignKey(d => d.ClienteNegocioId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("citas_cliente_negocio_id_fkey");

            entity.HasOne(d => d.Empleado).WithMany(p => p.Cita)
                .HasForeignKey(d => d.EmpleadoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("citas_empleado_id_fkey");

            entity.HasOne(d => d.Negocio).WithMany(p => p.Cita)
                .HasForeignKey(d => d.NegocioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("citas_negocio_id_fkey");

            entity.HasOne(d => d.Sucursal).WithMany(p => p.Cita)
                .HasForeignKey(d => d.SucursalId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("citas_sucursal_id_fkey");
        });

        modelBuilder.Entity<Cliente>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("clientes_pkey");

            entity.ToTable("clientes");

            entity.HasIndex(e => e.Correo, "idx_clientes_correo");

            entity.HasIndex(e => e.Telefono, "idx_clientes_telefono");

            entity.HasIndex(e => e.UsuarioId, "idx_clientes_usuario");

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.ActualizadoEn)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("actualizado_en");
            entity.Property(e => e.Apellido)
                .HasMaxLength(100)
                .HasColumnName("apellido");
            entity.Property(e => e.Correo)
                .HasMaxLength(150)
                .HasColumnName("correo");
            entity.Property(e => e.CreadoEn)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("creado_en");
            entity.Property(e => e.FechaNacimiento).HasColumnName("fecha_nacimiento");
            entity.Property(e => e.Genero)
                .HasDefaultValueSql("'not_specified'::text")
                .HasColumnName("genero");
            entity.Property(e => e.Nombre)
                .HasMaxLength(100)
                .HasColumnName("nombre");
            entity.Property(e => e.Notas).HasColumnName("notas");
            entity.Property(e => e.Telefono)
                .HasMaxLength(30)
                .HasColumnName("telefono");
            entity.Property(e => e.UsuarioId).HasColumnName("usuario_id");

            entity.HasOne(d => d.Usuario).WithMany(p => p.Clientes)
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("clientes_usuario_id_fkey");
        });

        modelBuilder.Entity<ClientesNegocio>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("clientes_negocio_pkey");

            entity.ToTable("clientes_negocio");

            entity.HasIndex(e => new { e.NegocioId, e.ClienteId }, "clientes_negocio_negocio_id_cliente_id_key").IsUnique();

            entity.HasIndex(e => e.ClienteId, "idx_clientes_negocio_cliente");

            entity.HasIndex(e => e.NegocioId, "idx_clientes_negocio_negocio");

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.ClienteId).HasColumnName("cliente_id");
            entity.Property(e => e.CreadoEn)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("creado_en");
            entity.Property(e => e.Estado)
                .HasDefaultValueSql("'active'::text")
                .HasColumnName("estado");
            entity.Property(e => e.NegocioId).HasColumnName("negocio_id");
            entity.Property(e => e.NotasInternas).HasColumnName("notas_internas");
            entity.Property(e => e.PrimeraVisitaEn)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("primera_visita_en");
            entity.Property(e => e.TotalVisitas)
                .HasDefaultValue(0)
                .HasColumnName("total_visitas");
            entity.Property(e => e.UltimaVisitaEn)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("ultima_visita_en");

            entity.HasOne(d => d.Cliente).WithMany(p => p.ClientesNegocios)
                .HasForeignKey(d => d.ClienteId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("clientes_negocio_cliente_id_fkey");

            entity.HasOne(d => d.Negocio).WithMany(p => p.ClientesNegocios)
                .HasForeignKey(d => d.NegocioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("clientes_negocio_negocio_id_fkey");
        });

        modelBuilder.Entity<Empleado>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("empleados_pkey");

            entity.ToTable("empleados");

            entity.HasIndex(e => e.Estado, "idx_empleados_estado");

            entity.HasIndex(e => e.NegocioId, "idx_empleados_negocio");

            entity.HasIndex(e => e.SucursalId, "idx_empleados_sucursal");

            entity.HasIndex(e => e.UsuarioId, "idx_empleados_usuario");

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.ActualizadoEn)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("actualizado_en");
            entity.Property(e => e.Apellido)
                .HasMaxLength(100)
                .HasColumnName("apellido");
            entity.Property(e => e.Biografia).HasColumnName("biografia");
            entity.Property(e => e.Correo)
                .HasMaxLength(150)
                .HasColumnName("correo");
            entity.Property(e => e.CreadoEn)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("creado_en");
            entity.Property(e => e.Estado)
                .HasDefaultValueSql("'active'::text")
                .HasColumnName("estado");
            entity.Property(e => e.FotoUrl)
                .HasMaxLength(500)
                .HasColumnName("foto_url");
            entity.Property(e => e.NegocioId).HasColumnName("negocio_id");
            entity.Property(e => e.Nombre)
                .HasMaxLength(100)
                .HasColumnName("nombre");
            entity.Property(e => e.Puesto)
                .HasMaxLength(100)
                .HasColumnName("puesto");
            entity.Property(e => e.SucursalId).HasColumnName("sucursal_id");
            entity.Property(e => e.Telefono)
                .HasMaxLength(30)
                .HasColumnName("telefono");
            entity.Property(e => e.UsuarioId).HasColumnName("usuario_id");

            entity.HasOne(d => d.Negocio).WithMany(p => p.Empleados)
                .HasForeignKey(d => d.NegocioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("empleados_negocio_id_fkey");

            entity.HasOne(d => d.Sucursal).WithMany(p => p.Empleados)
                .HasForeignKey(d => d.SucursalId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("empleados_sucursal_id_fkey");

            entity.HasOne(d => d.Usuario).WithMany(p => p.Empleados)
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("empleados_usuario_id_fkey");
        });

        modelBuilder.Entity<HorariosEmpleado>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("horarios_empleado_pkey");

            entity.ToTable("horarios_empleado");

            entity.HasIndex(e => new { e.EmpleadoId, e.DiaSemana }, "horarios_empleado_empleado_id_dia_semana_key").IsUnique();

            entity.HasIndex(e => e.EmpleadoId, "idx_horarios_empleado_empleado");

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.Activo)
                .HasDefaultValue(true)
                .HasColumnName("activo");
            entity.Property(e => e.DiaSemana).HasColumnName("dia_semana");
            entity.Property(e => e.EmpleadoId).HasColumnName("empleado_id");
            entity.Property(e => e.IniciaA).HasColumnName("inicia_a");
            entity.Property(e => e.TerminaA).HasColumnName("termina_a");

            entity.HasOne(d => d.Empleado).WithMany(p => p.HorariosEmpleados)
                .HasForeignKey(d => d.EmpleadoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("horarios_empleado_empleado_id_fkey");
        });

        modelBuilder.Entity<HorariosNegocio>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("horarios_negocio_pkey");

            entity.ToTable("horarios_negocio");

            entity.HasIndex(e => new { e.SucursalId, e.DiaSemana }, "horarios_negocio_sucursal_id_dia_semana_key").IsUnique();

            entity.HasIndex(e => e.SucursalId, "idx_horarios_negocio_sucursal");

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.AbreA).HasColumnName("abre_a");
            entity.Property(e => e.Cerrado)
                .HasDefaultValue(false)
                .HasColumnName("cerrado");
            entity.Property(e => e.CierraA).HasColumnName("cierra_a");
            entity.Property(e => e.DiaSemana).HasColumnName("dia_semana");
            entity.Property(e => e.SucursalId).HasColumnName("sucursal_id");

            entity.HasOne(d => d.Sucursal).WithMany(p => p.HorariosNegocios)
                .HasForeignKey(d => d.SucursalId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("horarios_negocio_sucursal_id_fkey");
        });

        modelBuilder.Entity<MiembrosNegocio>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("miembros_negocio_pkey");

            entity.ToTable("miembros_negocio");

            entity.HasIndex(e => e.NegocioId, "idx_miembros_negocio_negocio");

            entity.HasIndex(e => e.SucursalId, "idx_miembros_negocio_sucursal");

            entity.HasIndex(e => e.UsuarioId, "idx_miembros_negocio_usuario");

            entity.HasIndex(e => new { e.NegocioId, e.UsuarioId }, "miembros_negocio_negocio_id_usuario_id_key").IsUnique();

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.CreadoEn)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("creado_en");
            entity.Property(e => e.Estado)
                .HasDefaultValueSql("'active'::text")
                .HasColumnName("estado");
            entity.Property(e => e.NegocioId).HasColumnName("negocio_id");
            entity.Property(e => e.RolMiembro).HasColumnName("rol_miembro");
            entity.Property(e => e.SucursalId).HasColumnName("sucursal_id");
            entity.Property(e => e.UsuarioId).HasColumnName("usuario_id");

            entity.HasOne(d => d.Negocio).WithMany(p => p.MiembrosNegocios)
                .HasForeignKey(d => d.NegocioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("miembros_negocio_negocio_id_fkey");

            entity.HasOne(d => d.Sucursal).WithMany(p => p.MiembrosNegocios)
                .HasForeignKey(d => d.SucursalId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("miembros_negocio_sucursal_id_fkey");

            entity.HasOne(d => d.Usuario).WithMany(p => p.MiembrosNegocios)
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("miembros_negocio_usuario_id_fkey");
        });

        modelBuilder.Entity<Negocio>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("negocios_pkey");

            entity.ToTable("negocios");

            entity.HasIndex(e => e.UsuarioPropietarioId, "idx_negocios_propietario");

            entity.HasIndex(e => e.Slug, "slug").IsUnique();

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.ActualizadoEn)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("actualizado_en");
            entity.Property(e => e.Correo)
                .HasMaxLength(150)
                .HasColumnName("correo");
            entity.Property(e => e.CreadoEn)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("creado_en");
            entity.Property(e => e.Descripcion).HasColumnName("descripcion");
            entity.Property(e => e.Estado)
                .HasDefaultValueSql("'active'::text")
                .HasColumnName("estado");
            entity.Property(e => e.LogoUrl)
                .HasMaxLength(500)
                .HasColumnName("logo_url");
            entity.Property(e => e.Nombre)
                .HasMaxLength(150)
                .HasColumnName("nombre");
            entity.Property(e => e.Rnc)
                .HasMaxLength(30)
                .HasColumnName("rnc");
            entity.Property(e => e.Slug)
                .HasMaxLength(180)
                .HasColumnName("slug");
            entity.Property(e => e.Telefono)
                .HasMaxLength(30)
                .HasColumnName("telefono");
            entity.Property(e => e.TipoNegocio)
                .HasDefaultValueSql("'mixed'::text")
                .HasColumnName("tipo_negocio");
            entity.Property(e => e.UsuarioPropietarioId).HasColumnName("usuario_propietario_id");

            entity.HasOne(d => d.UsuarioPropietario).WithMany(p => p.Negocios)
                .HasForeignKey(d => d.UsuarioPropietarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("negocios_usuario_propietario_id_fkey");
        });

        modelBuilder.Entity<Notificacion>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("notificaciones_pkey");

            entity.ToTable("notificaciones");

            entity.HasIndex(e => e.CitaId, "idx_notificaciones_cita");

            entity.HasIndex(e => e.Estado, "idx_notificaciones_estado");

            entity.HasIndex(e => e.NegocioId, "idx_notificaciones_negocio");

            entity.HasIndex(e => e.UsuarioId, "idx_notificaciones_usuario");

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.Canal).HasColumnName("canal");
            entity.Property(e => e.CitaId).HasColumnName("cita_id");
            entity.Property(e => e.CreadoEn)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("creado_en");
            entity.Property(e => e.EnviadoEn)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("enviado_en");
            entity.Property(e => e.Estado)
                .HasDefaultValueSql("'pending'::text")
                .HasColumnName("estado");
            entity.Property(e => e.LeidoEn)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("leido_en");
            entity.Property(e => e.Mensaje).HasColumnName("mensaje");
            entity.Property(e => e.NegocioId).HasColumnName("negocio_id");
            entity.Property(e => e.Tipo)
                .HasMaxLength(100)
                .HasColumnName("tipo");
            entity.Property(e => e.Titulo)
                .HasMaxLength(150)
                .HasColumnName("titulo");
            entity.Property(e => e.UsuarioId).HasColumnName("usuario_id");

            entity.HasOne(d => d.Cita).WithMany(p => p.Notificaciones)
                .HasForeignKey(d => d.CitaId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("notificaciones_cita_id_fkey");

            entity.HasOne(d => d.Negocio).WithMany(p => p.Notificaciones)
                .HasForeignKey(d => d.NegocioId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("notificaciones_negocio_id_fkey");

            entity.HasOne(d => d.Usuario).WithMany(p => p.Notificaciones)
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("notificaciones_usuario_id_fkey");
        });

        modelBuilder.Entity<Pago>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pagos_pkey");

            entity.ToTable("pagos");

            entity.HasIndex(e => e.CitaId, "idx_pagos_cita");

            entity.HasIndex(e => e.Estado, "idx_pagos_estado");

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.CitaId).HasColumnName("cita_id");
            entity.Property(e => e.CreadoEn)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("creado_en");
            entity.Property(e => e.Estado)
                .HasDefaultValueSql("'pending'::text")
                .HasColumnName("estado");
            entity.Property(e => e.Metodo).HasColumnName("metodo");
            entity.Property(e => e.Monto)
                .HasPrecision(10, 2)
                .HasColumnName("monto");
            entity.Property(e => e.PagadoEn)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("pagado_en");
            entity.Property(e => e.ReferenciaTransaccion)
                .HasMaxLength(150)
                .HasColumnName("referencia_transaccion");

            entity.HasOne(d => d.Cita).WithMany(p => p.Pagos)
                .HasForeignKey(d => d.CitaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("pagos_cita_id_fkey");
        });

        modelBuilder.Entity<PlanesSuscripcion>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("planes_suscripcion_pkey");

            entity.ToTable("planes_suscripcion");

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.Activo)
                .HasDefaultValue(true)
                .HasColumnName("activo");
            entity.Property(e => e.CreadoEn)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("creado_en");
            entity.Property(e => e.Descripcion).HasColumnName("descripcion");
            entity.Property(e => e.MaxEmpleados)
                .HasDefaultValue(3)
                .HasColumnName("max_empleados");
            entity.Property(e => e.MaxServicios)
                .HasDefaultValue(20)
                .HasColumnName("max_servicios");
            entity.Property(e => e.MaxSucursales)
                .HasDefaultValue(1)
                .HasColumnName("max_sucursales");
            entity.Property(e => e.Nombre)
                .HasMaxLength(100)
                .HasColumnName("nombre");
            entity.Property(e => e.PermiteNotificaciones)
                .HasDefaultValue(true)
                .HasColumnName("permite_notificaciones");
            entity.Property(e => e.PermiteReportes)
                .HasDefaultValue(false)
                .HasColumnName("permite_reportes");
            entity.Property(e => e.PrecioMensual)
                .HasPrecision(10, 2)
                .HasColumnName("precio_mensual");
        });

        modelBuilder.Entity<RegistroAuditoria>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("registros_auditoria_pkey");

            entity.ToTable("registros_auditoria");

            entity.HasIndex(e => new { e.EntidadNombre, e.EntidadId }, "idx_registros_auditoria_entidad");

            entity.HasIndex(e => e.NegocioId, "idx_registros_auditoria_negocio");

            entity.HasIndex(e => e.UsuarioId, "idx_registros_auditoria_usuario");

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.Accion)
                .HasMaxLength(100)
                .HasColumnName("accion");
            entity.Property(e => e.AgenteUsuario)
                .HasMaxLength(500)
                .HasColumnName("agente_usuario");
            entity.Property(e => e.CreadoEn)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("creado_en");
            entity.Property(e => e.DireccionIp)
                .HasMaxLength(45)
                .HasColumnName("direccion_ip");
            entity.Property(e => e.EntidadId).HasColumnName("entidad_id");
            entity.Property(e => e.EntidadNombre)
                .HasMaxLength(100)
                .HasColumnName("entidad_nombre");
            entity.Property(e => e.NegocioId).HasColumnName("negocio_id");
            entity.Property(e => e.UsuarioId).HasColumnName("usuario_id");
            entity.Property(e => e.ValoresAnteriores)
                .HasColumnType("jsonb")
                .HasColumnName("valores_anteriores");
            entity.Property(e => e.ValoresNuevos)
                .HasColumnType("jsonb")
                .HasColumnName("valores_nuevos");

            entity.HasOne(d => d.Negocio).WithMany(p => p.RegistrosAuditoria)
                .HasForeignKey(d => d.NegocioId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("registros_auditoria_negocio_id_fkey");

            entity.HasOne(d => d.Usuario).WithMany(p => p.RegistrosAuditoria)
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("registros_auditoria_usuario_id_fkey");
        });

        modelBuilder.Entity<Resena>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("resenas_pkey");

            entity.ToTable("resenas");

            entity.HasIndex(e => e.CitaId, "cita_id").IsUnique();

            entity.HasIndex(e => e.ClienteId, "idx_resenas_cliente");

            entity.HasIndex(e => e.NegocioId, "idx_resenas_negocio");

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.Calificacion).HasColumnName("calificacion");
            entity.Property(e => e.CitaId).HasColumnName("cita_id");
            entity.Property(e => e.ClienteId).HasColumnName("cliente_id");
            entity.Property(e => e.Comentario).HasColumnName("comentario");
            entity.Property(e => e.CreadoEn)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("creado_en");
            entity.Property(e => e.NegocioId).HasColumnName("negocio_id");

            entity.HasOne(d => d.Cita).WithOne(p => p.Resena)
                .HasForeignKey<Resena>(d => d.CitaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("resenas_cita_id_fkey");

            entity.HasOne(d => d.Cliente).WithMany(p => p.Resenas)
                .HasForeignKey(d => d.ClienteId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("resenas_cliente_id_fkey");

            entity.HasOne(d => d.Negocio).WithMany(p => p.Resenas)
                .HasForeignKey(d => d.NegocioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("resenas_negocio_id_fkey");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("roles_pkey");

            entity.ToTable("roles");

            entity.HasIndex(e => e.Nombre, "nombre").IsUnique();

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.CreadoEn)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("creado_en");
            entity.Property(e => e.Descripcion)
                .HasMaxLength(255)
                .HasColumnName("descripcion");
            entity.Property(e => e.Nombre)
                .HasMaxLength(50)
                .HasColumnName("nombre");
        });

        modelBuilder.Entity<Servicio>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("servicios_pkey");

            entity.ToTable("servicios");

            entity.HasIndex(e => e.Activo, "idx_servicios_activo");

            entity.HasIndex(e => e.CategoriaId, "idx_servicios_categoria");

            entity.HasIndex(e => e.NegocioId, "idx_servicios_negocio");

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.Activo)
                .HasDefaultValue(true)
                .HasColumnName("activo");
            entity.Property(e => e.ActualizadoEn)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("actualizado_en");
            entity.Property(e => e.BufferAntesMinutos)
                .HasDefaultValue(0)
                .HasColumnName("buffer_antes_minutos");
            entity.Property(e => e.BufferDespuesMinutos)
                .HasDefaultValue(0)
                .HasColumnName("buffer_despues_minutos");
            entity.Property(e => e.CategoriaId).HasColumnName("categoria_id");
            entity.Property(e => e.CreadoEn)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("creado_en");
            entity.Property(e => e.Descripcion).HasColumnName("descripcion");
            entity.Property(e => e.DuracionMinutos).HasColumnName("duracion_minutos");
            entity.Property(e => e.NegocioId).HasColumnName("negocio_id");
            entity.Property(e => e.Nombre)
                .HasMaxLength(150)
                .HasColumnName("nombre");
            entity.Property(e => e.Precio)
                .HasPrecision(10, 2)
                .HasColumnName("precio");

            entity.HasOne(d => d.Categoria).WithMany(p => p.Servicios)
                .HasForeignKey(d => d.CategoriaId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("servicios_categoria_id_fkey");

            entity.HasOne(d => d.Negocio).WithMany(p => p.Servicios)
                .HasForeignKey(d => d.NegocioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("servicios_negocio_id_fkey");
        });

        modelBuilder.Entity<ServicioCita>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("servicios_cita_pkey");

            entity.ToTable("servicios_cita");

            entity.HasIndex(e => e.CitaId, "idx_servicios_cita_cita");

            entity.HasIndex(e => e.ServicioId, "idx_servicios_cita_servicio");

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.CitaId).HasColumnName("cita_id");
            entity.Property(e => e.DuracionMinutos).HasColumnName("duracion_minutos");
            entity.Property(e => e.NombreServicio)
                .HasMaxLength(150)
                .HasColumnName("nombre_servicio");
            entity.Property(e => e.Precio)
                .HasPrecision(10, 2)
                .HasColumnName("precio");
            entity.Property(e => e.ServicioId).HasColumnName("servicio_id");

            entity.HasOne(d => d.Cita).WithMany(p => p.ServiciosCita)
                .HasForeignKey(d => d.CitaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("servicios_cita_cita_id_fkey");

            entity.HasOne(d => d.Servicio).WithMany(p => p.ServiciosCita)
                .HasForeignKey(d => d.ServicioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("servicios_cita_servicio_id_fkey");
        });

        modelBuilder.Entity<ServiciosEmpleado>(entity =>
        {
            entity.HasKey(e => new { e.EmpleadoId, e.ServicioId }).HasName("servicios_empleado_pkey");

            entity.ToTable("servicios_empleado");

            entity.HasIndex(e => e.ServicioId, "idx_servicios_empleado_servicio");

            entity.Property(e => e.EmpleadoId).HasColumnName("empleado_id");
            entity.Property(e => e.ServicioId).HasColumnName("servicio_id");
            entity.Property(e => e.CreadoEn)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("creado_en");

            entity.HasOne(d => d.Empleado).WithMany(p => p.ServiciosEmpleados)
                .HasForeignKey(d => d.EmpleadoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("servicios_empleado_empleado_id_fkey");

            entity.HasOne(d => d.Servicio).WithMany(p => p.ServiciosEmpleados)
                .HasForeignKey(d => d.ServicioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("servicios_empleado_servicio_id_fkey");
        });

        modelBuilder.Entity<Sucursal>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("sucursales_pkey");

            entity.ToTable("sucursales");

            entity.HasIndex(e => e.NegocioId, "idx_sucursales_negocio");

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.ActualizadoEn)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("actualizado_en");
            entity.Property(e => e.Ciudad)
                .HasMaxLength(100)
                .HasColumnName("ciudad");
            entity.Property(e => e.CreadoEn)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("creado_en");
            entity.Property(e => e.Direccion)
                .HasMaxLength(255)
                .HasColumnName("direccion");
            entity.Property(e => e.EsPrincipal)
                .HasDefaultValue(false)
                .HasColumnName("es_principal");
            entity.Property(e => e.Estado)
                .HasDefaultValueSql("'active'::text")
                .HasColumnName("estado");
            entity.Property(e => e.Latitud)
                .HasPrecision(10, 7)
                .HasColumnName("latitud");
            entity.Property(e => e.Longitud)
                .HasPrecision(10, 7)
                .HasColumnName("longitud");
            entity.Property(e => e.NegocioId).HasColumnName("negocio_id");
            entity.Property(e => e.Nombre)
                .HasMaxLength(150)
                .HasColumnName("nombre");
            entity.Property(e => e.Pais)
                .HasMaxLength(100)
                .HasDefaultValueSql("'Republica Dominicana'::character varying")
                .HasColumnName("pais");
            entity.Property(e => e.Provincia)
                .HasMaxLength(100)
                .HasColumnName("provincia");
            entity.Property(e => e.Telefono)
                .HasMaxLength(30)
                .HasColumnName("telefono");

            entity.HasOne(d => d.Negocio).WithMany(p => p.Sucursales)
                .HasForeignKey(d => d.NegocioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("sucursales_negocio_id_fkey");
        });

        modelBuilder.Entity<SuscripcionesNegocio>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("suscripciones_negocio_pkey");

            entity.ToTable("suscripciones_negocio");

            entity.HasIndex(e => e.Estado, "idx_suscripciones_negocio_estado");

            entity.HasIndex(e => e.NegocioId, "idx_suscripciones_negocio_negocio");

            entity.HasIndex(e => e.PlanId, "idx_suscripciones_negocio_plan");

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.CreadoEn)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("creado_en");
            entity.Property(e => e.Estado)
                .HasDefaultValueSql("'trial'::text")
                .HasColumnName("estado");
            entity.Property(e => e.FinalizaEn)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("finaliza_en");
            entity.Property(e => e.IniciadaEn)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("iniciada_en");
            entity.Property(e => e.NegocioId).HasColumnName("negocio_id");
            entity.Property(e => e.PlanId).HasColumnName("plan_id");
            entity.Property(e => e.ProximoCobroEn)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("proximo_cobro_en");

            entity.HasOne(d => d.Negocio).WithMany(p => p.SuscripcionesNegocios)
                .HasForeignKey(d => d.NegocioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("suscripciones_negocio_negocio_id_fkey");

            entity.HasOne(d => d.Plan).WithMany(p => p.SuscripcionesNegocios)
                .HasForeignKey(d => d.PlanId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("suscripciones_negocio_plan_id_fkey");
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("usuarios_pkey");

            entity.ToTable("usuarios");

            entity.HasIndex(e => e.Correo, "correo").IsUnique();

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.ActualizadoEn)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("actualizado_en");
            entity.Property(e => e.Apellido)
                .HasMaxLength(100)
                .HasColumnName("apellido");
            entity.Property(e => e.ContrasenaHash)
                .HasMaxLength(255)
                .HasColumnName("contrasena_hash");
            entity.Property(e => e.Correo)
                .HasMaxLength(150)
                .HasColumnName("correo");
            entity.Property(e => e.CreadoEn)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("creado_en");
            entity.Property(e => e.Estado)
                .HasDefaultValueSql("'active'::text")
                .HasColumnName("estado");
            entity.Property(e => e.Nombre)
                .HasMaxLength(100)
                .HasColumnName("nombre");
            entity.Property(e => e.Telefono)
                .HasMaxLength(30)
                .HasColumnName("telefono");
            entity.Property(e => e.UltimoLoginEn)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("ultimo_login_en");
            entity.Property(e => e.VerificadoEn)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("verificado_en");
        });

        modelBuilder.Entity<UsuariosRole>(entity =>
        {
            entity.HasKey(e => new { e.UsuarioId, e.RolId }).HasName("usuarios_roles_pkey");

            entity.ToTable("usuarios_roles");

            entity.HasIndex(e => e.RolId, "idx_usuarios_roles_rol");

            entity.Property(e => e.UsuarioId).HasColumnName("usuario_id");
            entity.Property(e => e.RolId).HasColumnName("rol_id");
            entity.Property(e => e.CreadoEn)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("creado_en");

            entity.HasOne(d => d.Rol).WithMany(p => p.UsuariosRoles)
                .HasForeignKey(d => d.RolId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("usuarios_roles_rol_id_fkey");

            entity.HasOne(d => d.Usuario).WithMany(p => p.UsuariosRoles)
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("usuarios_roles_usuario_id_fkey");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
