namespace Domain.Model
{
    public class PacienteCriteria
    {
        public string Texto { get; }
        public PacienteCriteria(string texto)
        {
            Texto = texto;
        }
    }
}