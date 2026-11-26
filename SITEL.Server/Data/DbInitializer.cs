using Microsoft.EntityFrameworkCore;
using SITEL.Shared.Entities;
using SITEL.Shared.Enums;

namespace SITEL.Server.Data
{
    public static class DbInitializer
    {
        public static async Task InitializeAsync(AppDbContext context)
        {
            try
            {
                // 1. Asegurar esquema para tablas de Inventario (Productos y DetallesEnvio) y sanear nulos
                const string ensureInventoryTablesSql = @"
IF OBJECT_ID(N'[dbo].[Productos]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Productos] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [CodigoSku] NVARCHAR(30) NOT NULL,
        [Nombre] NVARCHAR(120) NOT NULL,
        [Descripcion] NVARCHAR(300) NOT NULL DEFAULT N'',
        [Categoria] INT NOT NULL DEFAULT 0,
        [UnidadMedida] NVARCHAR(30) NOT NULL DEFAULT N'Cajas',
        [StockActual] INT NOT NULL DEFAULT 0,
        [StockMinimo] INT NOT NULL DEFAULT 5,
        [PesoUnitarioKg] DECIMAL(18,2) NOT NULL DEFAULT 1.0,
        [Activo] BIT NOT NULL DEFAULT 1
    );
    CREATE UNIQUE INDEX [IX_Productos_CodigoSku] ON [dbo].[Productos] ([CodigoSku]);
END;

IF OBJECT_ID(N'[dbo].[DetallesEnvio]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[DetallesEnvio] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [EnvioId] INT NOT NULL,
        [ProductoId] INT NOT NULL,
        [Cantidad] INT NOT NULL,
        [ObservacionItem] NVARCHAR(MAX) NOT NULL DEFAULT N'',
        CONSTRAINT [FK_DetallesEnvio_Envios_EnvioId] FOREIGN KEY ([EnvioId]) REFERENCES [dbo].[Envios] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_DetallesEnvio_Productos_ProductoId] FOREIGN KEY ([ProductoId]) REFERENCES [dbo].[Productos] ([Id]) ON DELETE NO ACTION
    );
    CREATE INDEX [IX_DetallesEnvio_EnvioId] ON [dbo].[DetallesEnvio] ([EnvioId]);
    CREATE INDEX [IX_DetallesEnvio_ProductoId] ON [dbo].[DetallesEnvio] ([ProductoId]);
END;

IF OBJECT_ID(N'[dbo].[Clientes]', N'U') IS NOT NULL
BEGIN
    UPDATE [dbo].[Clientes] SET
        [NombreRazonSocial] = ISNULL([NombreRazonSocial], N''),
        [DocumentoNit] = ISNULL([DocumentoNit], N''),
        [Correo] = ISNULL([Correo], N''),
        [Telefono] = ISNULL([Telefono], N''),
        [Direccion] = ISNULL([Direccion], N'');
END;

IF OBJECT_ID(N'[dbo].[Envios]', N'U') IS NOT NULL
BEGIN
    UPDATE [dbo].[Envios] SET
        [Guia] = ISNULL([Guia], N''),
        [Destinatario] = ISNULL([Destinatario], N''),
        [DireccionEntrega] = ISNULL([DireccionEntrega], N''),
        [DescripcionCarga] = ISNULL([DescripcionCarga], N''),
        [ReceptorNombre] = ISNULL([ReceptorNombre], N''),
        [MotivoAnulacion] = ISNULL([MotivoAnulacion], N''),
        [AutorizadoPor] = ISNULL([AutorizadoPor], N'');
END;

IF OBJECT_ID(N'[dbo].[Conductores]', N'U') IS NOT NULL AND COL_LENGTH(N'[dbo].[Conductores]', N'UsuarioId') IS NULL
BEGIN
    ALTER TABLE [dbo].[Conductores] ADD [UsuarioId] INT NULL;
    IF OBJECT_ID(N'[dbo].[Usuarios]', N'U') IS NOT NULL
    BEGIN
        ALTER TABLE [dbo].[Conductores] ADD CONSTRAINT [FK_Conductores_Usuarios_UsuarioId] FOREIGN KEY ([UsuarioId]) REFERENCES [dbo].[Usuarios] ([Id]) ON DELETE SET NULL;
    END;
END;

IF OBJECT_ID(N'[dbo].[Clientes]', N'U') IS NOT NULL AND COL_LENGTH(N'[dbo].[Clientes]', N'UsuarioId') IS NULL
BEGIN
    ALTER TABLE [dbo].[Clientes] ADD [UsuarioId] INT NULL;
    IF OBJECT_ID(N'[dbo].[Usuarios]', N'U') IS NOT NULL
    BEGIN
        ALTER TABLE [dbo].[Clientes] ADD CONSTRAINT [FK_Clientes_Usuarios_UsuarioId] FOREIGN KEY ([UsuarioId]) REFERENCES [dbo].[Usuarios] ([Id]) ON DELETE SET NULL;
    END;
END;";

                if (context.Database.IsRelational())
                {
                    await context.Database.ExecuteSqlRawAsync(ensureInventoryTablesSql);
                }

                // 2. Seed Usuarios por Rol (con BCrypt) para pruebas inmediatas de Login y Redirección
                if (!await context.Usuarios.AnyAsync(u => u.Email == "admin@sitel.bo"))
                {
                    context.Usuarios.Add(new Usuario
                    {
                        NombreCompleto = "Ing. Juan Andrés (Administrador General)",
                        Email = "admin@sitel.bo",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123*"),
                        Rol = RolUsuario.Administrador
                    });
                }

                if (!await context.Usuarios.AnyAsync(u => u.Email == "operador@sitel.bo"))
                {
                    context.Usuarios.Add(new Usuario
                    {
                        NombreCompleto = "Lic. Valeria Rojas (Operadora Logística)",
                        Email = "operador@sitel.bo",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Operador123*"),
                        Rol = RolUsuario.OperadorLogistico
                    });
                }

                if (!await context.Usuarios.AnyAsync(u => u.Email == "conductor@sitel.bo"))
                {
                    context.Usuarios.Add(new Usuario
                    {
                        NombreCompleto = "Carlos Andrés Mendoza Vargas",
                        Email = "conductor@sitel.bo",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Conductor123*"),
                        Rol = RolUsuario.Conductor
                    });
                }
                await context.SaveChangesAsync();

                // 3. Seed Conductores iniciales
                if (!await context.Conductores.AnyAsync())
                {
                    context.Conductores.AddRange(
                        new Conductor { Nombres = "Sin Asignar", Apellidos = "(Pendiente RF02)", Documento = "0000000", Licencia = "N/A", Telefono = "-", Disponible = true },
                        new Conductor { Nombres = "Carlos Andrés", Apellidos = "Mendoza Vargas", Documento = "4589210 SC", Licencia = "Cat. C - Pesados", Telefono = "71045892", Disponible = true },
                        new Conductor { Nombres = "Roberto", Apellidos = "Suárez Justiniano", Documento = "5821349 SC", Licencia = "Cat. C - Pesados", Telefono = "76019283", Disponible = true },
                        new Conductor { Nombres = "Miguel Ángel", Apellidos = "Torrez Mamani", Documento = "6391042 LP", Licencia = "Cat. B - Camiones", Telefono = "72184930", Disponible = true }
                    );
                    await context.SaveChangesAsync();
                }

                // Sincronizar usuarios de rol Conductor con sus perfiles en la tabla Conductores
                var usuariosConductores = await context.Usuarios
                    .Where(u => u.Rol == RolUsuario.Conductor)
                    .ToListAsync();

                foreach (var uCond in usuariosConductores)
                {
                    var cond = await context.Conductores.FirstOrDefaultAsync(c => c.UsuarioId == uCond.Id);
                    if (cond == null)
                    {
                        var partes = (uCond.NombreCompleto ?? "").Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                        var primerNombre = partes.Length > 0 ? partes[0] : "Conductor";
                        var resto = partes.Length > 1 ? partes[1] : primerNombre;

                        var coincide = await context.Conductores
                            .FirstOrDefaultAsync(c => c.UsuarioId == null && (c.Nombres.Contains(primerNombre) || c.Apellidos.Contains(resto)));

                        if (coincide != null)
                        {
                            coincide.UsuarioId = uCond.Id;
                        }
                        else
                        {
                            context.Conductores.Add(new Conductor
                            {
                                UsuarioId = uCond.Id,
                                Nombres = primerNombre,
                                Apellidos = resto,
                                Documento = $"{new Random().Next(1000000, 9999999)} SC",
                                Licencia = "Cat. C - Pesados",
                                Telefono = "70000000",
                                Disponible = true
                            });
                        }
                    }
                }
                await context.SaveChangesAsync();

                // 4. Seed Rutas iniciales
                if (!await context.Rutas.AnyAsync())
                {
                    context.Rutas.AddRange(
                        new Ruta { Nombre = "Ruta Pendiente de Asignación", Origen = "Almacén Central", Destino = "Por definir", DistanciaKm = 0 },
                        new Ruta { Nombre = "Corredor Urbano Norte (Parque Industrial - 4to Anillo)", Origen = "Parque Industrial SCZ", Destino = "Av. Banzer / Zona Norte", DistanciaKm = 18.5m },
                        new Ruta { Nombre = "Ruta Metropolitana Sur - Doble Vía La Guardia", Origen = "Almacén Central SCZ", Destino = "Km 9 Doble Vía La Guardia", DistanciaKm = 24.0m },
                        new Ruta { Nombre = "Troncal Interdepartamental SCZ - CBBA", Origen = "Santa Cruz de la Sierra", Destino = "Cochabamba (Quillacollo)", DistanciaKm = 472.0m },
                        new Ruta { Nombre = "Troncal Oriental SCZ - Montero / Warnes", Origen = "Santa Cruz de la Sierra", Destino = "Parque Industrial Warnes / Montero", DistanciaKm = 54.0m }
                    );
                    await context.SaveChangesAsync();
                }

                // 5. Seed Clientes iniciales
                if (!await context.Clientes.AnyAsync())
                {
                    context.Clientes.AddRange(
                        new Cliente
                        {
                            NombreRazonSocial = "Supermercados Fidalga S.R.L.",
                            DocumentoNit = "1028495023",
                            Correo = "logistica@fidalga.com.bo",
                            Telefono = "3-3459000",
                            Direccion = "Av. Cristo Redentor esq. 3er Anillo Externo"
                        },
                        new Cliente
                        {
                            NombreRazonSocial = "Distribuidora Mayorista Andina S.A.",
                            DocumentoNit = "1015928301",
                            Correo = "recepcion@mayoristaandina.bo",
                            Telefono = "75091827",
                            Direccion = "Av. Santos Dumont 4to Anillo, Zona Parque Industrial"
                        },
                        new Cliente
                        {
                            NombreRazonSocial = "Farmacorp S.A. - Centro de Distribución",
                            DocumentoNit = "1029384015",
                            Correo = "abastecimiento@farmacorp.com",
                            Telefono = "3-3551200",
                            Direccion = "Av. Banzer Km 6.5, Centro Logístico Norte"
                        }
                    );
                    await context.SaveChangesAsync();
                }

                // 6. Seed Productos de Inventario (Abarrotes e Insumos)
                if (!await context.Productos.AnyAsync())
                {
                    context.Productos.AddRange(
                        new Producto
                        {
                            CodigoSku = "ABA-ARR-001",
                            Nombre = "Arroz Grano de Oro Seleccionado (Quintal 46Kg)",
                            Descripcion = "Saco de polipropileno laminado contra humedad, lote exportación.",
                            Categoria = CategoriaProducto.Abarrotes,
                            UnidadMedida = "Sacos",
                            StockActual = 120,
                            StockMinimo = 20,
                            PesoUnitarioKg = 46.0m,
                            Activo = true
                        },
                        new Producto
                        {
                            CodigoSku = "ABA-ACE-002",
                            Nombre = "Aceite Vegetal Fino (Caja x 12 botellas de 900ml)",
                            Descripcion = "Caja de cartón corrugado reforzado para transporte terrestre.",
                            Categoria = CategoriaProducto.Abarrotes,
                            UnidadMedida = "Cajas",
                            StockActual = 85,
                            StockMinimo = 15,
                            PesoUnitarioKg = 11.5m,
                            Activo = true
                        },
                        new Producto
                        {
                            CodigoSku = "INS-MED-003",
                            Nombre = "Kit de Insumos Médicos y Bioseguridad Hospitalaria",
                            Descripcion = "Contiene guantes de nitrilo, jeringas descartables y soluciones antisépticas.",
                            Categoria = CategoriaProducto.InsumosMedicos,
                            UnidadMedida = "Cajas",
                            StockActual = 40,
                            StockMinimo = 10,
                            PesoUnitarioKg = 8.2m,
                            Activo = true
                        },
                        new Producto
                        {
                            CodigoSku = "INS-IND-004",
                            Nombre = "Bobina de Film Stretch Industrial Transparente 20 Pulg.",
                            Descripcion = "Insumo de paletizado de alta resistencia para empaque de carga pesada.",
                            Categoria = CategoriaProducto.MaterialEmpaque,
                            UnidadMedida = "Unidades",
                            StockActual = 65,
                            StockMinimo = 12,
                            PesoUnitarioKg = 3.8m,
                            Activo = true
                        },
                        new Producto
                        {
                            CodigoSku = "BEB-LAC-005",
                            Nombre = "Leche Entera UHT Larga Vida (Bandeja x 12 Lts)",
                            Descripcion = "Producto lácteo UHT que no requiere cadena de frío durante tránsito corto.",
                            Categoria = CategoriaProducto.BebidasYLacteos,
                            UnidadMedida = "Bandejas",
                            StockActual = 8,
                            StockMinimo = 15,
                            PesoUnitarioKg = 12.6m,
                            Activo = true
                        }
                    );
                    await context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SITEL DbInitializer] Aviso al inicializar base de datos: {ex.Message}");
            }
        }
    }
}
