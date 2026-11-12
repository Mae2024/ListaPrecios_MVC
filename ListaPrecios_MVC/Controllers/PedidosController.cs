
using DocumentFormat.OpenXml.Office.CustomUI;
using ListaPrecios_MVC.DATA;
using ListaPrecios_MVC.Migrations;
using ListaPrecios_MVC.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace ListaPrecios_MVC.Controllers
{
    [Authorize]
    public class PedidosController : Controller
    {
        private readonly PreciosContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public PedidosController(PreciosContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
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
            string usuarioId = UsuarioActualId();
            Cliente? clienteElegido = null;

            if (!string.IsNullOrWhiteSpace(nuevoNombre))
            {
                // Opción 1: crear un cliente nuevo, que queda a nombre del usuario actual
                string nombre = nuevoNombre.Trim();
                Cliente? existente = await _context.Clientes.FirstOrDefaultAsync(c => c.Nombre == nombre);

                if (existente != null && existente.VendedorId == usuarioId)
                {
                    ModelState.AddModelError("", "Ese cliente ya existe. Elegilo de la lista de arriba.");
                }
                else if (existente != null)
                {
                    ModelState.AddModelError("", "Ese cliente ya está asignado a otro vendedor.");
                }
                else
                {
                    clienteElegido = new Cliente();
                    clienteElegido.Nombre = nombre;
                    clienteElegido.VendedorId = usuarioId;
                    if (nuevaDireccion != null)
                    {
                        clienteElegido.Direccion = nuevaDireccion.Trim();
                    }
                    _context.Clientes.Add(clienteElegido);
                }
            }
            else if (!string.IsNullOrWhiteSpace(cliente))
            {
                // Opción 2: un cliente propio que ya existe
                string nombre = cliente.Trim();
                clienteElegido = await _context.Clientes
                    .FirstOrDefaultAsync(c => c.Nombre == nombre && c.VendedorId == usuarioId);

                if (clienteElegido == null)
                {
                    ModelState.AddModelError("", "No encontré ese cliente entre los tuyos. Elegilo de las sugerencias o crealo abajo.");
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
            pedido.VendedorId = usuarioId;
            pedido.Fecha = HoraArgentina.Ahora();
            _context.Pedidos.Add(pedido);

            await _context.SaveChangesAsync();

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
            if (!PuedeVer(pedido))
            {
                return Forbid();
            }

            List<Articulo> resultados = new List<Articulo>();

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                IQueryable<Articulo> query = _context.Articulos;

                string texto = buscar.Trim();
                bool esCodigo = await _context.Articulos.AnyAsync(a => a.Codigo == texto);
                if (esCodigo)
                {
                    query = query.Where(a => a.Codigo == texto);
                }
                else
                {
                    foreach (var palabra in buscar.Split(' ', StringSplitOptions.RemoveEmptyEntries))
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
            if (!PuedeVer(pedido))
            {
                return Forbid();
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
            Pedido? pedidoDelItem = await _context.Pedidos.FindAsync(item.PedidoId);
            if (pedidoDelItem == null || !PuedeVer(pedidoDelItem))
            {
                return Forbid();
            }

            int pedidoId = item.PedidoId;

            _context.PedidoItems.Remove(item);
            await _context.SaveChangesAsync();

            TempData["Resultado"] = "Se quitó: " + item.Descripcion;
            return RedirectToAction("Editar", new { id = pedidoId });
        }



        // GET: /Pedidos?fecha=2026-11-02&vendedor=...
        [HttpGet]
        public async Task<IActionResult> Index(DateTime? fecha, string? vendedor)
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

            string? filtroVendedor;
            if (User.IsInRole("Admin"))
            {
                filtroVendedor = vendedor;          // vacío = todos los vendedores
                await CargarVendedoresEnViewBag();
            }
            else
            {
                filtroVendedor = UsuarioActualId(); // un vendedor solo ve los suyos
            }

            List<Pedido> pedidos = await PedidosDelDia(dia, filtroVendedor);

            ViewBag.Fecha = dia;
            ViewBag.Vendedor = vendedor;
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
            if (!PuedeVer(pedido))
            {
                return Forbid();
            }

            List<Pedido> lista = new List<Pedido>();
            lista.Add(pedido);

            return View("Imprimir", lista);
        }

        // GET: /Pedidos/ImprimirDia? fecha = 2026 - 11 - 02 & vendedor = ...
        [HttpGet]
        public async Task<IActionResult> ImprimirDia(DateTime fecha, string? vendedor)
        {
            string? filtroVendedor;
            if (User.IsInRole("Admin"))
            {
                filtroVendedor = vendedor;
            }
            else
            {
                filtroVendedor = UsuarioActualId();
            }

            List<Pedido> pedidos = await PedidosDelDia(fecha.Date, filtroVendedor);
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

        //solo clientes del usuario actual
        private async Task CargarClientesEnViewBag()
        {
            string usuarioId = UsuarioActualId();

            ViewBag.Clientes = await _context.Clientes
                .Where(c => c.VendedorId == usuarioId)
                .OrderBy(c => c.Nombre)
                .Select(c => c.Nombre)
                .ToListAsync();
        }

        //El id del usuario que inicio sesion
        private string UsuarioActualId()
        {
            string? id = _userManager.GetUserId(User);
            if (id == null)
            {
                return "";
            }
            return id;
        }
        //este usuario puede ver o modificar este pedido ?
        private bool PuedeVer(Pedido pedido)
        {
            if (User.IsInRole("Admin"))
            {
                return true;
            }
            return pedido.VendedorId == UsuarioActualId();
        }
        private async Task CargarVendedoresEnViewBag()
        {
            ViewBag.Vendedores = await _userManager.Users
                .OrderBy(u => u.UserName)
                .ToListAsync();
        }



        private async Task<List<Pedido>> PedidosDelDia(DateTime dia, string? vendedorId)
        {
            DateTime diaSiguiente = dia.AddDays(1);

            IQueryable<Pedido> query = _context.Pedidos
                       .Include(p => p.Cliente)
                      .Include(p => p.Items)
                      .Include(p => p.Vendedor)
                      .Where(p => p.Fecha >= dia && p.Fecha < diaSiguiente);
            if (!string.IsNullOrEmpty(vendedorId))
            {
                query = query.Where(p => p.VendedorId == vendedorId);
            }
            List<Pedido> pedidos = await query.OrderBy(pedidos => pedidos.Fecha).ToListAsync();
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
            if (!PuedeVer(pedido))
            {
                return Forbid();
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
            if (!PuedeVer(pedido))
            {
                return Forbid();
            }

            DateTime fechaDelPedido = pedido.Fecha.Date;

            _context.PedidoItems.RemoveRange(pedido.Items);   // primero las líneas
            _context.Pedidos.Remove(pedido);                  // después el pedido
            await _context.SaveChangesAsync();

            TempData["Resultado"] = "Se eliminó el pedido #" + id;
            return RedirectToAction("Index", new { fecha = fechaDelPedido.ToString("yyyy-MM-dd") });
        }


        //GET: /Pedidos/WhatsApp
        [HttpGet]
        public async Task<IActionResult> WhatsApp(int id, string? telefono)
        {
            Pedido? pedido = await _context.Pedidos
                .Include(p => p.Cliente)
                .Include(p => p.Items)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pedido == null)
            {
                return NotFound();
            }
            if (!PuedeVer(pedido))
            {
                return Forbid();
            }
            string texto = "Pedido #" + pedido.Id + " - " + pedido.Cliente?.Nombre + "\n";
            texto = texto + pedido.Fecha.ToString("dd/MM/yyyy") + "\n\n";

            foreach (PedidoItem item in pedido.Items.OrderBy(i => i.Id))
            {
                texto = texto + "- " + item.Cantidad.ToString("0.###") + " x " + item.Descripcion + " - $ " + item.SubTotal.ToString("N2") + "\n";
            }

            decimal total = pedido.Items.Sum(i => i.SubTotal);
            texto = texto + "\nTOTAL: $ " + total.ToString("N2");

            string numero = "";
            if (telefono != null)
            {
                foreach (char c in telefono)
                {
                    if (char.IsDigit(c))
                    {
                        numero = numero + c;
                    }
                }
            }

            // 6) Armar el link de WhatsApp
            string textoCodificado = Uri.EscapeDataString(texto);
            string link;

            if (numero == "")
            {
                link = "https://wa.me/?text=" + textoCodificado;
            }
            else
            {
                link = "https://wa.me/549" + numero + "?text=" + textoCodificado;
            }

            return Redirect(link);

        }
    }
}