# Trazabilidad UML → código

Correspondencia entre los artefactos de la documentación (E2 casos de uso, E3 modelo de dominio, E4 CUF / máquinas de estado / diccionario, E5 modelo de datos) y el código. Esta tabla cubre lo agregado o revisado en la rama `feature/cuf04-cuf12`; los casos anteriores (CUU01–CUU04, CUF01–CUF03, CUF07, CUF08, CUF10) tienen su trazabilidad en los comentarios de cada servicio.

Convención: cada servicio cita el paso del caso de uso en un comentario (`// Alt 3.a.1`, `// Paso 4`) y los mensajes del diccionario de datos son constantes `Mensaje…` con el texto literal.

## CUF04 – Conformar cuerpo de odontólogos (E4.R01 v1.04)

| Artefacto | Elemento | Código | Prueba |
| :--- | :--- | :--- | :--- |
| CU pasos 1-2 | sListadoOdontologos (nombre, apellido, matrícula, estado) | `ConformarCuerpoOdontologosService.ListarAsync` → `OdontologoListadoDTO` | `El_alta_aparece_en_el_listado` |
| CU pasos 3-5 | eDatosNuevoOdontologo + eDisponibilidadHoraria; estado inicial Habilitado | `RegistrarAsync`, `NuevoOdontologoDTO`, `EspecialidadDisponibilidadDTO`, `FranjaHorariaDTO` | `Camino_basico_registra_al_odontologo_habilitado_con_su_disponibilidad` |
| Alt 3.a | mensajeMatriculaExistente | `MensajeMatriculaExistente` | `Alt_3a_rechaza_matricula_repetida` |
| Alt 5.a | eAjusteDisponibilidad; turnos confirmados fuera de franja | `AjustarDisponibilidadAsync`, `MensajeTurnosFueraDeFranja` | `Alt_5a_ajusta_la_disponibilidad`, `Alt_5a_rechaza_si_un_turno_reservado_queda_fuera_de_la_franja` |
| Alt 5.b | agregar / quitar especialidad con turnos pendientes | `AgregarEspecialidadAsync`, `QuitarEspecialidadAsync` | `Alt_5b_agrega_y_quita_especialidad`, `Alt_5b_no_quita_una_especialidad_con_turnos_pendientes`, `Alt_5b_no_quita_la_ultima_especialidad` |
| Alt 5.c | eActualizacionContacto | `ActualizarContactoAsync`, `Odontologo.ActualizarContacto` | `Alt_5c_actualiza_el_contacto` |
| Alt 5.d / RN15 | inhabilitar sin afectar turnos, atenciones ni pagos | `InhabilitarAsync`, `Odontologo.Inhabilitar`; filtro `EstaHabilitado` en `AgendaTurnoService` | `Alt_5d_inhabilitado_sale_de_la_oferta_de_turnos_y_5e_lo_devuelve`, `Alt_5d_conserva_los_turnos_e_informa_los_pendientes` |
| Alt 5.e | rehabilitar | `RehabilitarAsync`, `Odontologo.Rehabilitar` | ídem |
| ME Odontólogo | Habilitado ↔ Inhabilitado | `EstadosOdontologo` | — |
| RN1 / Minuta E1 | lunes a viernes | `ValidarFranjas` (`MensajeDiaInvalido`) | `Rechaza_dias_fuera_de_lunes_a_viernes` |
| MDF | PK DisponibilidadesHorarias = (TipoDoc, NroDoc, DiaSemana, HoraInicio) | `TurnoMolarDbContext` | `Rechaza_franjas_superpuestas_entre_especialidades` |
| GUI | pantalla Odontólogos | `AdministracionController.Odontologos`, `Views/Administracion/Odontologos.cshtml` | prueba manual |

## CUF05 – Consultar valoraciones (E4.R01 v1.02)

