using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;
using ListaPrecios_MVC.DATA;
using ListaPrecios_MVC.Models;

namespace ListaPrecios_MVC.Controllers
{
    public class ClientesController : Controller
    {
        private readonly PreciosContext _context;

        public ClientesController(PreciosContext context)
        { 
            _context = context;
        }

        [HttpGet] //Get: //Clientes
        public async Task<IActionResult> Index()
        {
            List<Cliente> clientes = await _context.Clientes
                .OrderBy(c => c.Nombre)
                .ToListAsync();
            return View(clientes);
        }

        [HttpGet] //Get: //Clientes/Importar
        public IActionResult Importar()
        {
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]  //Post: //Clientes/Importar

        public async Task<IActionResult> Importar (IFormFile archivo, string clave)
        {
            if (!ClaveValida(clave)) 
            {
                ModelState.AddModelError("", "La clave es incorrecta.");
                return View();
            }
            if (archivo==null || archivo.Length==0)
            {
                ModelState.AddModelError("", "Elegi un archivo .xlsx");
                return View();
            }

            int creados = 0;
            int actualizados = 0;
            int sinCambios = 0;
            int omitidos = 0;
            HashSet<string> vistos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                using(Stream stream = archivo.OpenReadStream())
                using (XLWorkbook libro = new XLWorkbook(stream))
                {
                    IXLWorksheet hoja = libro.Worksheet(1);

                    foreach (IXLRow fila in hoja.RowsUsed().Skip(1)) //salta los titulos
                    {
                        string nombre = fila.Cell(1).GetString().Trim();
                        string direccion = fila.Cell(2).GetString().Trim();

                        //fila sin nombre o nombre repetido dentro del mismo archivo
                        if(nombre =="" || !vistos.Add(nombre))
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
                            _context.Clientes.Add(nuevo);
                            creados++;
                        }
                        else if (cliente.Direccion != direccion) 
                        {
                            cliente.Direccion = direccion;
                            actualizados++;

                        }
                        else { sinCambios++;}


                    }
                }
                await _context.SaveChangesAsync();
            }
            catch (Exception Ex)
            {
                ModelState.AddModelError("", "Error al leer archivo." + Ex.Message);
                return View();
            }
            TempData["Resultado"] = "Creados: " + creados + "* Actualizados: " + actualizados + "* Sin Cambios: " + sinCambios + "* Omitidos: " + omitidos;
            return RedirectToAction("Index");
        }

        private bool ClaveValida(string? clave)
        {
            IConfiguration configuracion = HttpContext.RequestServices
                .GetRequiredService<IConfiguration>();

            string? ClaveCorrecta = configuracion["ClaveImportar"];
                if(string.IsNullOrEmpty(ClaveCorrecta))  { return false; }
            return clave == ClaveCorrecta;
        }




    }

}
