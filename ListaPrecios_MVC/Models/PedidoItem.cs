using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;


namespace ListaPrecios_MVC.Models
{

    public class PedidoItem
    {
        public int Id { get; set; }
        public int PedidoId { get; set; }
        public Pedido? Pedido { get; set; }
        [Required, StringLength(200)]
        public string Descripcion { get; set; } = "";
        [StringLength(100)]
        public string Proveedor { get; set; } = "";
        [Precision(18, 3)]
        public decimal Cantidad { get; set; }
        [Precision(18, 2)]
        public decimal PrecioUnitario { get; set; }
        [Precision(18, 2)]
        public decimal  SubTotal { get; set; }
    }
}
