
using System;

namespace ApiRefeicoes.Models
{
    public class Dispositivo
    {
        public int Id { get; set; }

        // Identificador único gerado pelo app cliente (MAUI, por exemplo)
        public string DeviceIdentifier { get; set; }

        // Nome amigável para o dispositivo, ex: "Celular de Kaio"
        public string Nome { get; set; }

        public DateTime UltimoLogin { get; set; }

        public bool IsAtivo { get; set; } = true;

        // Chave estrangeira para o Usuário
        public int UsuarioId { get; set; }
        public Usuario Usuario { get; set; }
    }
}