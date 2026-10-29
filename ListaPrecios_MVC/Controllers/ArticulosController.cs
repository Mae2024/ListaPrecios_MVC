using ListaPrecios_MVC.DATA;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using ClosedXML.Excel;
using ListaPrecios_MVC.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Data;
using DocumentFormat.OpenXml.Office2021.MipLabelMetaData;

namespace ListaPrecios_MVC.Controllers
{
    public class ArticulosController : Controller
    {
        private readonly PreciosContext _context;
        public ArticulosController(PreciosContext context) => _context = context;   //la forma corta de escribir el constructor sin llaves{}



        public async Task<IActionResult> Index(string? buscar, string? proveedor)
        {
            var query = _context.Articulos.AsQueryable();

            if (!string.IsNullOrWhiteSpace(buscar)) // filtro por texto
            {
                foreach (var palabra in buscar.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                    query = query.Where(a => EF.Functions
                    .Collate(a.Descripcion, "Modern_Spanish_CI_AI")
                    .Contains(palabra));
            }
            if (!string.IsNullOrWhiteSpace(proveedor) && proveedor != "*") //filtro por proveedor
                query = query.Where(a => a.Proveedor == proveedor);

            ViewBag.Buscar = buscar;
            ViewBag.Proveedor = proveedor;
            ViewBag.Proveedores = await _context.Articulos
                .Select(a => a.Proveedor).Distinct().OrderBy(p => p).ToListAsync();

            if (String.IsNullOrWhiteSpace(buscar) && string.IsNullOrWhiteSpace(proveedor))
            {
                ViewBag.SinFiltro = true;
                return View(new List<Articulo>());
            }

            return View(await query.OrderBy(a => a.Descripcion).ToListAsync());
        }


        [HttpGet]
        public IActionResult Importar() => View();

        
        
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Importar(IFormFile archivo, string proveedor, string clave)
        {
            var claveCorrecta = HttpContext.RequestServices.GetRequiredService<IConfiguration>()["ClaveImportar"];
            if (!ClaveValida(clave))
            {
                ModelState.AddModelError("", "clave incorrecta.");
                return View();

            }

            if (archivo == null || archivo.Length == 0)
            {
                ModelState.AddModelError("", "Elegi un archivo .xlsx");
                return View();
            }

            proveedor = (proveedor ?? "").Trim();
            int creados = 0, actualizados = 0, sinCambios = 0, omitidos = 0;
            var vistos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                using var stream = archivo.OpenReadStream();
                using var libro = new XLWorkbook(stream);
                var hoja = libro.Worksheet(1);
                foreach (var fila in hoja.RowsUsed().Skip(1)) //salta los titulos
                {
                    var desc = fila.Cell(1).GetString().Trim();
                    if (string.IsNullOrEmpty(desc) || !vistos.Add(desc) || !LeerPrecio(fila.Cell(2), out var SinIva) || !LeerPrecio(fila.Cell(3), out var final))
                    { omitidos++; continue; }

                    var art = await _context.Articulos.FirstOrDefaultAsync(a => a.Descripcion == desc && a.Proveedor == proveedor);

                    if (art == null)
                    {
                        _context.Articulos.Add(new Articulo
                        {
                            Descripcion = desc,
                            Proveedor = proveedor,
                            PrecioSinIva = SinIva,
                            PrecioFinal = final,
                            FechaActualizacion = DateTime.Now

                        });
                        creados++;
                    }
                    else if (art.PrecioSinIva != SinIva || art.PrecioFinal != final)
                    {
                        art.PrecioSinIva = SinIva;
                        art.PrecioFinal = final;
                        art.FechaActualizacion = DateTime.Now;
                        actualizados++;
                    }
                    else sinCambios++;
                }
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error al leer el archivo:" + ex.Message);
                return View();
            }
            TempData["Resultado"] = $"Creados: {creados} - Actualizados:{actualizados} - Sin Cambios: {sinCambios} - Omitidos: {omitidos}";
            return RedirectToAction(nameof(Index));

        }


