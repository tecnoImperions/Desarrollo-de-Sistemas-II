using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SITEL.Server.Data;
using SITEL.Shared.DTOs;
using SITEL.Shared.Entities;
using SITEL.Shared.Enums;

namespace SITEL.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductosController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ProductosController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/productos
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Producto>>> GetProductos(
            [FromQuery] CategoriaProducto? categoria = null,
            [FromQuery] bool soloDisponibles = false)
        {
            try
            {
                var query = _context.Productos.AsNoTracking().AsQueryable();

                if (categoria.HasValue)
                    query = query.Where(p => p.Categoria == categoria.Value);

                if (soloDisponibles)
                    query = query.Where(p => p.Activo && p.StockActual > 0);

                var productos = await query
                    .OrderBy(p => p.Categoria)
                    .ThenBy(p => p.Nombre)
                    .ToListAsync();

                return Ok(productos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al consultar el inventario de mercancías.", detalle = ex.Message });
            }
        }

        // GET: api/productos/5
        [HttpGet("{id:int}")]
        public async Task<ActionResult<Producto>> GetProductoPorId(int id)
        {
            var producto = await _context.Productos.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
            if (producto == null)
                return NotFound(new { mensaje = $"El producto con ID {id} no existe." });

            return Ok(producto);
        }

        // POST: api/productos
        [HttpPost]
        public async Task<ActionResult<Producto>> CrearProducto([FromBody] Producto producto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { mensaje = "Datos de producto inválidos. Revise los campos requeridos." });

            producto.CodigoSku = producto.CodigoSku.Trim().ToUpperInvariant();
            producto.Nombre = producto.Nombre.Trim();
            producto.Descripcion = producto.Descripcion.Trim();
            producto.UnidadMedida = string.IsNullOrWhiteSpace(producto.UnidadMedida) ? "Cajas" : producto.UnidadMedida.Trim();

            if (producto.StockActual < 0)
                return BadRequest(new { mensaje = "El stock inicial no puede ser negativo." });

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                bool existeSku = await _context.Productos
                    .AnyAsync(p => p.CodigoSku.ToUpper() == producto.CodigoSku);

                if (existeSku)
                    return BadRequest(new { mensaje = $"Ya existe una mercancía registrada con el código SKU '{producto.CodigoSku}'." });

                _context.Productos.Add(producto);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return CreatedAtAction(nameof(GetProductoPorId), new { id = producto.Id }, producto);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { mensaje = "Error al registrar el ítem en inventario.", detalle = ex.Message });
            }
        }

        // PUT: api/productos/5
        [HttpPut("{id:int}")]
        public async Task<IActionResult> ActualizarProducto(int id, [FromBody] Producto productoActualizado)
        {
            if (productoActualizado.StockActual < 0)
                return BadRequest(new { mensaje = "El stock disponible no puede ser menor a cero." });

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var producto = await _context.Productos.FindAsync(id);
                if (producto == null)
                    return NotFound(new { mensaje = $"No se encontró el producto con ID {id}." });

                var skuNormalizado = productoActualizado.CodigoSku.Trim().ToUpperInvariant();
                bool skuDuplicado = await _context.Productos
                    .AnyAsync(p => p.Id != id && p.CodigoSku.ToUpper() == skuNormalizado);

                if (skuDuplicado)
                    return BadRequest(new { mensaje = $"El código SKU '{skuNormalizado}' ya pertenece a otro producto." });

                producto.CodigoSku = skuNormalizado;
                producto.Nombre = productoActualizado.Nombre.Trim();
                producto.Descripcion = productoActualizado.Descripcion.Trim();
                producto.Categoria = productoActualizado.Categoria;
                producto.UnidadMedida = productoActualizado.UnidadMedida.Trim();
                producto.StockActual = productoActualizado.StockActual;
                producto.StockMinimo = productoActualizado.StockMinimo;
                producto.PesoUnitarioKg = productoActualizado.PesoUnitarioKg;
                producto.Activo = productoActualizado.Activo;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(producto);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { mensaje = "Error al actualizar el producto en inventario.", detalle = ex.Message });
            }
        }

        // PATCH: api/productos/5/stock
        [HttpPatch("{id:int}/stock")]
        public async Task<IActionResult> AjustarStock(int id, [FromBody] AjusteStockDto ajuste)
        {
            if (ajuste.Cantidad == 0)
                return BadRequest(new { mensaje = "La cantidad de ajuste debe ser distinta de cero." });

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var producto = await _context.Productos.FindAsync(id);
                if (producto == null)
                    return NotFound(new { mensaje = "Producto no encontrado en el almacén." });

                int nuevoStock = producto.StockActual + ajuste.Cantidad;
                if (nuevoStock < 0)
                {
                    return BadRequest(new
                    {
                        mensaje = $"Stock insuficiente para '{producto.Nombre}'. Stock actual: {producto.StockActual} {producto.UnidadMedida}, salida solicitada: {Math.Abs(ajuste.Cantidad)}."
                    });
                }

                producto.StockActual = nuevoStock;
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new
                {
                    mensaje = $"Stock actualizado correctamente ({producto.StockActual} {producto.UnidadMedida} disponibles).",
                    producto
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { mensaje = "Error transaccional al ajustar el stock.", detalle = ex.Message });
            }
        }

        // DELETE: api/productos/5
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> EliminarProducto(int id)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var producto = await _context.Productos.FindAsync(id);
                if (producto == null)
                    return NotFound(new { mensaje = "El producto solicitado no existe." });

                bool usadoEnEnvios = await _context.DetallesEnvio.AnyAsync(d => d.ProductoId == id);
                if (usadoEnEnvios)
                {
                    return BadRequest(new
                    {
                        mensaje = $"No se puede eliminar '{producto.Nombre}' porque está vinculado a guías de despacho registradas. Puede desactivarlo o ajustar su stock a 0."
                    });
                }

                _context.Productos.Remove(producto);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { mensaje = "Mercancía eliminada del catálogo de inventario." });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { mensaje = "Error al eliminar el producto.", detalle = ex.Message });
            }
        }
    }
}
