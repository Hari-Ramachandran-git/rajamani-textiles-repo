using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;

var builder = WebApplication.CreateBuilder(args);
// Railway injects PORT; bind publicly inside the container.
var port = builder.Configuration["PORT"];
if (!string.IsNullOrWhiteSpace(port))
{
    if (!int.TryParse(port, out var number) || number is < 1 or > 65535)
        throw new InvalidOperationException("PORT must be a valid TCP port.");
    builder.WebHost.UseUrls($"http://0.0.0.0:{number}");
}
var adminKey = builder.Configuration["AdminKey"];
if (string.IsNullOrWhiteSpace(adminKey))
{
    if (!builder.Environment.IsDevelopment()) throw new InvalidOperationException("Set AdminKey before starting outside Development.");
    adminKey = "local-demo-admin";
}
var connectionString = builder.Configuration.GetConnectionString("Shop");
if (string.IsNullOrWhiteSpace(connectionString))
{
    var dataDirectory = builder.Configuration["DataDirectory"]
        ?? builder.Configuration["RAILWAY_VOLUME_MOUNT_PATH"]
        ?? builder.Environment.ContentRootPath;
    Directory.CreateDirectory(dataDirectory);
    connectionString = new SqliteConnectionStringBuilder { DataSource = Path.Combine(dataDirectory, "shop.db") }.ToString();
}
builder.Services.AddDbContext<ShopDb>(o => o.UseSqlite(connectionString));
var allowedOrigins = (builder.Configuration["AllowedOrigins"] ?? (builder.Environment.IsDevelopment()
    ? "http://localhost:5173,http://localhost:5174" : ""))
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
{
    if (allowedOrigins.Length > 0)
        policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod();
}));
var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ShopDb>();
    db.Database.EnsureCreated();
    // Upgrade databases created by the original barebones catalog without losing orders.
    var connection = db.Database.GetDbConnection();
    connection.Open();
    using (var command = connection.CreateCommand())
    {
        command.CommandText = "PRAGMA table_info(Products)";
        using var reader = command.ExecuteReader();
        var hasImage = false;
        while (reader.Read()) hasImage |= reader.GetString(1) == "ImageUrl";
        reader.Close();
        if (!hasImage) db.Database.ExecuteSqlRaw("ALTER TABLE Products ADD COLUMN ImageUrl TEXT NOT NULL DEFAULT ''");
    }
    connection.Close();
    var originalNames = new[] { "Natural cotton sheeting", "Sage linen sheet", "Terracotta canvas", "Ivory wide-width voile", "Indigo cotton sheet", "Oatmeal linen sheet" };
    var existing = db.Products.ToList();
    if (existing.Count == 0 || (existing.Count == 6 && existing.All(p => originalNames.Contains(p.Name))))
    {
        db.Products.RemoveRange(existing);
        db.Products.AddRange(
            new Product { Name = "Traditional Floral Double Bedsheet", Category = "Bedsheets", Description = "Pure cotton with a traditional floral print. Includes two pillow covers.", Size = "Double · 90 × 100 in", Price = 890, Stock = 40, Color = "#8b131b", ImageUrl = "/images/bedsheet-double-floral.jpg" },
            new Product { Name = "Luxury Satin Stripe King Bedsheet", Category = "Bedsheets", Description = "Soft satin cotton with elegant woven stripes and two pillow covers.", Size = "King · 108 × 108 in", Price = 1250, Stock = 25, Color = "#e9dfcb", ImageUrl = "/images/bedsheet-satin-stripe.jpg" },
            new Product { Name = "Everyday Geometric Single Bedsheet", Category = "Bedsheets", Description = "Breathable cotton with a crisp geometric print for everyday comfort.", Size = "Single · 60 × 90 in", Price = 490, Stock = 32, Color = "#2d3e59", ImageUrl = "/images/bedsheet-single-geometric.jpg" },
            new Product { Name = "Pure Cotton Waffle Bath Towels", Category = "Towels", Description = "Lightweight, absorbent waffle cotton towels. A practical pack of two.", Size = "Pack of 2 · 30 × 60 in", Price = 590, Stock = 30, Color = "#bb9139", ImageUrl = "/images/towel-waffle-pack.jpg" },
            new Product { Name = "Crimson Turkish Cotton Bath Towel", Category = "Towels", Description = "Plush combed cotton with a soft hand feel and generous coverage.", Size = "Bath towel · 30 × 60 in", Price = 690, Stock = 24, Color = "#8b131b", ImageUrl = "/images/towel-turkish-bath.jpg" },
            new Product { Name = "Cotton Face & Hand Towels", Category = "Towels", Description = "A colorful set of four soft cotton towels for the whole family.", Size = "Set of 4 · 16 × 24 in", Price = 380, Stock = 35, Color = "#a48764", ImageUrl = "/images/towel-hand-set.jpg" },
            new Product { Name = "Traditional Ilavam Panju Pillow", Category = "Pillows", Description = "A traditional pillow filled with natural silk cotton for gentle support.", Size = "Standard · 18 × 27 in", Price = 650, Stock = 20, Color = "#e7ddc9", ImageUrl = "/images/pillow-silk-cotton.jpg" },
            new Product { Name = "Hotel Comfort Microfiber Pillows", Category = "Pillows", Description = "A pair of fluffy microfiber pillows for a soft and comfortable bed.", Size = "Pair of 2 · 18 × 27 in", Price = 890, Stock = 18, Color = "#eeebe3", ImageUrl = "/images/pillow-microfiber-pair.jpg" },
            new Product { Name = "Handcrafted Cotton Mattress", Category = "Beds & Mattresses", Description = "Traditional cotton-filled bedding with a hand-tufted finish.", Size = "Single · 36 × 72 in", Price = 3490, Stock = 12, Color = "#aa7b5d", ImageUrl = "/images/category-beds-mattresses.webp" },
            new Product { Name = "Baby Bed & Mosquito Net Set", Category = "Baby", Description = "A cozy baby bedding set with a protective net for supervised rest.", Size = "Baby bed set", Price = 1190, Stock = 16, Color = "#dbc3bb", ImageUrl = "/images/category-baby-section.webp" },
            new Product { Name = "Gold Border Ceremonial Shawl", Category = "Function Shawls", Description = "A festive silk-cotton shawl finished with an elegant gold border.", Size = "Shawl · 1.2 × 2 m", Price = 790, Stock = 25, Color = "#c59837", ImageUrl = "/images/category-function-shawls.webp" },
            new Product { Name = "Cotton Everyday Innerwear Set", Category = "Inner Wears", Description = "Soft cotton essentials for comfortable everyday wear.", Size = "Set of 2 · Medium", Price = 390, Stock = 24, Color = "#e7e1d4", ImageUrl = "/images/category-inner-wears.webp" },
            new Product { Name = "Woven Eyelet Curtains", Category = "Screens & Curtains", Description = "Textured woven panels to bring warmth and privacy to your space.", Size = "Pair · 7 ft", Price = 1290, Stock = 18, Color = "#af9c7b", ImageUrl = "/images/category-screens.webp" },
            new Product { Name = "Traditional Handloom Bedspread", Category = "Bed Spreads", Description = "A beautifully woven cotton spread with a timeless patterned finish.", Size = "Double · 90 × 100 in", Price = 1490, Stock = 15, Color = "#a4765a", ImageUrl = "/images/bedspread-jacquard-handloom.jpg" },
            new Product { Name = "Quilted Reversible Bedcover", Category = "Bed Covers", Description = "A softly quilted bedcover with two versatile sides for easy styling.", Size = "Double · 90 × 100 in", Price = 1890, Stock = 14, Color = "#b9a28d", ImageUrl = "/images/category-bedcovers.webp" });
        db.SaveChanges();
    }
}
// Run CORS before the admin key check so browser preflights can complete.
app.UseCors();
app.MapGet("/health", async (ShopDb db) =>
{
    try
    {
        return await db.Database.CanConnectAsync()
            ? Results.Ok(new { status = "healthy" })
            : Results.StatusCode(503);
    }
    catch { return Results.StatusCode(503); }
});
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/api/admin") && context.Request.Headers["X-Admin-Key"] != adminKey)
    { context.Response.StatusCode = 401; await context.Response.WriteAsJsonAsync(new { error = "Invalid admin key." }); return; }
    await next();
});
app.MapGet("/api/products", async (ShopDb db) => await db.Products.OrderBy(p => p.Id).ToListAsync());
var admin = app.MapGroup("/api/admin");
admin.MapPost("/products", async (ProductInput input, ShopDb db) =>
{
    if (!input.Valid()) return Results.BadRequest(new { error = "Enter valid product details, price, stock and a hex color." });
    var product = new Product(); input.Apply(product); db.Products.Add(product); await db.SaveChangesAsync();
    return Results.Created($"/api/products/{product.Id}", product);
});
admin.MapPut("/products/{id:int}", async (int id, ProductInput input, ShopDb db) =>
{
    if (!input.Valid()) return Results.BadRequest(new { error = "Enter valid product details, price, stock and a hex color." });
    var product = await db.Products.FindAsync(id); if (product is null) return Results.NotFound();
    input.Apply(product); await db.SaveChangesAsync(); return Results.Ok(product);
});
admin.MapDelete("/products/{id:int}", async (int id, ShopDb db) =>
{
    var product = await db.Products.FindAsync(id); if (product is null) return Results.NotFound();
    db.Products.Remove(product); await db.SaveChangesAsync(); return Results.NoContent();
});
app.MapPost("/api/orders", async (OrderInput input, ShopDb db) =>
{
    if (string.IsNullOrWhiteSpace(input.CustomerName) || input.CustomerName.Length > 120 ||
        string.IsNullOrWhiteSpace(input.Email) || input.Email.Length > 200 || !System.Net.Mail.MailAddress.TryCreate(input.Email, out _) ||
        string.IsNullOrWhiteSpace(input.Address) || input.Address.Length > 1000 || input.Items is null || input.Items.Count is < 1 or > 100 ||
        input.Items.Any(i => i.Quantity is < 1 or > 1000) || input.Items.Select(i => i.ProductId).Distinct().Count() != input.Items.Count)
        return Results.BadRequest(new { error = "Enter your name, valid email, delivery address and cart items." });
    await using var transaction = await db.Database.BeginTransactionAsync();
    var order = new Order { CustomerName = input.CustomerName.Trim(), Email = input.Email.Trim(), Address = input.Address.Trim() };
    foreach (var item in input.Items)
    {
        var product = await db.Products.FindAsync(item.ProductId);
        if (product is null || product.Stock < item.Quantity) return Results.BadRequest(new { error = "An item is unavailable or has insufficient stock. Refresh the catalog." });
        product.Stock -= item.Quantity;
        order.Items.Add(new OrderItem { ProductName = product.Name, Quantity = item.Quantity, UnitPrice = product.Price });
    }
    order.Total = order.Items.Sum(i => i.UnitPrice * i.Quantity);
    db.Orders.Add(order); await db.SaveChangesAsync(); await transaction.CommitAsync();
    return Results.Ok(new { order.Id, order.Total });
});
admin.MapGet("/orders", async (ShopDb db) => await db.Orders.Include(o => o.Items).OrderByDescending(o => o.Id).ToListAsync());
admin.MapPatch("/orders/{id:int}", async (int id, StatusInput input, ShopDb db) =>
{
    if (!new[] { "Pending", "Processing", "Shipped", "Delivered" }.Contains(input.Status)) return Results.BadRequest(new { error = "Invalid order status." });
    var order = await db.Orders.FindAsync(id); if (order is null) return Results.NotFound();
    order.Status = input.Status; await db.SaveChangesAsync(); return Results.Ok(order);
});
app.Run();

