namespace ApiRefeicoes.Models
{
    public class Colaborador
    {
        public int Id { get; set; } // Chave Primária
        public string CartaoPonto { get; set; }
        public string Nome { get; set; }
        public string? FotoUrl { get; set; } // A foto pode ser opcional inicialmente
        public int DepartamentoId { get; set; }
        public Departamento Departamento { get; set; }

        public int FuncaoId { get; set; }
        public Funcao Funcao { get; set; }
    }
}