namespace Domain.Model
{
  public class HorarioOdont
  {
    public string HoraDesde { get; private set; }
    public string HoraHasta { get; private set; }
    public string DiaSemana { get; private set; }


    public HorarioOdont(string horadesde, string horahasta, string diasemana)
    {
      SetHoraD(horadesde);
      SetHoraH(horahasta);
      SetDiaS(diasemana);

    }


    public void SetHoraD(string horadesde)
    {
      if (string.IsNullOrWhiteSpace(horadesde))
        throw new ArgumentException("El horario desde no puede ser nulo o vacío.", nameof(horadesde));
      HoraDesde = horadesde;
    }

    public void SetHoraH(string horahasta)
    {
      if (string.IsNullOrWhiteSpace(horahasta))
        throw new ArgumentException("El horario hasta no puede ser nulo o vacío.", nameof(horahasta));
      HoraHasta = horahasta;
    }
    public void SetDiaS(string diasemana)
    {
      if (string.IsNullOrWhiteSpace(diasemana))
        throw new ArgumentException("El dia de la semana no puede ser nulo o vacío.", nameof(diasemana));
      DiaSemana = diasemana;
    }
  }
}
