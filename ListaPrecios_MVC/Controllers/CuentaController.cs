using ListaPrecios_MVC;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using ListaPrecios_MVC.Models;
using Microsoft.Identity.Client;


namespace ListaPrecios_MVC.Controllers
{
    public class CuentaController : Controller
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        public CuentaController(SignInManager<IdentityUser> signInManager)
        {
            _signInManager = signInManager;
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




    }
}