class ShopDb(DbContextOptions<ShopDb> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();
}
class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public string Description { get; set; } = "";
    public string Size { get; set; } = "";
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public string Color { get; set; } = "#ded5c3";
    public string ImageUrl { get; set; } = "";
}
class Order
{
    public int Id { get; set; }
    public string CustomerName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Address { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "Pending";
    public decimal Total { get; set; }
    public List<OrderItem> Items { get; set; } = [];
}
class OrderItem
{
    public int Id { get; set; }
    public string ProductName { get; set; } = "";
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
record CartItem(int ProductId, int Quantity);
record OrderInput(string CustomerName, string Email, string Address, List<CartItem>? Items);
record StatusInput(string Status);
record ProductInput(string Name, string Category, string Description, string Size, decimal Price, int Stock, string Color, string? ImageUrl = null)
{
    public bool Valid() => !string.IsNullOrWhiteSpace(Name) && Name.Length <= 120 && !string.IsNullOrWhiteSpace(Category) && Category.Length <= 60 &&
        !string.IsNullOrWhiteSpace(Size) && Size.Length <= 80 && Description is { Length: <= 1000 } && Price > 0 && Price <= 1000000 && decimal.Round(Price, 2) == Price && Stock >= 0 &&
        Color is not null && System.Text.RegularExpressions.Regex.IsMatch(Color, "^#[0-9a-fA-F]{6}$") &&
        (string.IsNullOrEmpty(ImageUrl) || (ImageUrl.Length <= 1000 && (System.Text.RegularExpressions.Regex.IsMatch(ImageUrl, "^/images/[a-zA-Z0-9._-]+$") || (Uri.TryCreate(ImageUrl, UriKind.Absolute, out var uri) && uri.Scheme == "https"))));
    public void Apply(Product p) { p.Name = Name.Trim(); p.Category = Category.Trim(); p.Description = Description.Trim(); p.Size = Size.Trim(); p.Price = Price; p.Stock = Stock; p.Color = Color; p.ImageUrl = ImageUrl ?? ""; }
}
