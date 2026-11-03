using ListaPrecios_MVC;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using ListaPrecios_MVC.Models;
using Microsoft.Identity.Client;
using Microsoft.AspNetCore.Authorization;

namespace ListaPrecios_MVC.Controllers
{
    public class CuentaController : Controller
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        public CuentaController(SignInManager<IdentityUser> signInManager, UserManager<IdentityUser> userManager    )
        {
            _signInManager = signInManager;
            _userManager = userManager;
        }

        //GET: /Cuenta/Login
        [HttpGet]
        public IActionResult Login(string? returnUrl)
        {
            ViewBag.returnUrl = returnUrl;
            return View();
        }

        //POST: //Cuenta/Login
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel modelo, string? returnUrl)
        {
            ViewBag.returnUrl = returnUrl;
            if (!ModelState.IsValid)
            {
                return View(modelo);
            }
            Microsoft.AspNetCore.Identity.SignInResult resultado = await _signInManager.PasswordSignInAsync(
                modelo.Usuario.Trim(),
                modelo.Clave,
                modelo.Recordarme,
                true);  //true es para bloquear temporalmente si falla varia sveces

            if (resultado.Succeeded)
            {
                //volver a la pagina que queria ver, solo si es de esta app
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }
                return RedirectToAction("Index", "Articulos");
            }

            if (resultado.IsLockedOut)
            {
                ModelState.AddModelError("", "Demasiados intentos fallidos, espera 5 minutos y proba nuevamente.");
            }
            else
            {
                ModelState.AddModelError("", "Usuario o contraseña incorrectos");
            }
            return View(modelo);

        }

        // POST: /Cuenta/Logout
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Articulos");
        }

        //GET: /Cuenta/AccdesoDenegado
        [HttpGet]
        public IActionResult AccesoDenegado()
        {
            return View();
        }

        // GET: /Cuenta/CambiarClave
        [HttpGet]
        [Authorize]
        public IActionResult CambiarClave()
        {
            return View();
        }

        // POST: /Cuenta/CambiarClave
        [HttpPost, ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> CambiarClave(CambiarClaveViewModel modelo)
        {
            if (!ModelState.IsValid)
            {
                return View(modelo);
            }

            IdentityUser? usuario = await _userManager.GetUserAsync(User);
            if (usuario == null)
            {
                return RedirectToAction("Login");
            }

            IdentityResult resultado = await _userManager.ChangePasswordAsync(usuario, modelo.ClaveActual, modelo.ClaveNueva);

            if (!resultado.Succeeded)
            {
                ModelState.AddModelError("", "No se pudo cambiar. Revisá que la contraseña actual sea correcta.");
                return View(modelo);
            }

            // Renueva la sesión, para que no te saque al cambiar la contraseña
            await _signInManager.RefreshSignInAsync(usuario);

            TempData["Resultado"] = "Tu contraseña se cambió correctamente.";
            return RedirectToAction("Index", "Articulos");
        }


    }
}
