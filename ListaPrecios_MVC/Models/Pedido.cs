namespace ListaPrecios_MVC.Models
{
    public class Pedido
    {
        public int Id { get; set; }
        public int ClienteId { get; set; }
        public Cliente? Cliente { get; set; }
        public DateTime Fecha { get; set; }
        public List<PedidoItem> Items { get; set; } = new List<PedidoItem>();
    }
}
