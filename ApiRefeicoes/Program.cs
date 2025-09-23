// ApiRefeicoes/Program.cs

using ApiRefeicoes.Data;
using ApiRefeicoes.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Adicionar services ao contêiner.
builder.Services.AddControllers();

// Configuração do DbContext para o Entity Framework Core
builder.Services.AddDbContext<ApiRefeicoesDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configuração da autenticação JWT
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
        ValidateIssuer = false, // Em produção, considere validar o Issuer
        ValidateAudience = false // Em produção, considere validar a Audience
    };
});


// ==================================================================
// INÍCIO DA CORREÇÃO
// ==================================================================
// Registrando os serviços no sistema de injeção de dependência.
// Estas linhas informam à API como criar os serviços quando eles forem necessários.
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<FaceApiService>(); // <-- ADICIONE ESTA LINHA
// ==================================================================
// FIM DA CORREÇÃO
// ==================================================================


// Configuração do Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configurar o pipeline de requisições HTTP.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseDeveloperExceptionPage(); // Adiciona a página de exceção de desenvolvedor
}

app.UseHttpsRedirection();

// Adiciona o middleware de autenticação e autorização
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();