using System.ComponentModel.DataAnnotations;

namespace ListaPrecios_MVC.Models
{
    // Una fila de la lista de usuarios
    public class UsuarioFila
    {
        public string Id { get; set; } = "";
        public string UserName { get; set; } = "";
        public string Rol { get; set; } = "";
        public bool Bloqueado { get; set; }
    }

    // Formulario para crear un usuario
    public class CrearUsuarioViewModel
    {
        [Required(ErrorMessage = "Escribí el nombre de usuario.")]
        [Display(Name = "Usuario")]
        public string Usuario { get; set; } = "";

        [Required(ErrorMessage = "Escribí una contraseña.")]
        [MinLength(6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres.")]
        [Display(Name = "Contraseña inicial")]
        public string Clave { get; set; } = "";

        [Display(Name = "Rol")]
        public string Rol { get; set; } = "Vendedor";
    }

    // Formulario para cambiar la propia contraseña
    public class CambiarClaveViewModel
    {
        [Required(ErrorMessage = "Escribí tu contraseña actual.")]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña actual")]
        public string ClaveActual { get; set; } = "";

        [Required(ErrorMessage = "Escribí la contraseña nueva.")]
        [MinLength(6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres.")]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña nueva")]
        public string ClaveNueva { get; set; } = "";

        [Required(ErrorMessage = "Repetí la contraseña nueva.")]
        [Compare("ClaveNueva", ErrorMessage = "Las contraseñas no coinciden.")] //esta linea compara que las 2 contraseñas seas iguales
        [DataType(DataType.Password)]
        [Display(Name = "Repetir contraseña nueva")]
        public string ClaveNuevaRepetida { get; set; } = "";
    }
}