using Data;
using Domain.Model;
using DTOs;
using Microsoft.EntityFrameworkCore;

namespace Application.Services
{
    // =========================================================================
    // CUF09 - ABASTECER INSUMOS Y MATERIALES (E4.R01, v1.02)
    // Actor primario: Responsable de la clínica.
    // Camino básico: 1. listado (nombre, costo unitario, stock); 2. alta con nombre, costo y stock inicial,
    //   validando que el nombre no exista; 3. registra el insumo, habilitado para su uso en las atenciones.
    // Alt 2.a: nombre repetido -> mensajeInsumoYaRegistrado(d).
    // Alt 3.a: reponer stock -> suma la cantidad e informa el nuevo total.
    // Alt 3.b: modificar el costo unitario "sin afectar los montos ya calculados en atenciones previas"
    //   (RN16): cada DetalleInsumoUtilizado guarda su costoUnitarioAlMomento.
    // Validaciones que el CU no detalla y salen del MDF: CostoUnitario CHECK > 0, StockDisponible CHECK >= 0.
    // =========================================================================
    public interface IAbastecerInsumosService
    {
        Task<IReadOnlyList<InsumoListadoDTO>> ListarAsync();
        Task<ResultadoOperacionDTO> RegistrarAsync(string? nombre, decimal? costoUnitario, int? stockInicial);
        Task<ResultadoOperacionDTO> ReponerStockAsync(int idInsumo, int? cantidadIncorporada);
        Task<ResultadoOperacionDTO> ModificarCostoAsync(int idInsumo, decimal? costoUnitario);
    }

    public class AbastecerInsumosService : IAbastecerInsumosService
    {
        public const string MensajeInsumoYaRegistrado = "Ya existe un insumo registrado con ese nombre.";
        public const string MensajeAjusteCostoExitoso = "El costo unitario del insumo fue actualizado correctamente.";
        public const string MensajeNombreRequerido = "Ingresá el nombre del insumo.";
        public const string MensajeCostoInvalido = "El costo unitario debe ser un monto mayor a cero.";
        public const string MensajeStockInvalido = "El stock inicial no puede ser negativo.";
        public const string MensajeCantidadInvalida = "La cantidad a incorporar debe ser mayor a cero.";
        public const string MensajeInsumoInexistente = "No se encontró el insumo indicado.";

        private readonly TurnoMolarDbContext _contexto;

        public AbastecerInsumosService(TurnoMolarDbContext contexto) => _contexto = contexto;

        // Paso 1: sListadoInsumos = 0{nombre + costoUnitario + stockDisponible}N
        public async Task<IReadOnlyList<InsumoListadoDTO>> ListarAsync() =>
            await _contexto.Insumos.AsNoTracking()
                .OrderBy(i => i.Nombre)
                .Select(i => new InsumoListadoDTO
                {
                    IdInsumo = i.IdInsumo,
                    Nombre = i.Nombre,
                    CostoUnitario = i.CostoUnitario,
                    StockDisponible = i.StockDisponible
                })
                .ToListAsync();

        public async Task<ResultadoOperacionDTO> RegistrarAsync(string? nombre, decimal? costoUnitario, int? stockInicial)
        {
            // Paso 2: eDatosNuevoInsumo = nombre + costoUnitario + stockDisponible.
            var nombreNormalizado = nombre?.Trim();
            if (string.IsNullOrEmpty(nombreNormalizado))
                return ResultadoOperacionDTO.Error("2", MensajeNombreRequerido);

            // Alt 2.a.
            var nombreMinusculas = nombreNormalizado.ToLower();
            if (await _contexto.Insumos.AnyAsync(i => i.Nombre.ToLower() == nombreMinusculas))
                return ResultadoOperacionDTO.Error("2.a.1", MensajeInsumoYaRegistrado);

            if (costoUnitario is null or <= 0m)
                return ResultadoOperacionDTO.Error("2", MensajeCostoInvalido);
            if (stockInicial is null or < 0)
                return ResultadoOperacionDTO.Error("2", MensajeStockInvalido);

            // Paso 3: sConfirmacionAltaInsumo = nombre + costoUnitario + stockDisponible.
            var insumo = new Insumo(nombreNormalizado, costoUnitario.Value, stockInicial.Value);
            _contexto.Insumos.Add(insumo);
            await _contexto.SaveChangesAsync();
            return ResultadoOperacionDTO.Ok(
                $"Insumo registrado: {insumo.Nombre}, costo unitario ${insumo.CostoUnitario:N2}, stock disponible {insumo.StockDisponible}.", "3");
        }

        public async Task<ResultadoOperacionDTO> ReponerStockAsync(int idInsumo, int? cantidadIncorporada)
        {
            // Alt 3.a.1: eReposicionStock = nombre + cantidadIncorporada.
            var insumo = await _contexto.Insumos.FindAsync(idInsumo);
            if (insumo == null)
                return ResultadoOperacionDTO.Error("3.a.1", MensajeInsumoInexistente);
            if (cantidadIncorporada is null or <= 0)
                return ResultadoOperacionDTO.Error("3.a.1", MensajeCantidadInvalida);

            insumo.ReponerStock(cantidadIncorporada.Value);
            await _contexto.SaveChangesAsync();

            // sConfirmacionReposicionStock = stockDisponible.
            return ResultadoOperacionDTO.Ok($"Stock de {insumo.Nombre} actualizado. Nuevo stock disponible: {insumo.StockDisponible}.", "3.a.1");
        }

        public async Task<ResultadoOperacionDTO> ModificarCostoAsync(int idInsumo, decimal? costoUnitario)
        {
            // Alt 3.b.1: eAjusteCostoUnitario = nombre + costoUnitario.
            var insumo = await _contexto.Insumos.FindAsync(idInsumo);
            if (insumo == null)
                return ResultadoOperacionDTO.Error("3.b.1", MensajeInsumoInexistente);
            if (costoUnitario is null or <= 0m)
                return ResultadoOperacionDTO.Error("3.b.1", MensajeCostoInvalido);

            insumo.SetCostoUnitario(costoUnitario.Value);
            await _contexto.SaveChangesAsync();
            return ResultadoOperacionDTO.Ok(MensajeAjusteCostoExitoso, "3.b.1");
        }
    }
}
