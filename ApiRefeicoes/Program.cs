using ApiRefeicoes.Data;
using ApiRefeicoes.Services;
using Microsoft.EntityFrameworkCore;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// --- PASSO 1: ADICIONAR SERVI�OS (Ingredientes) ---

// Configura��o do DbContext para o Entity Framework
builder.Services.AddDbContext<ApiRefeicoesDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Registro dos nossos servi�os customizados
builder.Services.AddScoped<FaceApiService>();
// Futuramente, o TokenService tamb�m pode ser registrado aqui se preferir.

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ## C�DIGO DE AUTENTICA��O JWT DEVE FICAR AQUI ##
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
    };
});
builder.Services.AddAuthorization();


// --- PASSO 2: CONSTRUIR A APLICA��O (Assar o bolo) ---
var app = builder.Build();


// --- PASSO 3: CONFIGURAR O PIPELINE DE REQUISI��ES (Decorar o bolo) ---

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// ## AS CHAMADAS "USE" DE AUTENTICA��O DEVEM FICAR AQUI ##
// Importante: UseAuthentication() deve vir ANTES de UseAuthorization()
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();