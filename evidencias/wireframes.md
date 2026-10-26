# Evidencias de diseño — wireframes

Este archivo reúne los cuatro bocetos textuales de `diseno-del-juego.md` para consulta rápida. Son wireframes de propuesta, no capturas de una aplicación implementada.

## 1. Menú principal

```text
┌────────────────────────────────────────────────────────────┐
│ NEXUS // EXPLORACIÓN DE REALIDADES                          │
│                                                            │
│                   N E X U S                                │
│                La anomalía perdida                         │
│                                                            │
│              [ NUEVA PARTIDA ]                              │
│              [ CONTINUAR      ]                             │
│              [ DOCUMENTACIÓN  ]                             │
│              [ SALIR          ]                             │
└────────────────────────────────────────────────────────────┘
```

## 2. Exploración / mapa

```text
┌────────────────────────────────────────────────────────────┐
│ NEXUS > MAPA                  Vida 100 | Energía 60         │
├───────────────┬─────────────────────────┬───────────────────┤
│ ZONAS         │ MAPA                    │ DETALLES          │
│ Bosque        │ Bosque ── Ruinas        │ Zona actual       │
│ Ruinas        │           \             │ Descripción       │
│ Anomalía      │          Anomalía       │ [Explorar zona]   │
│ Extracción    │              |          │                   │
│               │          Extracción     │                   │
├───────────────┴─────────────────────────┴───────────────────┤
│ [Misiones] [Inventario] [Estado] [Menú]                     │
└────────────────────────────────────────────────────────────┘
```

## 3. Misiones

```text
┌────────────────────────────────────────────────────────────┐
│ NEXUS > MISIONES                                            │
│ La anomalía perdida                                        │
│ [✓] Llegar a la zona de anomalía                            │
│ [ ] Encontrar el núcleo energético                          │
│ [ ] Sobrevivir al encuentro                                 │
│ [ ] Regresar al punto de extracción                         │
│ Progreso: 1 de 4                                            │
│ [Ir al mapa] [Guardar partida] [Volver]                     │
└────────────────────────────────────────────────────────────┘
```

## 4. Explorador / inventario

```text
┌────────────────────────────────────────────────────────────┐
│ NEXUS > EXPLORADOR / INVENTARIO                             │
├───────────────────────────┬────────────────────────────────┤
│ CADETE                    │ INVENTARIO                     │
│ Vida       100            │ Célula de energía | +25        │
│ Energía     60            │ Escudo portátil   | +20        │
│ Experiencia  0 XP         │ Analizador                     │
│                           │ [Usar objeto] [Detalles]       │
├───────────────────────────┴────────────────────────────────┤
│ Mensaje de resultado: “Energía recuperada: +25”             │
│ [Mapa] [Misiones] [Volver]                                  │
└────────────────────────────────────────────────────────────┘
```
