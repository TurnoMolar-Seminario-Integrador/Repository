namespace Domain.Model
{
    // Alias / Compatibilidad con código previo
    public class Consulta : AtencionOdontologica
    {
        public Consulta() : base() { }

        public Consulta(int codAtencion, DateTime inicio, DateTime fin, string observaciones, int codTurno, DateTime fechaReserva, int nroHC, string pacienteTipoDoc, int pacienteNroDoc)
            : base(codAtencion, inicio, fin, observaciones, codTurno, fechaReserva, nroHC, pacienteTipoDoc, pacienteNroDoc)
        {
        }
    }
}
