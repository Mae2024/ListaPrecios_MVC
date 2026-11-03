using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ListaPrecios_MVC.DATA
{
    public class SeedIdentidad
    {
        public static async Task Inicializar(IServiceProvider servicios) 
        {
            RoleManager<IdentityRole> roleManager = servicios.GetRequiredService<RoleManager<IdentityRole>>();
            UserManager<IdentityUser> userManager = servicios.GetRequiredService<UserManager<IdentityUser>>();
            IConfiguration configuracion = servicios.GetRequiredService<IConfiguration>();

            //crear los soles si no existen
            string[] roles = { "Admin", "Vendedor" };

            foreach (string role in roles)
            {
                bool existe = await roleManager.RoleExistsAsync(role);
                if (!existe)
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // crear admin si no existe

            string? usuarioAdmin = configuracion["AdminInicial:Usuario"];
            string? claveAdmin = configuracion["AdminInicial:Clave"];

            if (string.IsNullOrWhiteSpace(usuarioAdmin) || string.IsNullOrWhiteSpace(claveAdmin))
            {
                return;           //no hay datos configurados: no crea nada
            }

            IdentityUser? admin = await userManager.FindByNameAsync(usuarioAdmin);

            if(admin==null)
            {
                admin = new IdentityUser();
                admin.UserName = usuarioAdmin;

                IdentityResult resultado = await userManager.CreateAsync(admin, claveAdmin);
                if (!resultado.Succeeded)
                {
                    string errores = string.Join("", resultado.Errors.Select(e => e.Description));
                    throw new Exception("No se pudo crear el usuario admin: " + errores);
                }
            }

            //asegurar que tenga rol admin

            bool esAdmin = await userManager.IsInRoleAsync(admin, "Admin");
            if(!esAdmin)
            {
                await userManager.AddToRoleAsync(admin, "Admin");
            }


            //Clientes y pedidos sin vendedor quedan asignados al admin
            PreciosContext db = servicios.GetRequiredService<PreciosContext>();
            string adminId = admin.Id;
            await db.Clientes
                .Where(c => c.VendedorId == null)
                .ExecuteUpdateAsync(s => s.SetProperty(c => c.VendedorId, adminId));
            await db.Pedidos
                .Where(p => p.VendedorId == null)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.VendedorId, adminId));


        }
     

    }
}
