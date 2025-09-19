// ApiRefeicoes/Program.cs

using ApiRefeicoes.Data;
using ApiRefeicoes.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Adicionar servi�os ao cont�iner.
builder.Services.AddControllers();

// Configura��o do DbContext para o Entity Framework Core
builder.Services.AddDbContext<ApiRefeicoesDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configura��o da autentica��o JWT
var key = Encoding.ASCII.GetBytes(builder.Configuration["Jwt:Key"]);
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false, // Em produ��o, considere validar o Issuer
        ValidateAudience = false // Em produ��o, considere validar a Audience
    };
});


// ==================================================================
// IN�CIO DA CORRE��O
// ==================================================================
// Registrando o TokenService no sistema de inje��o de depend�ncia.
// Esta linha informa � API como criar o TokenService quando ele for necess�rio.
builder.Services.AddScoped<TokenService>();
// ==================================================================
// FIM DA CORRE��O
// ==================================================================


// Configura��o do Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configurar o pipeline de requisi��es HTTP.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseDeveloperExceptionPage(); // Adiciona a p�gina de exce��o de desenvolvedor
}

app.UseHttpsRedirection();

// Adiciona o middleware de autentica��o e autoriza��o
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();