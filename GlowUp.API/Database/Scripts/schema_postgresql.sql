-- GlowUp RD - esquema PostgreSQL (equivalente al esquema MySQL original de glowuprd_db)
-- Tablas y columnas en espanol. Los valores de los campos de estado (enum en MySQL)
-- se mantienen en ingles (pending/active/etc.) porque asi los consume el codigo actual
-- (CitaService, Dashboard.jsx); solo se tradujeron los nombres de tabla/columna.
--
-- Uso:
--   psql -h localhost -U postgres -d glowuprd_db -f schema_postgresql.sql

begin;

-- ---------------------------------------------------------------------------
-- Funcion generica para mantener "actualizado_en" al hacer UPDATE
-- (equivalente a "ON UPDATE CURRENT_TIMESTAMP" de MySQL, que Postgres no tiene)
-- ---------------------------------------------------------------------------
create or replace function set_actualizado_en()
returns trigger as $$
begin
    new.actualizado_en = now();
    return new;
end;
$$ language plpgsql;

-- ---------------------------------------------------------------------------
-- usuarios (users)
-- ---------------------------------------------------------------------------
create table usuarios (
    id                  bigint generated always as identity primary key,
    nombre              varchar(100) not null,
    apellido            varchar(100) not null,
    correo              varchar(150) not null,
    telefono            varchar(30),
    contrasena_hash     varchar(255) not null,
    estado              text not null default 'active'
                            check (estado in ('active', 'inactive', 'blocked')),
    verificado_en       timestamp,
    ultimo_login_en     timestamp,
    creado_en           timestamp not null default now(),
    actualizado_en      timestamp
);

create unique index correo on usuarios (correo);

create trigger trg_usuarios_actualizado_en
    before update on usuarios
    for each row execute function set_actualizado_en();

-- ---------------------------------------------------------------------------
-- roles
-- ---------------------------------------------------------------------------
create table roles (
    id                  bigint generated always as identity primary key,
    nombre              varchar(50) not null,
    descripcion         varchar(255),
    creado_en           timestamp not null default now()
);

create unique index nombre on roles (nombre);

-- ---------------------------------------------------------------------------
-- usuarios_roles (user_roles)
-- ---------------------------------------------------------------------------
create table usuarios_roles (
    usuario_id          bigint not null references usuarios (id),
    rol_id              bigint not null references roles (id),
    creado_en           timestamp not null default now(),
    primary key (usuario_id, rol_id)
);

create index idx_usuarios_roles_rol on usuarios_roles (rol_id);

-- ---------------------------------------------------------------------------
-- planes_suscripcion (subscription_plans)
-- ---------------------------------------------------------------------------
create table planes_suscripcion (
    id                      bigint generated always as identity primary key,
    nombre                  varchar(100) not null,
    descripcion             text,
    precio_mensual          numeric(10, 2) not null,
    max_sucursales          integer not null default 1,
    max_empleados           integer not null default 3,
    max_servicios           integer not null default 20,
    permite_reportes        boolean not null default false,
    permite_notificaciones  boolean not null default true,
    activo                  boolean not null default true,
    creado_en               timestamp not null default now()
);

-- ---------------------------------------------------------------------------
-- negocios (businesses)
-- ---------------------------------------------------------------------------
create table negocios (
    id                      bigint generated always as identity primary key,
    usuario_propietario_id  bigint not null references usuarios (id),
    nombre                  varchar(150) not null,
    slug                    varchar(180) not null,
    tipo_negocio            text not null default 'mixed'
                                check (tipo_negocio in ('salon', 'barbershop', 'spa', 'mixed')),
    descripcion             text,
    rnc                     varchar(30),
    telefono                varchar(30),
    correo                  varchar(150),
    logo_url                varchar(500),
    estado                  text not null default 'active'
                                check (estado in ('active', 'inactive', 'suspended')),
    creado_en               timestamp not null default now(),
    actualizado_en          timestamp
);

create unique index slug on negocios (slug);
create index idx_negocios_propietario on negocios (usuario_propietario_id);

create trigger trg_negocios_actualizado_en
    before update on negocios
    for each row execute function set_actualizado_en();

-- ---------------------------------------------------------------------------
-- feriados_negocio (business_holidays)
-- ---------------------------------------------------------------------------
create table feriados_negocio (
    id                  bigint generated always as identity primary key,
    negocio_id          bigint not null references negocios (id),
    fecha               date not null,
    nombre              varchar(150) not null,
    creado_en           timestamp not null default now(),
    unique (negocio_id, fecha)
);

create index idx_feriados_negocio_negocio on feriados_negocio (negocio_id);

