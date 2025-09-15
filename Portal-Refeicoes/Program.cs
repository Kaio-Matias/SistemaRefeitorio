using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// --- ETAPA 1: Adicionar todos os serviços ANTES do builder.Build() ---

builder.Services.AddRazorPages();

// Configuração do HttpClient para se comunicar com a API
builder.Services.AddHttpClient("ApiClient", (serviceProvider, client) =>
{
    var settings = serviceProvider.GetRequiredService<IConfiguration>();
    client.BaseAddress = new Uri(settings.GetValue<string>("ApiSettings:BaseUrl"));
});

// Configuração de Autenticação por Cookies para o Login no Portal
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme) // CORREÇÃO APLICADA AQUI
    .AddCookie(options =>
    {
        options.LoginPath = "/Login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });


// --- ETAPA 2: Construir a aplicação ---
var app = builder.Build();


// --- ETAPA 3: Configurar o pipeline DEPOIS do builder.Build() ---

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.Run();