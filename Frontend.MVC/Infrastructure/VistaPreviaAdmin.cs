namespace Frontend.MVC.Infrastructure
{
    // El rol Admin no tiene fila propia en Pacientes (ver DbInitializer): para que pueda recorrer el
    // portal del paciente se le muestra el de "el primer paciente de la base" (vista previa). Las
    // pantallas de solo lectura funcionan igual, pero cualquier operación (agendar, cancelar,
    // reprogramar, pagar la deuda, cambiar la obra social o los datos de contacto) se aplicaba a ese
    // paciente real, que no es el administrador ni sabe que alguien opera sobre su cuenta.
    public static class VistaPreviaAdmin
    {
        public const string MensajeSoloLectura =
            "Estás viendo el portal en vista previa como administrador: no se pueden registrar operaciones sobre la cuenta del paciente que se muestra.";
    }
}
