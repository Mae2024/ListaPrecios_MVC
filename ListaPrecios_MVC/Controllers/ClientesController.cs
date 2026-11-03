using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;
using ListaPrecios_MVC.DATA;
using ListaPrecios_MVC.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace ListaPrecios_MVC.Controllers
{
    [Authorize]
    public class ClientesController : Controller
    {
        private readonly PreciosContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public ClientesController(PreciosContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet] // get index
        public async Task<IActionResult> Index(string? vendedor)
        {
            IQueryable<Cliente> query = _context.Clientes.Include(c => c.Vendedor);

            if (User.IsInRole("Admin"))
            {
                if (!string.IsNullOrEmpty(vendedor))
                {
                    query = query.Where(c => c.VendedorId == vendedor);
                }
                await CargarVendedoresEnViewBag();
            }
            else
            {
                string? usuarioId = _userManager.GetUserId(User);
                query = query.Where(c => c.VendedorId == usuarioId);
            }

            List<Cliente> clientes = await query.OrderBy(c => c.Nombre).ToListAsync();

            ViewBag.Vendedor = vendedor;
            return View(clientes);
        }


        //Get: //Clientes/Importar
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Importar()
        {
            await CargarVendedoresEnViewBag();
            return View();
        }

        //Post: //Clientes/Importar
        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Importar(IFormFile archivo, string? vendedorId)
        {
            IdentityUser? vendedor = null;
            if (!string.IsNullOrEmpty(vendedorId))
            {
                vendedor = await _userManager.FindByIdAsync(vendedorId);
            }

            if (vendedor == null)
            {
                ModelState.AddModelError("", "Elegí a qué vendedor asignar los clientes.");
                await CargarVendedoresEnViewBag();
                return View();
            }

            if (archivo == null || archivo.Length == 0)
            {
                ModelState.AddModelError("", "Elegí un archivo .xlsx");
                await CargarVendedoresEnViewBag();
                return View();
            }

            int creados = 0;
            int actualizados = 0;
            int sinCambios = 0;
            int omitidos = 0;
            HashSet<string> vistos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                using (Stream stream = archivo.OpenReadStream())
                using (XLWorkbook libro = new XLWorkbook(stream))
                {
                    IXLWorksheet hoja = libro.Worksheet(1);

                    foreach (IXLRow fila in hoja.RowsUsed().Skip(1)) //salta los titulos
                    {
                        string nombre = fila.Cell(1).GetString().Trim();
                        string direccion = fila.Cell(2).GetString().Trim();

                        //fila sin nombre o nombre repetido dentro del mismo archivo
                        if (nombre == "" || !vistos.Add(nombre))
                        {
                            omitidos++;
                            continue;
                        }
                        Cliente? cliente = await _context.Clientes
                            .FirstOrDefaultAsync(c => c.Nombre == nombre);
                        if (cliente == null)
                        {
                            Cliente nuevo = new Cliente();
                            nuevo.Nombre = nombre;
                            nuevo.Direccion = direccion;
                            nuevo.VendedorId = vendedor.Id;
                            _context.Clientes.Add(nuevo);
                            creados++;
                        }
                        else
                        {
                            bool huboCambios = false;

                            if (cliente.Direccion != direccion)
                            {
                                cliente.Direccion = direccion;
                                huboCambios = true;
                            }
                            if (cliente.VendedorId != vendedor.Id)
                            {
                                cliente.VendedorId = vendedor.Id;   // el cliente pasa a este vendedor
                                huboCambios = true;
                            }

                            if (huboCambios)
                            {
                                actualizados++;
                            }
                            else
                            {
                                sinCambios++;
                            }
                        }


                    }
                }
                await _context.SaveChangesAsync();
            }
            catch (Exception Ex)
            {
                ModelState.AddModelError("", "Error al leer archivo." + Ex.Message);
                await CargarVendedoresEnViewBag();
                return View();
            }
            TempData["Resultado"] = "Creados: " + creados + "* Actualizados: " + actualizados + "* Sin Cambios: " + sinCambios + "* Omitidos: " + omitidos;
            return RedirectToAction("Index");
        }

        private async Task CargarVendedoresEnViewBag()
        {
            ViewBag.Vendedores = await _userManager.Users
                .OrderBy(u => u.UserName)
                .ToListAsync();

        }

    }
}
