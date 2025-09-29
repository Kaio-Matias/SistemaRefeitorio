namespace ApiRefeicoes.Models
{
    public class Usuario
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string PasswordHash { get; set; }
        public string Role { get; set; }
        public virtual ICollection<Dispositivo> Dispositivos { get; set; } = new List<Dispositivo>();
    }
}