-- ---------------------------------------------------------------------------
-- sucursales (branches)
-- ---------------------------------------------------------------------------
create table sucursales (
    id                  bigint generated always as identity primary key,
    negocio_id          bigint not null references negocios (id),
    nombre              varchar(150) not null,
    telefono            varchar(30),
    direccion           varchar(255) not null,
    ciudad              varchar(100) not null,
    provincia           varchar(100) not null,
    pais                varchar(100) not null default 'Republica Dominicana',
    latitud             numeric(10, 7),
    longitud            numeric(10, 7),
    es_principal        boolean not null default false,
    estado              text not null default 'active'
                            check (estado in ('active', 'inactive')),
    creado_en           timestamp not null default now(),
    actualizado_en      timestamp
);

create index idx_sucursales_negocio on sucursales (negocio_id);

create trigger trg_sucursales_actualizado_en
    before update on sucursales
    for each row execute function set_actualizado_en();

-- ---------------------------------------------------------------------------
-- horarios_negocio (business_hours)
-- ---------------------------------------------------------------------------
create table horarios_negocio (
    id                  bigint generated always as identity primary key,
    sucursal_id         bigint not null references sucursales (id),
    dia_semana          smallint not null,
    abre_a              time,
    cierra_a            time,
    cerrado             boolean not null default false,
    unique (sucursal_id, dia_semana)
);

create index idx_horarios_negocio_sucursal on horarios_negocio (sucursal_id);

-- ---------------------------------------------------------------------------
-- miembros_negocio (business_members)
-- ---------------------------------------------------------------------------
create table miembros_negocio (
    id                  bigint generated always as identity primary key,
    negocio_id          bigint not null references negocios (id),
    usuario_id          bigint not null references usuarios (id),
    sucursal_id         bigint references sucursales (id) on delete set null,
    rol_miembro         text not null
                            check (rol_miembro in ('owner', 'manager', 'employee', 'receptionist')),
    estado              text not null default 'active'
                            check (estado in ('active', 'inactive')),
    creado_en           timestamp not null default now(),
    unique (negocio_id, usuario_id)
);

create index idx_miembros_negocio_sucursal on miembros_negocio (sucursal_id);
create index idx_miembros_negocio_negocio on miembros_negocio (negocio_id);
create index idx_miembros_negocio_usuario on miembros_negocio (usuario_id);

-- ---------------------------------------------------------------------------
-- clientes (customers)
-- ---------------------------------------------------------------------------
create table clientes (
    id                  bigint generated always as identity primary key,
    usuario_id          bigint references usuarios (id) on delete set null,
    nombre              varchar(100) not null,
    apellido            varchar(100) not null,
    telefono            varchar(30),
    correo              varchar(150),
    fecha_nacimiento    date,
    genero              text default 'not_specified'
                            check (genero in ('female', 'male', 'other', 'not_specified')),
    notas               text,
    creado_en           timestamp not null default now(),
    actualizado_en      timestamp
);

create index idx_clientes_correo on clientes (correo);
create index idx_clientes_telefono on clientes (telefono);
create index idx_clientes_usuario on clientes (usuario_id);

create trigger trg_clientes_actualizado_en
    before update on clientes
    for each row execute function set_actualizado_en();

-- ---------------------------------------------------------------------------
-- clientes_negocio (business_customers)
-- ---------------------------------------------------------------------------
create table clientes_negocio (
    id                  bigint generated always as identity primary key,
    negocio_id          bigint not null references negocios (id),
    cliente_id          bigint not null references clientes (id),
    notas_internas      text,
    primera_visita_en   timestamp,
    ultima_visita_en    timestamp,
    total_visitas       integer not null default 0,
    estado              text not null default 'active'
                            check (estado in ('active', 'inactive', 'blocked')),
    creado_en           timestamp not null default now(),
    unique (negocio_id, cliente_id)
);

create index idx_clientes_negocio_negocio on clientes_negocio (negocio_id);
create index idx_clientes_negocio_cliente on clientes_negocio (cliente_id);

-- ---------------------------------------------------------------------------
-- suscripciones_negocio (business_subscriptions)
-- ---------------------------------------------------------------------------
create table suscripciones_negocio (
    id                  bigint generated always as identity primary key,
    negocio_id          bigint not null references negocios (id),
    plan_id             bigint not null references planes_suscripcion (id),
    estado              text not null default 'trial'
                            check (estado in ('trial', 'active', 'past_due', 'cancelled', 'expired')),
    iniciada_en         timestamp not null,
    finaliza_en         timestamp,
    proximo_cobro_en    timestamp,
    creado_en           timestamp not null default now()
);

create index idx_suscripciones_negocio_negocio on suscripciones_negocio (negocio_id);
create index idx_suscripciones_negocio_plan on suscripciones_negocio (plan_id);
create index idx_suscripciones_negocio_estado on suscripciones_negocio (estado);

