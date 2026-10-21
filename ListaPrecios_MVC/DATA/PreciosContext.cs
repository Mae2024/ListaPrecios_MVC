using ListaPrecios_MVC;
using ListaPrecios_MVC.Models;
using Microsoft.EntityFrameworkCore;


namespace ListaPrecios_MVC.DATA
{
    public class PreciosContext : DbContext
    {
        public PreciosContext(DbContextOptions<PreciosContext> options) : base (options) { }
        public DbSet<Articulo> Articulos => Set<Articulo>();

    }
}