| Artefacto | Elemento | Código | Prueba |
| :--- | :--- | :--- | :--- |
| CU paso 1 | sListadoValoraciones del odontólogo | `ConsultarValoracionesService.ConsultarPorOdontologoAsync` → `ValoracionListadoDTO` | `Camino_basico_el_odontologo_ve_solo_sus_valoraciones` |
| CU paso 2 | sDetalleValoracion | `ObtenerDetalleAsync` | `Paso_2_muestra_el_detalle_y_no_deja_ver_el_de_otro_odontologo` |
| Alt 1.a | mensajeSinValoraciones | `MensajeSinValoraciones` | `Alt_1a_informa_que_no_hay_valoraciones` |
| Alt 1.b | sListadoValoracionesClinica (incluye odontólogo) | `ConsultarClinicaAsync` | `Alt_1b_el_responsable_ve_todas_con_el_odontologo` |
| GUI | Mis Valoraciones / Valoraciones | `OdontologoController.MisValoraciones`, `AdministracionController.Valoraciones`, `_ValoracionesTabla.cshtml` | prueba manual |

## CUF06 – Administrar convenios con obras sociales (E4.R01 v1.04)

| Artefacto | Elemento | Código | Prueba |
| :--- | :--- | :--- | :--- |
| CU paso 1 | sListadoObrasSociales (id, nombre, plan, estado) | `AdministrarConveniosService.ListarAsync` → `ObraSocialListadoDTO` | — |
| CU pasos 2-3 | eDatosNuevaObraSocial + eConveniosDefinidos (1..N); estado Disponible | `RegistrarAsync`, `NuevaObraSocialDTO`, `ConvenioDTO` | `Camino_basico_registra_la_obra_social_disponible_con_sus_convenios`, `El_alta_exige_al_menos_un_convenio` |
| Alt 2.a | mensajeObraSocialExistente | `MensajeObraSocialExistente` | `Alt_2a_rechaza_identificador_repetido` |
| Alt 3.a | modificar plan | `ModificarPlanAsync`, `MensajeConfirmacionPlan` | `Alt_3a_modifica_el_plan` |
| Alt 3.b / 3.c | baja (No Disponible) y rehabilitación | `DarDeBajaAsync`, `RehabilitarAsync`, `ObraSocial.DarDeBaja/Rehabilitar` | `Alt_3b_y_3c_baja_y_rehabilitacion`, `La_obra_social_dada_de_baja_deja_de_cubrir_especialidades` |
| Alt 3.d | agregar, modificar, eliminar convenio | `GuardarConvenioAsync`, `EliminarConvenioAsync`, `MensajeConfirmacionConvenio` | `Alt_3d_modifica_y_agrega_convenios`, `Alt_3d_elimina_convenio_sin_turnos`, `No_elimina_un_convenio_con_turnos_reservados_por_obra_social` |
| ME Obra social | Disponible ↔ No Disponible | `EstadosObraSocial` | — |
| MDF | Convenios.ArancelConvenio CHECK > 0 | `MensajeArancelInvalido` | — |

## CUF09 – Abastecer insumos y materiales (E4.R01 v1.02)

| Artefacto | Elemento | Código | Prueba |
| :--- | :--- | :--- | :--- |
| CU paso 1 | sListadoInsumos | `AbastecerInsumosService.ListarAsync` | `Paso_1_lista_los_insumos` |
| CU pasos 2-3 | eDatosNuevoInsumo | `RegistrarAsync` | `Camino_basico_registra_el_insumo` |
| Alt 2.a | mensajeInsumoYaRegistrado | `MensajeInsumoYaRegistrado` | `Alt_2a_rechaza_nombre_repetido` |
| Alt 3.a | eReposicionStock → sConfirmacionReposicionStock | `ReponerStockAsync`, `Insumo.ReponerStock` | `Alt_3a_repone_stock_e_informa_el_total`, `Alt_3a_rechaza_cantidad_no_positiva` |
| Alt 3.b / RN16 | eAjusteCostoUnitario | `ModificarCostoAsync`, `MensajeAjusteCostoExitoso` | `Alt_3b_modifica_el_costo` |
| MDF | CostoUnitario > 0, StockDisponible ≥ 0 | `MensajeCostoInvalido`, `MensajeStockInvalido` | — |

