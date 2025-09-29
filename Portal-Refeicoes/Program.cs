using Microsoft.AspNetCore.Authentication.Cookies;
using Portal_Refeicoes.Services;
using System;

var builder = WebApplication.CreateBuilder(args);

// Adicionar serviços ao contêiner.
builder.Services.AddRazorPages();

// Configurar sessão
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Configurar autenticação por Cookies para o Portal
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login";
        options.LogoutPath = "/Logout";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });

// Configurar o HttpClient e o ApiClient
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient<ApiClient>(client =>
{
    // CORREÇÃO: Lê a chave aninhada "ApiSettings:ApiBaseUrl"
    client.BaseAddress = new Uri(builder.Configuration["ApiSettings:ApiBaseUrl"]);
});

var app = builder.Build();

// Configurar o pipeline de requisições HTTP.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Habilitar sessão
app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

// Exige autorização para todas as páginas, exceto as marcadas com [AllowAnonymous]
app.MapRazorPages().RequireAuthorization();

app.Run();