        [HttpGet]
        public async Task<IActionResult> Crear()
        {
            ViewBag.Proveedores = await _context.Articulos
                .Select(a => a.Proveedor).Distinct().OrderBy(p => p).ToListAsync();
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(string descripcion, string proveedor, decimal precioSinIva, decimal precioFinal, string clave)
        {
            descripcion = (descripcion ?? "").Trim();
            proveedor = (proveedor ?? "").Trim();

            if (!ClaveValida(clave))
                ModelState.AddModelError("", "Clave incorrecta.");
            else if (descripcion == "" || proveedor == "" || precioFinal <= 0)
                ModelState.AddModelError("", "Descripción, proveedor y precio final son obligatorios.");
            else if (await _context.Articulos.AnyAsync(a => a.Descripcion == descripcion && a.Proveedor == proveedor))
                ModelState.AddModelError("", "Ya existe ese articulo para ese proveedor, usa modif. para cambiar el precio.");

            if (!ModelState.IsValid)
            {
                ViewBag.Descripcion = descripcion;
                ViewBag.ProveedorElegido = proveedor;
                ViewBag.Proveedores = await _context.Articulos
                    .Select(a => a.Proveedor).Distinct().OrderBy(p => p).ToArrayAsync();
                return View();
            }

            var art = new Articulo
            {
                Descripcion = descripcion,
                Proveedor = proveedor,
                PrecioSinIva = Math.Round(precioSinIva, 2),
                PrecioFinal = Math.Round(precioFinal, 2),
                FechaActualizacion = DateTime.Now

            };

            _context.Articulos.Add(art);
            await _context.SaveChangesAsync();

            TempData["Resultado"] = $"Se agrego {art.Descripcion}";
            return RedirectToAction(nameof(Index), new { buscar = art.Descripcion });


        }



        
        [HttpGet]
        public async Task<IActionResult> Editar(int id) 
        {
            var art = await _context.Articulos.FindAsync(id);
            if (art == null) return NotFound();
            return View(art);
        }

        
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id, string descripcion, decimal precioSinIva, decimal PrecioFinal, string clave)
        {
            var art = await _context.Articulos.FindAsync(id);
            if (art == null) return NotFound();  //resume: {return ...}

            var claveCorrecta = HttpContext.RequestServices
                .GetRequiredService<IConfiguration>()["ClaveImportar"];
            if(!ClaveValida(clave))
            {
                ModelState.AddModelError("", "Clave  incorrecta");
                return View(art);
            }
            if (string.IsNullOrWhiteSpace(descripcion) || PrecioFinal <= 0)
            {
                ModelState.AddModelError("", "La descripcion y el precio final son obligatorios");
                return View(art);
            }
            art.Descripcion = descripcion.Trim();
            art.PrecioSinIva = Math.Round(precioSinIva, 2);
            art.PrecioFinal = Math.Round(PrecioFinal, 2);
            art.FechaActualizacion = DateTime.Now;
            await _context.SaveChangesAsync();

            TempData["Resultado"] = $"Se actualizó: {art.Descripcion}";  //usar $"...{}" es lo mismo que usar: "se actualizo: " + art.Descripcion
            return RedirectToAction(nameof(Index), new { buscar = art.Descripcion });

        }

        
        
        //Acepta celdas numericas (con formato moneda) o texto tipo " $ 1.234,66"
        private static bool LeerPrecio(IXLCell celda, out decimal valor)
        {
            if (celda.TryGetValue(out valor)) return true;
            var texto = celda.GetString().Replace("$", "").Trim();
            return decimal.TryParse(texto, NumberStyles.Number, new CultureInfo("es-AR"), out valor);

        }



        //Validar clave para importar, editar y crear
        private bool ClaveValida(string? clave)
        {
            var claveCorrecta = HttpContext.RequestServices
                .GetRequiredService<IConfiguration>()["claveImportar"];
            return !string.IsNullOrEmpty(claveCorrecta) && clave == claveCorrecta;
        }
    }
}