## CUF11 – Coordinar atención extraordinaria (E4.R01 v1.01)

| Artefacto | Elemento | Código | Prueba |
| :--- | :--- | :--- | :--- |
| CU pasos 1-2 | paciente Habilitado y sin turno pendiente (RN8, RN9) | `CoordinarAtencionExtraordinariaService.ValidarPacienteAsync` | `Alt_2a_rechaza_paciente_con_deuda`, `Alt_2b_rechaza_paciente_con_turno_pendiente` |
| CU paso 3 | eDatosAtencionExtraordinaria | `AtencionExtraordinariaRequestDTO`, `RegistrarAsync` | `Rechaza_odontologo_inhabilitado`, `Obra_social_sin_convenio_vigente_se_rechaza` |
| CU paso 4 / RN13 | turno Reservado fuera de la grilla + comprobante con descripcionMaterial | `RegistrarAsync` → `AtencionExtraordinariaResultDTO` | `Camino_basico_registra_el_turno_fuera_de_la_grilla_y_emite_comprobante` |
| RN19 | no aplica | sin validación de antelación | `No_aplica_la_antelacion_minima_RN19` |
| Alt 3.a | el responsable cancela: no se registra nada | botón Cancelar en la vista | prueba manual |
| GUI | Atención Extraordinaria + comprobante | `AtencionExtraordinaria.cshtml`, `ComprobanteExtraordinario.cshtml` | prueba manual |

## CUF12 – Administrar especialidad (E4.R01 v1.01)

| Artefacto | Elemento | Código | Prueba |
| :--- | :--- | :--- | :--- |
| CU pasos 1-3 | eDatosNuevaEspecialidad; mensajeConfirmacionAlta | `AdministrarEspecialidadService.RegistrarAsync` | `Camino_basico_registra_la_especialidad` |
| Alt 1.a | mensajeSobreNombre | `MensajeSobreNombre` | `Alt_1a_rechaza_nombre_repetido_sin_distinguir_mayusculas` |
| Alt 1.b | mensajeSobreArancel | `MensajeSobreArancel` | `Alt_1b_rechaza_arancel_no_positivo` |
| Alt 1.c / RN16 | modificar arancel; mensajeConfirmacionUpdate | `ModificarArancelAsync` | `Alt_1c_modifica_el_arancel`, `Alt_1c2a_rechaza_arancel_invalido_al_modificar` |

## Cierre de jornada (decisión del usuario sobre la ambigüedad A3)

| Artefacto | Elemento | Código | Prueba |
| :--- | :--- | :--- | :--- |
| ME Turno | Reservado → Ausente al vencer el día | `VencimientoTurnosService`, `CierreJornadaHostedService` | `Turno_particular_vencido_pasa_a_ausente_con_arancel_particular`, `No_toca_turnos_de_hoy_ni_otros_estados` |
| RN11 / MD /montoPenalizacion | arancel de convenio u arancel particular | ídem | `Turno_por_obra_social_usa_el_arancel_del_convenio` |
| ME Paciente / RN9 | Habilitado → Inhabilitado, /montoAdeudado | ídem | ídem |

## Casos revisados (ya implementados)

| Caso | Prueba |
| :--- | :--- |
| CUF07 cancelar en término / fuera de término (alt 3.a) | `CUF07_cancela_en_termino_sin_penalizacion`, `CUF07_alt_3a_fuera_de_termino_pide_confirmacion_y_luego_penaliza` |
| CUF08 reprogramar | `CUF08_reprograma_a_un_horario_disponible`, `CUF08_rechaza_un_horario_fuera_de_la_disponibilidad` |
| CUF10 regularizar deuda | `CUF10_pagar_la_deuda_rehabilita_al_paciente`, `CUF10_alt_1a_sin_deuda` |
| CUU03 sin pagos parciales (decisión del usuario) | `CUU03_rechaza_pago_particular_parcial`, `CUU03_acepta_pago_particular_total`, `CUU03_rechaza_aportes_de_obra_social_que_no_suman_el_total` |
