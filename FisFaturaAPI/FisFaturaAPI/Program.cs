using FisFaturaAPI.Data; // Context sınıfı
using FisFaturaAPI.Services; // OCR servisi
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ✅ 1. SQL Server bağlantısını ekle
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ✅ 2. HttpClient + OCR servisinin DI kaydı
builder.Services.AddHttpClient<OcrService>();

// ✅ 3. CORS (eğer frontend ayrı portta çalışıyorsa)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

builder.Services.AddHttpClient();
builder.Services.AddScoped<IOcrService, OcrService>();


// ✅ 4. API ve Swagger servisleri
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// ✅ 5. Development ortamı ise Swagger'ı aktif et
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// ✅ 6. CORS aktif et
app.UseCors("AllowAll");

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