-- ---------------------------------------------------------------------------
-- empleados (employees)
-- ---------------------------------------------------------------------------
create table empleados (
    id                  bigint generated always as identity primary key,
    negocio_id          bigint not null references negocios (id),
    sucursal_id         bigint references sucursales (id) on delete set null,
    usuario_id          bigint references usuarios (id) on delete set null,
    nombre              varchar(100) not null,
    apellido            varchar(100) not null,
    telefono            varchar(30),
    correo              varchar(150),
    puesto              varchar(100),
    biografia           text,
    foto_url            varchar(500),
    estado              text not null default 'active'
                            check (estado in ('active', 'inactive', 'on_leave')),
    creado_en           timestamp not null default now(),
    actualizado_en      timestamp
);

create index idx_empleados_sucursal on empleados (sucursal_id);
create index idx_empleados_negocio on empleados (negocio_id);
create index idx_empleados_estado on empleados (estado);
create index idx_empleados_usuario on empleados (usuario_id);

create trigger trg_empleados_actualizado_en
    before update on empleados
    for each row execute function set_actualizado_en();

-- ---------------------------------------------------------------------------
-- horarios_empleado (employee_schedules)
-- ---------------------------------------------------------------------------
create table horarios_empleado (
    id                  bigint generated always as identity primary key,
    empleado_id         bigint not null references empleados (id),
    dia_semana          smallint not null,
    inicia_a            time not null,
    termina_a           time not null,
    activo              boolean not null default true,
    unique (empleado_id, dia_semana, inicia_a, termina_a)
);

create index idx_horarios_empleado_empleado on horarios_empleado (empleado_id);

-- ---------------------------------------------------------------------------
-- categorias_servicio (service_categories)
-- ---------------------------------------------------------------------------
create table categorias_servicio (
    id                  bigint generated always as identity primary key,
    negocio_id          bigint not null references negocios (id),
    nombre              varchar(100) not null,
    descripcion         text,
    orden               integer not null default 0,
    activo              boolean not null default true,
    unique (negocio_id, nombre)
);

create index idx_categorias_servicio_negocio on categorias_servicio (negocio_id);

-- ---------------------------------------------------------------------------
-- servicios (services)
-- ---------------------------------------------------------------------------
create table servicios (
    id                          bigint generated always as identity primary key,
    negocio_id                  bigint not null references negocios (id),
    categoria_id                bigint references categorias_servicio (id) on delete set null,
    nombre                      varchar(150) not null,
    descripcion                 text,
    duracion_minutos            integer not null,
    precio                      numeric(10, 2) not null,
    buffer_antes_minutos        integer not null default 0,
    buffer_despues_minutos      integer not null default 0,
    activo                      boolean not null default true,
    creado_en                   timestamp not null default now(),
    actualizado_en              timestamp
);

create index idx_servicios_activo on servicios (activo);
create index idx_servicios_negocio on servicios (negocio_id);
create index idx_servicios_categoria on servicios (categoria_id);

create trigger trg_servicios_actualizado_en
    before update on servicios
    for each row execute function set_actualizado_en();

-- ---------------------------------------------------------------------------
-- servicios_empleado (employee_services)
-- ---------------------------------------------------------------------------
create table servicios_empleado (
    empleado_id         bigint not null references empleados (id),
    servicio_id         bigint not null references servicios (id),
    creado_en           timestamp not null default now(),
    primary key (empleado_id, servicio_id)
);

create index idx_servicios_empleado_servicio on servicios_empleado (servicio_id);

-- ---------------------------------------------------------------------------
-- ausencias_empleado (employee_time_off)
-- ---------------------------------------------------------------------------
create table ausencias_empleado (
    id                  bigint generated always as identity primary key,
    empleado_id         bigint not null references empleados (id),
    inicia_en           timestamp not null,
    termina_en          timestamp not null,
    motivo              varchar(255),
    estado              text not null default 'scheduled'
                            check (estado in ('scheduled', 'cancelled')),
    creado_en           timestamp not null default now()
);

create index idx_ausencias_empleado_empleado on ausencias_empleado (empleado_id);

-- ---------------------------------------------------------------------------
-- citas (appointments)
-- ---------------------------------------------------------------------------
create table citas (
    id                      bigint generated always as identity primary key,
    negocio_id              bigint not null references negocios (id),
    sucursal_id             bigint not null references sucursales (id),
    cliente_id              bigint not null references clientes (id),
    cliente_negocio_id      bigint references clientes_negocio (id) on delete set null,
    empleado_id             bigint not null references empleados (id),
    fecha_cita              date not null,
    inicio                  timestamp not null,
    fin                     timestamp not null,
    estado                  text not null default 'pending'
                                check (estado in ('pending', 'confirmed', 'completed', 'cancelled', 'no_show')),
    motivo_cancelacion      varchar(255),
    notas                   text,
    total                   numeric(10, 2) not null,
    creado_en               timestamp not null default now(),
    actualizado_en          timestamp
);

