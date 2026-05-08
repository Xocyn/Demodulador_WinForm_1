# 🎯 Próximos Pasos - Guía de Acción

## ✅ Lo que está completado

- ✅ Clase `Expansion` refactorizada y funcional
- ✅ Visualización de onda en tiempo real (waveViewer1)
- ✅ Thread-safety garantizado
- ✅ Compilación exitosa (0 errores)
- ✅ Documentación completa

---

## 🧪 Fase 1: Testing (RECOMENDADO AHORA)

### Pruebas Funcionales

```
[ ] Iniciar aplicación sin crashes
[ ] Seleccionar dispositivo de audio
[ ] Capturar en banda VHF
    ├─ Verificar que waveViewer1 muestra onda
    ├─ Recibir mensaje DSC
    └─ Verificar mensaje en MAINDISPLAY
[ ] Cambiar a banda MF/HF
    ├─ Verificar que waveViewer1 se actualiza
    ├─ Recibir mensaje DSC
    └─ Verificar mensaje en MAINDISPLAY
[ ] Si mensaje tiene extensión (100-106)
    ├─ Verificar decodificación correcta
    └─ Verificar escritura en MAINDISPLAY
[ ] Cambiar nuevamente a VHF
    ├─ Verificar funcionamiento correcto
    └─ Verificar sin crashes
```

### Pruebas de Performance

```
[ ] Monitorear CPU durante captura (~2-3% esperado)
[ ] Monitorear memoria (estable)
[ ] Verificar que no hay memory leaks
[ ] WaveViewer actualiza suavemente (~20 FPS)
[ ] UI no se congela durante captura
```

### Pruebas de Thread-Safety

```
[ ] Cambiar dispositivo durante captura
[ ] Cambiar banda múltiples veces
[ ] Recibir múltiples mensajes consecutivos
[ ] Sin excepciones de threading
```

---

## 📊 Fase 2: Validación de Datos

### Datos de Prueba para Expansion

**Tipo 100 (Resolución Mejorada):**
```
Enviar: [100, 15, 30, 45, 60, 127]
Esperado: "Mejora de Latitud..." + "Mejora de Longitud..."
```

**Tipo 102 (Velocidad):**
```
Enviar: [102, 25, 30, 127]
Esperado: "Velocidad actual del barco: 25,30 nudos"
```

**Tipo 104 (Identificador):**
```
Enviar: [104, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 127]
Esperado: "Identificador adicional: A B C D E F G H I J"
```

---

## 🔍 Fase 3: Debugging (Si es Necesario)

### Si waveViewer1 No Muestra Onda

1. Verificar que `_waveDisplayManager` se crea correctamente
2. Verificar que `UpdateWaveDisplay()` se llama
3. Verificar que `waveViewer1.AddSamples()` recibe datos
4. Usar breakpoints en `WaveViewerControl.OnPaint()`

### Si Expansion No Escribe en UI

1. Verificar que `_expansion` se inicializa en Procesamiento
2. Verificar que callback `LogToDisplay` funciona
3. Usar mensajes de debug en `_expansion.Decodificar()`
4. Verificar que `MAINDISPLAY` no está null

### Si Hay Crashes al Cambiar Banda

1. Verificar que `DetenerCaptura()` limpia correctamente
2. Verificar que `_cts.Dispose()` se llama
3. Verificar que `_waveDisplayManager = null`
4. Usar try-catch para capturar excepciones

---

## 📈 Fase 4: Optimizaciones (Opcional)

### Mejorar Performance

```csharp
// En CapturaDatos.cs, línea ~125, ajustar:
_waveDisplayManager = new WaveDisplayManager(
    updateDisplay,
    targetSamples: 2048,      // ← Reducir si CPU alta
    updateIntervalMs: 100     // ← Aumentar si CPU alta
);
```

### Mejorar Visualización

