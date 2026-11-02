using Domain.Model;

namespace Data
{
    public interface IPacienteRepository
    {
        Task<Paciente> AddAsync(Paciente paciente);
        Task<bool> DeleteAsync(string tipoDocumento, string nroDocumento);
        Task<bool> DeleteAsync(int nroDocumento);
        Task<Paciente?> GetAsync(string tipoDocumento, string nroDocumento);
        Task<Paciente?> GetAsync(int nroDocumento);
        Task<IEnumerable<Paciente>> GetAllAsync();

        // Lanza ConflictoConcurrenciaException si otro usuario modificó los datos personales del paciente
        // (nombre, apellido, fecha de nacimiento, teléfono, email o domicilio) entre que se leyó y este guardado.
        Task<bool> UpdateAsync(Paciente paciente);
        Task<IEnumerable<Paciente>> GetByCriteriaAsync(PacienteCriteria criteria);

        // CUF01, paso 3: registra al paciente y su historia clínica (inicialmente vacía) en una única
        // transacción: o quedan las dos filas o ninguna (postcondición de fracaso: no se crea ni el
        // paciente ni la historia clínica). Devuelve la historia clínica con el NroHC asignado.
        Task<HistoriaClinica> AddConHistoriaClinicaAsync(Paciente paciente, HistoriaClinica historiaClinica);

        // CUF02: rectifica el tipo y/o número de documento, que son la clave del paciente (y la que usa
        // para ingresar). Guarda también los cambios pendientes del paciente (sus datos personales) y
        // traslada su historia clínica y sus turnos a la clave nueva, todo en una única transacción. Lanza
        // ConflictoConcurrenciaException (sin cambios) si otro usuario modificó al paciente.
        Task CambiarDocumentoAsync(Paciente paciente, string nuevoTipoDocumento, string nuevoNroDocumento);
    }
}
