using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace ListaPrecios_MVC.Models
{
    [Index(nameof(Descripcion), nameof(Proveedor), IsUnique = true)]
    public class Articulo
    {
        public int Id { get; set; }
        [Required, StringLength(200)]
        public string Descripcion { get; set; } = "";
        [StringLength(100)]
        public string Proveedor { get; set; } = "";
        [Precision(18,2), Display(Name ="Precio Sin IVA")]
        public decimal PrecioSinIva { get; set; }
        [Precision(18,2), Display(Name ="Precio Final")]
        public decimal PrecioFinal { get; set; }
        [Display (Name ="Actualizado")]
        public DateTime FechaActualizacion { get; set; }



    }
}
