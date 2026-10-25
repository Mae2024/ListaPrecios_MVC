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
        public ArticulosController(PreciosContext context) => _context = context;

        public async Task<IActionResult> Index(string? buscar, string? proveedor)
        {
            var query = _context.Articulos.AsQueryable();

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                foreach (var palabra in buscar.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                    query = query.Where(a => a.Descripcion.Contains(palabra));
            }
            if (!string.IsNullOrWhiteSpace(proveedor))
                query = query.Where(a => a.Proveedor == proveedor);

            ViewBag.Buscar = buscar;
            ViewBag.Proveedor = proveedor;
            ViewBag.Proveedores = await _context.Articulos
                .Select(a => a.Proveedor).Distinct().OrderBy(p => p).ToListAsync();
            return View(await query.OrderBy(a => a.Descripcion).ToListAsync());
        }

        public IActionResult Importar() => View();

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Importar(IFormFile archivo, string proveedor, string clave)
        {
            var claveCorrecta = HttpContext.RequestServices.GetRequiredService<IConfiguration>()["ClaveImportar"];
            if (string.IsNullOrEmpty(claveCorrecta) || clave != claveCorrecta)
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

        //Acepta celdas numericas (con formato moneda) o texto tipo " $ 1.234,66"
        private static bool LeerPrecio(IXLCell celda, out decimal valor)
        {
            if (celda.TryGetValue(out valor)) return true;
            var texto = celda.GetString().Replace("$", "").Trim();
            return decimal.TryParse(texto, NumberStyles.Number, new CultureInfo("es-AR"), out valor);

        }
    }
}
