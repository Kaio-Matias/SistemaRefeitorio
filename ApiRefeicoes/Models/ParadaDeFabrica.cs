namespace ApiRefeicoes.Models
{
    public class ParadaDeFabrica
    {
        public int Id { get; set; }

        // Ajustar para que por padrão seja false
        public bool? Parada { get; set; }
        public DateTime? DataParada { get; set; }
        public Usuario? Usuario { get; set; }  


    }
}
