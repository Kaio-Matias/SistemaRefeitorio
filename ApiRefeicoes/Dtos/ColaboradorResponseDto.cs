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
    }
}