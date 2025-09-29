using Microsoft.AspNetCore.Authentication.Cookies;
using Portal_Refeicoes.Services;
using System;

var builder = WebApplication.CreateBuilder(args);

// Adicionar servi�os ao cont�iner.
builder.Services.AddRazorPages();

// Configurar sess�o
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Configurar autentica��o por Cookies para o Portal
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
    // CORRE��O: L� a chave aninhada "ApiSettings:ApiBaseUrl"
    client.BaseAddress = new Uri(builder.Configuration["ApiSettings:ApiBaseUrl"]);
});

var app = builder.Build();

// Configurar o pipeline de requisi��es HTTP.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Habilitar sess�o
app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

// Exige autoriza��o para todas as p�ginas, exceto as marcadas com [AllowAnonymous]
app.MapRazorPages().RequireAuthorization();

app.Run();