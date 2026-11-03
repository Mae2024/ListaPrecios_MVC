using ListaPrecios_MVC.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ListaPrecios_MVC.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UsuariosController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;

        public UsuariosController(UserManager<IdentityUser> userManager)
        {
            _userManager = userManager;
        }

        // GET: /Usuarios
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            List<IdentityUser> usuarios = await _userManager.Users
                .OrderBy(u => u.UserName)
                .ToListAsync();

            List<UsuarioFila> filas = new List<UsuarioFila>();

            foreach (IdentityUser u in usuarios)
            {
                IList<string> roles = await _userManager.GetRolesAsync(u);

                UsuarioFila fila = new UsuarioFila();
                fila.Id = u.Id;
                fila.UserName = u.UserName ?? "";
                fila.Rol = string.Join(", ", roles);
                fila.Bloqueado = u.LockoutEnd != null && u.LockoutEnd > DateTimeOffset.Now;

                filas.Add(fila);
            }

            return View(filas);
        }

        // GET: /Usuarios/Crear
        [HttpGet]
        public IActionResult Crear()
        {
            return View(new CrearUsuarioViewModel());
        }

        // POST: /Usuarios/Crear
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(CrearUsuarioViewModel modelo)
        {
            if (modelo.Rol != "Admin" && modelo.Rol != "Vendedor")
            {
                ModelState.AddModelError("", "Rol no válido.");
            }

            if (!ModelState.IsValid)
            {
                return View(modelo);
            }

            string nombre = modelo.Usuario.Trim().ToLower();

            IdentityUser? existente = await _userManager.FindByNameAsync(nombre);
            if (existente != null)
            {
                ModelState.AddModelError("", "Ya existe un usuario con ese nombre.");
                return View(modelo);
            }

            IdentityUser nuevo = new IdentityUser();
            nuevo.UserName = nombre;

            IdentityResult resultado = await _userManager.CreateAsync(nuevo, modelo.Clave);

            if (!resultado.Succeeded)
            {
                foreach (IdentityError error in resultado.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
                return View(modelo);
            }

            await _userManager.AddToRoleAsync(nuevo, modelo.Rol);

            TempData["Resultado"] = "Se creó el usuario " + nombre + " (" + modelo.Rol + ").";
            return RedirectToAction("Index");
        }

        // GET: /Usuarios/ResetearClave/abc-123
        [HttpGet]
        public async Task<IActionResult> ResetearClave(string id)
        {
            IdentityUser? usuario = await _userManager.FindByIdAsync(id);
            if (usuario == null)
            {
                return NotFound();
            }

            ViewBag.UserName = usuario.UserName;
            ViewBag.Id = usuario.Id;
            return View();
        }

        // POST: /Usuarios/ResetearClave/abc-123
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetearClave(string id, string? claveNueva)
        {
            IdentityUser? usuario = await _userManager.FindByIdAsync(id);
            if (usuario == null)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(claveNueva) || claveNueva.Length < 6)
            {
                ModelState.AddModelError("", "La contraseña debe tener al menos 6 caracteres.");
                ViewBag.UserName = usuario.UserName;
                ViewBag.Id = usuario.Id;
                return View();
            }

            // Identity pide un "token" para cambiar una contraseña sin saber la anterior
            string token = await _userManager.GeneratePasswordResetTokenAsync(usuario);
            IdentityResult resultado = await _userManager.ResetPasswordAsync(usuario, token, claveNueva);

            if (!resultado.Succeeded)
            {
                foreach (IdentityError error in resultado.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
                ViewBag.UserName = usuario.UserName;
                ViewBag.Id = usuario.Id;
                return View();
            }

            // Si estaba bloqueado por intentos fallidos, lo desbloquea
            await _userManager.SetLockoutEndDateAsync(usuario, null);
            await _userManager.ResetAccessFailedCountAsync(usuario);

            TempData["Resultado"] = "Se cambió la contraseña de " + usuario.UserName + ".";
            return RedirectToAction("Index");
        }
    }
}