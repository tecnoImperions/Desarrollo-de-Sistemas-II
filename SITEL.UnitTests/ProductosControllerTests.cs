using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SITEL.Server.Controllers;
using SITEL.Server.Data;
using SITEL.Shared.DTOs;
using SITEL.Shared.Entities;
using SITEL.Shared.Enums;
using Xunit;

namespace SITEL.UnitTests
{
    public class ProductosControllerTests
    {
        private static AppDbContext CrearContexto()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase($"SitelUnit_Productos_{Guid.NewGuid()}")
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;
            return new AppDbContext(options);
        }

        [Theory]
        [InlineData(10, 5, true, 15)]
        [InlineData(10, -10, true, 0)]
        [InlineData(10, -11, false, 10)]
        [InlineData(10, 0, false, 10)]
        public async Task AjustarStock_ValoresLimite_DebeValidarStockNoNegativo(
            int stockInicial, int delta, bool debeAceptar, int stockEsperado)
        {
            await using var db = CrearContexto();
            var prod = new Producto
            {
                CodigoSku = "SKU-TEST-01",
                Nombre = "Producto Prueba",
                Categoria = CategoriaProducto.Abarrotes,
                UnidadMedida = "Cajas",
                StockActual = stockInicial,
                StockMinimo = 2,
                Activo = true
            };
            db.Productos.Add(prod);
            await db.SaveChangesAsync();

            var controller = new ProductosController(db);
            var res = await controller.AjustarStock(prod.Id, new AjusteStockDto
            {
                ProductoId = prod.Id,
                Cantidad = delta,
                Motivo = "Prueba de valores límite"
            });

            if (debeAceptar)
                Assert.IsType<OkObjectResult>(res);
            else
                Assert.IsType<BadRequestObjectResult>(res);

            var actualizado = await db.Productos.FindAsync(prod.Id);
            Assert.NotNull(actualizado);
            Assert.Equal(stockEsperado, actualizado!.StockActual);
        }
    }
}
