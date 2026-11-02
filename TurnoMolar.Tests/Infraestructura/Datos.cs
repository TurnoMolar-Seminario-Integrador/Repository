using Data;
using Domain.Model;

namespace TurnoMolar.Tests.Infraestructura
{
    // Datos del seed (DbInitializer) que usan los tests.
    public static class Datos
    {
        // Pacientes
        public const string PacienteOsde = "34567890";      // Manuel Fernández, OSDE
        public const string PacienteSwiss = "38999111";     // Laura Gómez, SWISS
        public const string PacienteParticular = "29888777"; // Carlos Rossi, sin obra social

        // Odontólogos y su disponibilidad
        public const string OdontologoA = "28456789"; // esp. 1 lunes y martes 8-13; esp. 3 viernes 9-18
        public const string OdontologoB = "30123456"; // esp. 2 miércoles 14-19
        public const string OdontologoC = "26789012"; // esp. 3 jueves 9-18

        public const int General = 1;      // arancel particular 15000; OSDE 18000
        public const int Endodoncia = 2;
        public const int Ortodoncia = 3;

        // Próxima fecha (a partir de mañana + 2 días, para cumplir RN19) que cae en ese día de la semana.
        public static DateTime Proximo(DayOfWeek dia, int hora, int minuto = 0)
        {
            var f = DateTime.Today.AddDays(3);
            while (f.DayOfWeek != dia) f = f.AddDays(1);
            return f.AddHours(hora).AddMinutes(minuto);
        }

        public static Turno AgregarTurno(TurnoMolarDbContext ctx, DateTime fecha, string paciente, string odontologo, int especialidad,
            string modalidad = "PARTICULAR", string estado = "RESERVADO")
        {
            var t = new Turno(0, fecha, modalidad, especialidad, "DNI", odontologo, "DNI", paciente, estado);
            ctx.Turnos.Add(t);
            ctx.SaveChanges();
            return t;
        }
    }
}
