namespace ApiRefeicoes.Dtos
{
    public class ColaboradorResponseDto
    {
        public int Id { get; set; }
        public string Nome { get; set; }
        public string CartaoPonto { get; set; }
        public string Funcao { get; set; }
        public string Departamento { get; set; }
        public bool Ativo { get; set; }
        public byte[]? Foto { get; set; }
        public int FuncaoId { get; set; }
        public int DepartamentoId { get; set; }
        public bool AcessoCafeDaManha { get; set; } = false;
        public bool AcessoAlmoco { get; set; } = false;
        public bool AcessoJanta { get; set; } = false;
        public bool AcessoCeia { get; set; } = false;
    }
}