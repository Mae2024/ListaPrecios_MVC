
using ListaPrecios_MVC.DATA;
using ListaPrecios_MVC.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ListaPrecios_MVC.Controllers
{
    public class PedidosController : Controller
    {
        private readonly PreciosContext _context;

        public PedidosController(PreciosContext context)
        {
            _context = context;
        }

        // GET: /Pedidos/Nuevo
        [HttpGet]
        public async Task<IActionResult> Nuevo()
        {
            await CargarClientesEnViewBag();
            return View();
        }

        // POST: /Pedidos/Nuevo
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Nuevo(string? cliente, string? nuevoNombre, string? nuevaDireccion)
        {
            Cliente? clienteElegido = null;

            if (!string.IsNullOrWhiteSpace(nuevoNombre))
            {
                // Opción 1: crear un cliente nuevo
                string nombre = nuevoNombre.Trim();

                bool yaExiste = await _context.Clientes.AnyAsync(c => c.Nombre == nombre);
                if (yaExiste)
                {
                    ModelState.AddModelError("", "Ese cliente ya existe. Elegilo de la lista de arriba.");
                }
                else
                {
                    clienteElegido = new Cliente();
                    clienteElegido.Nombre = nombre;
                    if (nuevaDireccion != null)
                    {
                        clienteElegido.Direccion = nuevaDireccion.Trim();
                    }
                    _context.Clientes.Add(clienteElegido);
                }
            }
            else if (!string.IsNullOrWhiteSpace(cliente))
            {
                // Opción 2: un cliente que ya existe
                string nombre = cliente.Trim();
                clienteElegido = await _context.Clientes.FirstOrDefaultAsync(c => c.Nombre == nombre);

                if (clienteElegido == null)
                {
                    ModelState.AddModelError("", "No encontré ese cliente. Elegilo de las sugerencias o crealo abajo.");
                }
            }
            else
            {
                ModelState.AddModelError("", "Elegí un cliente o creá uno nuevo.");
            }

            if (!ModelState.IsValid || clienteElegido == null)
            {
                await CargarClientesEnViewBag();
                return View();
            }

            Pedido pedido = new Pedido();
            pedido.Cliente = clienteElegido;
            pedido.Fecha = DateTime.Now;
            _context.Pedidos.Add(pedido);

            await _context.SaveChangesAsync();   // guarda el cliente nuevo (si hay) y el pedido

            return RedirectToAction("Editar", new { id = pedido.Id });
        }

        // GET: /Pedidos/Editar/5   (en la 3b se completa con los artículos)
        [HttpGet]
        public async Task<IActionResult> Editar(int id)
        {
            Pedido? pedido = await _context.Pedidos
                .Include(p => p.Cliente)
                .Include(p => p.Items)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pedido == null)
            {
                return NotFound();
            }

            return View(pedido);
        }

        private async Task CargarClientesEnViewBag()
        {
            ViewBag.Clientes = await _context.Clientes
                .OrderBy(c => c.Nombre)
                .Select(c => c.Nombre)
                .ToListAsync();
        }
    }
}