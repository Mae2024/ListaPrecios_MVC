using ListaPrecios_MVC.DATA;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<PreciosContext>(o => o.UseSqlServer(builder.Configuration.GetConnectionString("PreciosDb")));
builder.Services
    .AddIdentity<IdentityUser, IdentityRole>(opciones =>
    {
        //contraseña minimo 6 caracteres...
        opciones.Password.RequiredLength = 6;
        opciones.Password.RequireDigit = false;
        opciones.Password.RequireUppercase = false;
        opciones.Password.RequireLowercase = false;
        opciones.Password.RequireNonAlphanumeric = false;

        //se ingresa con nombre de usuario
        opciones.User.RequireUniqueEmail = false;
    })
    .AddEntityFrameworkStores<PreciosContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(opciones =>
{
    opciones.LoginPath = "/Cuenta/Login";
    opciones.AccessDeniedPath = "/Cuenta/AccesoDenegado";
    opciones.ExpireTimeSpan = TimeSpan.FromDays(30);
    opciones.SlidingExpiration = true;
});


var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PreciosContext>();
    db.Database.Migrate();

    await SeedIdentidad.Inicializar(scope.ServiceProvider);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();  // quien sos?
app.UseAuthorization();   // que podes hacer?

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Articulos}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
