using AbcRetailer.Services;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// ---------------- Add services to the container ----------------
builder.Services.AddControllersWithViews();

// ---------------- Register AzureStorageService using IConfiguration ----------------
builder.Services.AddSingleton<AzureStorageService>(sp =>
    new AzureStorageService(sp.GetRequiredService<IConfiguration>()));

var app = builder.Build();

// ---------------- Configure the HTTP request pipeline ----------------
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

// ---------------- Map Controller Routes ----------------

// Default route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);

// Admin routes
app.MapControllerRoute(
    name: "admin_login",
    pattern: "Admin/Login",
    defaults: new { controller = "Admin", action = "Login" }
);

app.MapControllerRoute(
    name: "admin_inventory",
    pattern: "Admin/Inventory",
    defaults: new { controller = "Admin", action = "ManageInventory" }
);

// Customer routes
app.MapControllerRoute(
    name: "customer_dashboard",
    pattern: "Customer/Dashboard",
    defaults: new { controller = "Customer", action = "Dashboard" }
);

app.MapControllerRoute(
    name: "customer_index",
    pattern: "Customer/Index",
    defaults: new { controller = "Customer", action = "Index" }
);

app.Run();
