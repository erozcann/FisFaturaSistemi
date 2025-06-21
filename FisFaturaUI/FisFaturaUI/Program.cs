var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// HttpClient ayarı
builder.Services.AddHttpClient("FisFaturaAPI", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ApiSettings:BaseUrl"]);
    client.DefaultRequestHeaders.Accept.Clear();
    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
});

// SESSION ekle
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = "FisFatura.Session";
});

// Layout'ta session kullanabilmek için bu satır gerekiyor
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

// IWebHostEnvironment'ı enjekte etmek için ekleyin
builder.Services.AddSingleton<IWebHostEnvironment>(builder.Environment);

// MVC
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// SESSION kullanıma al
app.UseSession();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
