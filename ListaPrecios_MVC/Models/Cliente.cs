using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace ListaPrecios_MVC.Models

{
    [Index(nameof(Nombre), IsUnique =true)]
    public class Cliente
    {
        public int Id { get; set; }
        [Required, StringLength(150)]
        public string  Nombre { get; set; }
        [StringLength(200)]
        public string? Direccion { get; set; }
        public List<Pedido> Pedidos { get; set; } = new List<Pedido>();      //new list crea la lista vacia desde el principio, para luego hacer Add sin problemas

    }
}
