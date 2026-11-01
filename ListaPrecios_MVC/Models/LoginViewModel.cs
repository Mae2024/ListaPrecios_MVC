using System.ComponentModel.DataAnnotations;

namespace ListaPrecios_MVC.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage ="Escribí tu usuario. ")]
        [Display(Name ="Usuario")]
        public string Usuario { get; set; } = "";
        [Required(ErrorMessage ="Escribí tu contraseña. ")]
        [DataType(DataType.Password)] //aca hace que asp-for dibuje el campo con puntitos
        [Display(Name ="Contraseña")]
        public string Clave { get; set; } = "";
        [Display(Name ="Recordarme")]
        public bool Recordarme { get; set; }
    }
}
