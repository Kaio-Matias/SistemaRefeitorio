namespace ApiRefeicoes.Models
{
    public class Colaborador
    {
        public int Id { get; set; } // Chave Primária
        public string CartaoPonto { get; set; }
        public string Nome { get; set; }
        public byte[]? Foto { get; set; } // Armazena a foto como array de bytes
        public string? AzureId { get; set; } // Armazena o ID retornado pela Azure Face API

        public int DepartamentoId { get; set; }
        public Departamento Departamento { get; set; }

        public int FuncaoId { get; set; }
        public Funcao Funcao { get; set; }
    }
}