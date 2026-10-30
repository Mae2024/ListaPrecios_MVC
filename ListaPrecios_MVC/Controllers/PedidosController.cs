
using ListaPrecios_MVC.DATA;
using ListaPrecios_MVC.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

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




                // GET: /Pedidos/Editar/5?buscar=jamon
        [HttpGet]
        public async Task<IActionResult> Editar(int id, string? buscar)
        {
            Pedido? pedido = await _context.Pedidos
                .Include(p => p.Cliente)
                .Include(p => p.Items)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pedido == null)
            {
                return NotFound();
            }

            List<Articulo> resultados = new List<Articulo>();

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                IQueryable<Articulo> query = _context.Articulos;

                foreach (string palabra in buscar.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                {
                    query = query.Where(a => EF.Functions
                        .Collate(a.Descripcion, "Modern_Spanish_CI_AI")
                        .Contains(palabra));
                }

                resultados = await query
                    .OrderBy(a => a.Descripcion)
                    .Take(30)
                    .ToListAsync();
            }

            ViewBag.Buscar = buscar;
            ViewBag.Resultados = resultados;

            return View(pedido);
        }
       
        
                // POST: /Pedidos/AgregarItem
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AgregarItem(int pedidoId, int articuloId, string? cantidad)
        {
            decimal cantidadNumero;
            bool cantidadValida = LeerDecimal(cantidad, out cantidadNumero);

            if (!cantidadValida || cantidadNumero <= 0)
            {
                TempData["Error"] = "Poné una cantidad mayor a cero.";
                return RedirectToAction("Editar", new { id = pedidoId });
            }

            Pedido? pedido = await _context.Pedidos.FindAsync(pedidoId);
            Articulo? articulo = await _context.Articulos.FindAsync(articuloId);

            if (pedido == null || articulo == null)
            {
                return NotFound();
            }

            PedidoItem item = new PedidoItem();
            item.PedidoId = pedido.Id;
            item.Descripcion = articulo.Descripcion;
            item.Proveedor = articulo.Proveedor;
            item.Cantidad = Math.Round(cantidadNumero, 3);
            item.PrecioUnitario = articulo.PrecioFinal;
            item.SubTotal = Math.Round(item.Cantidad * item.PrecioUnitario, 2);

            _context.PedidoItems.Add(item);
            await _context.SaveChangesAsync();

            TempData["Resultado"] = "Agregado: " + item.Descripcion;
            return RedirectToAction("Editar", new { id = pedidoId });
        }

                
        
                // POST: /Pedidos/QuitarItem
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> QuitarItem(int id)
        {
            PedidoItem? item = await _context.PedidoItems.FindAsync(id);

            if (item == null)
            {
                return NotFound();
            }

            int pedidoId = item.PedidoId;

            _context.PedidoItems.Remove(item);
            await _context.SaveChangesAsync();

            TempData["Resultado"] = "Se quitó: " + item.Descripcion;
            return RedirectToAction("Editar", new { id = pedidoId });
        }



                // GET: /Pedidos?fecha=2026-10-30
        [HttpGet]
        public async Task<IActionResult> Index(DateTime? fecha)
        {
            DateTime dia;
            if (fecha.HasValue)
            {
                dia = fecha.Value.Date;
            }
            else
            {
                dia = DateTime.Today;
            }

            List<Pedido> pedidos = await PedidosDelDia(dia);

            ViewBag.Fecha = dia;
            return View(pedidos);
        }

        // GET: /Pedidos/Imprimir/5   → un solo pedido
        [HttpGet]
        public async Task<IActionResult> Imprimir(int id)
        {
            Pedido? pedido = await _context.Pedidos
                .Include(p => p.Cliente)
                .Include(p => p.Items)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pedido == null)
            {
                return NotFound();
            }

            List<Pedido> lista = new List<Pedido>();
            lista.Add(pedido);

            return View("Imprimir", lista);
        }

           // GET: /Pedidos/ImprimirDia?fecha=2026-10-30   → todos los del día
        [HttpGet]
        public async Task<IActionResult> ImprimirDia(DateTime fecha)
        {
            List<Pedido> pedidos = await PedidosDelDia(fecha.Date);
            return View("Imprimir", pedidos);
        }



        // Convierte "5,54" o "5.54" en 5.54
        private static bool LeerDecimal(string? texto, out decimal valor)
        {
            valor = 0;

            if (string.IsNullOrWhiteSpace(texto))
            {
                return false;
            }

            string limpio = texto.Trim().Replace(",", ".");
            return decimal.TryParse(limpio, NumberStyles.Number, CultureInfo.InvariantCulture, out valor);
        }

        private async Task CargarClientesEnViewBag()
        {
            ViewBag.Clientes = await _context.Clientes
                .OrderBy(c => c.Nombre)
                .Select(c => c.Nombre)
                .ToListAsync();
        }
        private async Task<List<Pedido>> PedidosDelDia(DateTime dia)
        {
            DateTime diaSiguiente = dia.AddDays(1);

            List<Pedido> pedidos = await _context.Pedidos
                .Include(p => p.Cliente)
                .Include(p => p.Items)
                .Where(p => p.Fecha >= dia && p.Fecha < diaSiguiente)
                .OrderBy(p => p.Fecha)
                .ToListAsync();

            return pedidos;
        }
            
        
        
        
                // GET: /Pedidos/Eliminar/5   → muestra la confirmación
        [HttpGet]
        public async Task<IActionResult> Eliminar(int id)
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



                // POST: /Pedidos/Eliminar/5   → borra de verdad
        [HttpPost, ValidateAntiForgeryToken]
        [ActionName("Eliminar")]
        public async Task<IActionResult> EliminarConfirmado(int id)
        {
            Pedido? pedido = await _context.Pedidos
                .Include(p => p.Items)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pedido == null)
            {
                return NotFound();
            }

            DateTime fechaDelPedido = pedido.Fecha.Date;

            _context.PedidoItems.RemoveRange(pedido.Items);   // primero las líneas
            _context.Pedidos.Remove(pedido);                  // después el pedido
            await _context.SaveChangesAsync();

            TempData["Resultado"] = "Se eliminó el pedido #" + id;
            return RedirectToAction("Index", new { fecha = fechaDelPedido.ToString("yyyy-MM-dd") });
        }

    }
}