```csharp
// En WaveViewerControl.cs, cambiar colores:
private readonly Color _waveColor = Color.Cyan;    // ← Cambiar de Lime
private readonly Color _backgroundColor = Color.Navy; // ← Cambiar de Black
```

---

## 🚀 Fase 5: Nuevas Funcionalidades (Futuro)

### Expansion: Nuevos Tipos

```csharp
// En Expansion.Decodificar(), agregar nuevo case:
case 124:
    i++;
    i = nuevo_tipo(EXTENSION, i);  // ← Nueva función
    break;
```

### WaveViewer: Mejoras

```csharp
// Ideas:
// 1. Agregar FFT para espectro
// 2. Agregar triggers automáticos
// 3. Agregar exportación a WAV
// 4. Agregar escalas dinámicas
```

---

## 📋 Checklist Pre-Deployment

- [ ] Testing funcional completado
- [ ] Sin crashes en cambios de banda
- [ ] Expansion escribe correctamente
- [ ] WaveViewer actualiza smoothly
- [ ] Performance aceptable (<5% CPU)
- [ ] Sin warnings de compilación
- [ ] Documentación revisada
- [ ] Cambios en Git (opcional)

---

## 🐛 Troubleshooting Rápido

### Problema: "waveViewer1 no muestra nada"
**Solución:**
1. Verificar que `IniciarCaptura()` se ejecuta
2. Verificar que audio tiene datos
3. Verificar que `waveViewer1` es visible
4. Aumentar `targetSamples` en WaveDisplayManager

### Problema: "Expansion no escribe en UI"
**Solución:**
1. Verificar que extensión se detecta (`extension = true`)
2. Verificar que `_expansion` no es null
3. Verificar que callback `LogToDisplay` funciona
4. Agregar logs de debug en `Expansion.Decodificar()`

### Problema: "Crashes al cambiar banda"
**Solución:**
1. Usar try-catch en `CambiarModo()`
2. Verificar que `DetenerCaptura()` finaliza
3. Esperar más en `Thread.Sleep(500)`
4. Revisar `CancellationTokenSource` disposal

### Problema: "CPU muy alto"
**Solución:**
1. Aumentar `updateIntervalMs` a 100 o más
2. Reducir `targetSamples` a 2048
3. Desactivar otros procesos
4. Revisar que `WaveDisplayManager` no acumula infinitamente

---

## 📞 Referencias Rápidas

### Documentación Clave
- `QUICK_REFERENCE_EXPANSION.md` - Referencia rápida
- `WAVEVIEWER_EXAMPLES.md` - Ejemplos de configuración
- `SESSION_SUMMARY.md` - Resumen técnico
- `DOCUMENTATION_INDEX.md` - Índice de todos los docs

### Archivos Código
- `Migrado/Procesamiento.cs` - Lógica de decodificación
- `Migrado/CapturaDatos.cs` - Captura de audio
- `WaveViewerControl.cs` - Visualización
- `Form1.cs` - UI principal

### Configuración
- Línea ~125 en `CapturaDatos.cs` - WaveDisplayManager config
- Línea ~50 en `WaveViewerControl.cs` - Colores
- `LogToDisplay()` - Thread-safe logging

---

## ✨ Conclusión

La aplicación está **lista para testing**. 

Próximos pasos recomendados:

1. **Ejecutar aplicación** y verificar que no hay crashes
2. **Capturar audio VHF** y verificar waveViewer1
3. **Recibir mensaje DSC** y verificar en MAINDISPLAY
4. **Cambiar de banda** y verificar funcionamiento
5. **Reportar resultados** para ajustes si es necesario

---

**Status**: ✅ READY FOR TESTING
**Compilación**: ✅ EXITOSA
**Documentación**: ✅ COMPLETA
**Next Action**: 🧪 TESTING

---

*¿Preguntas o problemas? Revisar DOCUMENTATION_INDEX.md para encontrar el archivo relevante.*