create index idx_citas_sucursal on citas (sucursal_id);
create index idx_citas_negocio on citas (negocio_id);
create index idx_citas_cliente_negocio on citas (cliente_negocio_id);
create index idx_citas_cliente on citas (cliente_id);
create index idx_citas_fecha on citas (fecha_cita);
create index idx_citas_empleado on citas (empleado_id);
create index idx_citas_estado on citas (estado);
create index idx_citas_rango_tiempo on citas (empleado_id, inicio, fin);

create trigger trg_citas_actualizado_en
    before update on citas
    for each row execute function set_actualizado_en();

-- ---------------------------------------------------------------------------
-- servicios_cita (appointment_services)
-- ---------------------------------------------------------------------------
create table servicios_cita (
    id                  bigint generated always as identity primary key,
    cita_id             bigint not null references citas (id),
    servicio_id         bigint not null references servicios (id),
    nombre_servicio     varchar(150) not null,
    duracion_minutos    integer not null,
    precio              numeric(10, 2) not null
);

create index idx_servicios_cita_cita on servicios_cita (cita_id);
create index idx_servicios_cita_servicio on servicios_cita (servicio_id);

-- ---------------------------------------------------------------------------
-- pagos (payments)
-- ---------------------------------------------------------------------------
create table pagos (
    id                          bigint generated always as identity primary key,
    cita_id                     bigint not null references citas (id),
    monto                       numeric(10, 2) not null,
    metodo                      text not null
                                    check (metodo in ('cash', 'card', 'transfer', 'online')),
    estado                      text not null default 'pending'
                                    check (estado in ('pending', 'paid', 'failed', 'refunded')),
    referencia_transaccion      varchar(150),
    pagado_en                   timestamp,
    creado_en                   timestamp not null default now()
);

create index idx_pagos_cita on pagos (cita_id);
create index idx_pagos_estado on pagos (estado);

-- ---------------------------------------------------------------------------
-- resenas (reviews)
-- ---------------------------------------------------------------------------
create table resenas (
    id                  bigint generated always as identity primary key,
    cita_id             bigint not null references citas (id),
    cliente_id          bigint not null references clientes (id),
    negocio_id          bigint not null references negocios (id),
    calificacion        smallint not null,
    comentario          text,
    creado_en           timestamp not null default now()
);

create unique index cita_id on resenas (cita_id);
create index idx_resenas_negocio on resenas (negocio_id);
create index idx_resenas_cliente on resenas (cliente_id);

-- ---------------------------------------------------------------------------
-- notificaciones (notifications)
-- ---------------------------------------------------------------------------
create table notificaciones (
    id                  bigint generated always as identity primary key,
    usuario_id          bigint references usuarios (id) on delete set null,
    negocio_id          bigint references negocios (id) on delete set null,
    cita_id             bigint references citas (id) on delete set null,
    canal               text not null
                            check (canal in ('email', 'sms', 'whatsapp', 'system')),
    tipo                varchar(100) not null,
    titulo              varchar(150) not null,
    mensaje             text not null,
    estado              text not null default 'pending'
                            check (estado in ('pending', 'sent', 'failed', 'read')),
    enviado_en          timestamp,
    leido_en            timestamp,
    creado_en           timestamp not null default now()
);

create index idx_notificaciones_cita on notificaciones (cita_id);
create index idx_notificaciones_negocio on notificaciones (negocio_id);
create index idx_notificaciones_estado on notificaciones (estado);
create index idx_notificaciones_usuario on notificaciones (usuario_id);

-- ---------------------------------------------------------------------------
-- registros_auditoria (audit_logs)
-- ---------------------------------------------------------------------------
create table registros_auditoria (
    id                  bigint generated always as identity primary key,
    usuario_id          bigint references usuarios (id) on delete set null,
    negocio_id          bigint references negocios (id) on delete set null,
    accion              varchar(100) not null,
    entidad_nombre      varchar(100) not null,
    entidad_id          bigint,
    valores_anteriores  jsonb,
    valores_nuevos      jsonb,
    direccion_ip        varchar(45),
    agente_usuario      varchar(500),
    creado_en           timestamp not null default now()
);

create index idx_registros_auditoria_negocio on registros_auditoria (negocio_id);
create index idx_registros_auditoria_entidad on registros_auditoria (entidad_nombre, entidad_id);
create index idx_registros_auditoria_usuario on registros_auditoria (usuario_id);

commit;
