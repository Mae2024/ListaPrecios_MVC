using DocumentFormat.OpenXml.Drawing.Charts;
using ListaPrecios_MVC;
using ListaPrecios_MVC.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;


namespace ListaPrecios_MVC.DATA
{
    public class PreciosContext : IdentityDbContext<IdentityUser>
    {
        public PreciosContext(DbContextOptions<PreciosContext> options) : base (options) { }
        public DbSet<Articulo> Articulos => Set<Articulo>();
        public DbSet<Cliente> Clientes => Set<Cliente>();
        public DbSet<Pedido> Pedidos => Set<Pedido>();
        public DbSet<PedidoItem> PedidoItems => Set<PedidoItem>();
    }
}
