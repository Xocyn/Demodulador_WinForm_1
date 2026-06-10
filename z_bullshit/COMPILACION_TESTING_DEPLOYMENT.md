# 🔨 Compilación, Testing y Deployment

## 🏗️ Paso 1: Compilación

### Verificación Previa
```
1. Abre el proyecto en Visual Studio
2. Verifica que CapturaDatos.cs esté en: Migrado/CapturaDatos.cs
3. Verifica que el proyecto tenga .NET 10 como target
4. Verifica que C# 14.0 esté habilitado
```

### Compilar
```
Opción A (Visual Studio):
1. Click derecho en proyecto → Build
2. Esperar a "Build succeeded"

Opción B (Command Line):
> dotnet build Demodulador_WinForm_1.csproj
```

### Verificar Éxito
```
✅ Build succeeded
❌ 0 errors
⚠️ 0 warnings (óptimo)
```

---

## 🧪 Paso 2: Testing Inicial (No Instrumentado)

### Test 1: Compilación y Ejecución Básica
```
Procedimiento:
1. Presionar F5 (Debug) o Ctrl+F5 (Release)
2. Esperar a que se abra la ventana principal
3. El programa debe abrir SIN excepciones
4. Verificar que no haya mensajes de error

Resultado esperado:
✅ Ventana de aplicación abierta
✅ Sin crash
✅ Sin logs rojos de error
```

### Test 2: Iniciar/Detener Captura (Básico)
```
Procedimiento:
1. En la aplicación, click en botón "Grabar" o similar
2. Debe aparecer en DISPLAYSECUNDARIO: "[Iniciada captura de audio]"
3. Esperar 3 segundos
4. Click en "Detener"
5. Debe aparecer: "[Detenida captura de audio]"

Resultado esperado:
✅ Logs aparecen en orden correcto
✅ No hay excepciones
✅ El callback inicia grabación de audio
```

### Test 3: Cierre de Aplicación
```
Procedimiento:
1. Iniciar captura (click "Grabar")
2. Esperar 2 segundos
3. Presionar X (cerrar ventana)
4. Observar tiempo de cierre

Resultado esperado:
✅ Cierra en <3 segundos
✅ No queda proceso en background
✅ No hay diálogos "no responde"
```

---

## 📊 Paso 3: Testing de Rendimiento (Instrumentado)

### Setup: Task Manager
```
1. Iniciar Task Manager (Ctrl+Shift+Esc)
2. Ir a pestaña "Performance"
3. Abrir "Resource Monitor" (para detalles de threads)
4. Mantener visible mientras se prueba
```

### Test 4: CPU bajo Captura
```
Procedimiento:
1. Task Manager abierto, pestaña Performance
2. Click "Grabar" en aplicación
3. Observar CPU durante 10 segundos
4. Anotar:
   - CPU promedio
   - Picos máximos
   - Qué thread consume más

Resultado esperado:
✅ CPU promedio: 10-30%
✅ CPU pico: <50%
✅ Thread de NAudio: <10%
✅ audioProcessingThread: 10-25%

Si NO cumple:
❌ CPU >50% → Hay un problema
   → Revisar que callback sea ultra-rápido
   → Verificar que audioProcessingThread se creó
```

### Test 5: Memoria Estable
```
Procedimiento:
1. Abrir Task Manager → Memory tab
2. Anotar memoria inicial
3. Grabar durante 60 segundos
4. Anotar memoria final

Cálculo:
Consumo = Memoria_final - Memoria_inicial

Resultado esperado:
✅ <10MB de incremento
✅ Crecimiento lineal (no exponencial)
✅ Estable después de 30 segundos

Si NO cumple:
❌ >20MB de incremento → Memory leak
   → Revisar BlockingCollection
   → Puede haber buffers sin liberar
```

### Test 6: Detección de Patrones (Si hay audio)
```
Procedimiento:
1. Conectar señal BFSK a dispositivo de audio
2. Click "Grabar"
3. Reproducir señal
4. Observar logs

Resultado esperado:
✅ "[DOT PATTERN detectado]" o "[Valor 125 detectado]"
✅ "[IniciarGrabacion] Fase X bloqueada"
✅ Bits se acumulan correctamente

Logs esperados:
[Iniciada captura de audio]
[DOT PATTERN detectado (fase 0)]
[IniciarGrabacion] Fase 0 bloqueada.
[FinalizarCaptura - SILENCIO] 1200 bits capturados
[Detenida captura de audio]
```

---

## ⚙️ Paso 4: Testing Avanzado (Optional)

### Test 7: Stress Test (Carga Alta)
```
Procedimiento:
1. Iniciar captura
2. Generar audio continuo a máximo nivel
3. Mantener durante 5 minutos
4. Monitorear:
   - CPU
   - Memoria
   - Logs (no deben tener errores)
5. Detener

Resultado esperado:
✅ CPU estable
✅ Memoria estable
✅ Sin "Buffer queue full" warnings
✅ Sin "OutOfMemory" exceptions
```

### Test 8: Pause/Resume
```
Procedimiento:
1. Iniciar captura
2. Click "Pausar"
   → Log: "[Captura pausada]"
3. Esperar 2 segundos (verificar que NO se pierde audio)
4. Click "Reanudar"
   → Log: "[Captura reanudada]"
5. Debe continuar capturando
6. Detener

Resultado esperado:
✅ Pausa/Resume sin problemas
✅ No se pierden buffers durante pausa
✅ Continúa correctamente después
```

### Test 9: Cierre Mientras Procesa
```
Procedimiento:
1. Iniciar captura
2. Verificar que esté capturando activamente
3. Presionar X (cerrar ventana) inmediatamente
4. Observar tiempo de cierre

Resultado esperado:
✅ Cierra en <3 segundos
✅ No hay "no responde"
✅ Threads terminan correctamente
✅ Recursos liberados
```

### Test 10: Concurrencia (Multiple Starts/Stops)
```
Procedimiento:
1. Iniciar captura
2. Esperar 1 segundo
3. Click "Detener"
4. Inmediatamente click "Grabar" (sin pausa)
5. Repetir 10 veces
6. Verificar que no haya estado inconsistente

Resultado esperado:
✅ Cada ciclo funciona
✅ Sin deadlocks
✅ Sin memory leaks
✅ Sin exception thrown
```

---

## 📋 Paso 5: Checklist Final Antes de Producción

### Checklist de Código
- [ ] No hay `using` faltantes
- [ ] `BlockingCollection` está disponible
- [ ] `AudioBufferData` está bien definida
- [ ] `audioProcessingThread` se crea y inicia
- [ ] Callback es simple y rápido
- [ ] `DetenerCaptura()` libera recursos

### Checklist de Compilación
- [ ] Build succeeds sin errores
- [ ] Build succeeds sin warnings (idealmente)
- [ ] Ejecutable es válido
- [ ] Dependencias (.dll) están en lugar correcto

### Checklist de Ejecución
- [ ] Aplicación inicia sin crash
- [ ] Logs aparecen correctamente
- [ ] Captura se puede iniciar/detener
- [ ] Cierre es limpio (<3 segundos)
- [ ] No hay exception unhandled

### Checklist de Rendimiento
- [ ] CPU <40% durante captura activa
- [ ] Memoria estable (no crece indefinidamente)
- [ ] Thread de NAudio <10%
- [ ] audioProcessingThread visible en profiler

### Checklist de Funcionalidad
- [ ] Patrones se detectan correctamente
- [ ] Bits se capturan completamente
- [ ] Mensajes se procesan correctamente
- [ ] Pausa/resume funciona
- [ ] Silencio se detecta

---

## 🚀 Paso 6: Deployment

### Versiones de Release

#### Release Build
```
Visual Studio:
1. Cambiar a "Release" (dropdown en toolbar)
2. Rebuild solution
3. Ejecutable en: bin\Release\net10.0\...

Command Line:
> dotnet build -c Release
```

#### Testing Pre-Release
```
1. Ejecutar todos los tests de la sección anterior
2. Con Release build (más rápido que Debug)
3. Verificar que todo funciona igual
4. Release suele ser más rápido (optimizaciones)
```

### Distribución
```
Archivos necesarios:
✅ Migrado/CapturaDatos.cs (ya compilado en .exe)
✅ Demodulador_WinForm_1.exe
✅ Todas las .dll dependencias (NAudio, etc)
✅ Eventuales archivos de configuración

NO distribuir:
❌ Código fuente (*.cs) - opcional
❌ Archivos Debug (*.pdb) - a menos que sea debug release
❌ Archivos de compilación (obj/, bin/)
```

---

## 📈 Monitoreo Post-Deployment

### Métricas a Monitorear
```
1. CPU: Debe estar <40% bajo carga normal
2. Memoria: Debe ser estable (no crece > 10MB/min)
3. Uptime: Sin crashes aleatorios
4. Logs: Sin excepciones repetidas
5. Detección: % de patrones detectados correctamente
```

### Alarmas (Reaccionar si...)
```
🔴 CRÍTICO:
   - CPU >80%
   - Memoria >500MB
   - Crash aleatorio
   - Pattern detection <80%

🟡 ADVERTENCIA:
   - CPU >60% consistente
   - Memoria >300MB
   - Logs con warnings
   - Lentitud en UI
```

### Acciones Recomendadas
```
Si hay problema:
1. Consultar logs en DISPLAYSECUNDARIO
2. Revisar CHECKLIST_IMPLEMENTACION.md para troubleshooting
3. Aumentar prioridad de audioProcessingThread si CPU baja
4. Aumentar tamaño de BlockingCollection si hay warnings de queue
5. Contactar con soporte técnico si persiste
```

---

## 🔄 Rollback Plan

Si algo funciona mal en producción:

```
Opción 1: Mantener versión anterior
- Tener backup de versión anterior compilada
- Revertir a ella si hay problemas críticos

Opción 2: Actualizar código
- Revisar CHECKLIST_IMPLEMENTACION.md
- Aplicar fix específico
- Recompilar y testear
- Redistribuir

Opción 3: Reportar bug
- Incluir logs de DISPLAYSECUNDARIO
- Incluir CPU/Memory screenshots
- Describir pasos para reproducir
```

---

## 📞 Contacto y Soporte

### Documentación Técnica
```
Para problemas técnicos:
→ CHECKLIST_IMPLEMENTACION.md

Para entender la arquitectura:
→ ANALISIS_TECNICO_REFACTORIZACION.md

Para troubleshooting rápido:
→ QUICK_START.md

Para casos de uso:
→ GUIA_USO_CAPTURADATOS.md
```

### Niveles de Soporte
```
Nivel 1 (Self-service):
- Revisar documentación
- Consultar CHECKLIST_IMPLEMENTACION.md

Nivel 2 (Technical):
- Revisar ANALISIS_TECNICO_REFACTORIZACION.md
- Profiler con Visual Studio
- Debugger paso a paso

Nivel 3 (Expert):
- Revisar código fuente
- Modificar según necesidad
- Recompilar y testear
```

---

## ✅ Resumen de Pasos

1. ✅ Compilar (Build succeeds)
2. ✅ Test Básico (Inicia/Detiene)
3. ✅ Test CPU (Bajo)
4. ✅ Test Memoria (Estable)
5. ✅ Test Funcionalidad (Patrones se detectan)
6. ✅ Release Build (Optimizado)
7. ✅ Deployment (A usuarios)
8. ✅ Monitoreo (Métricas normales)

---

**Testing y Deployment: GUÍA COMPLETA ✅**
Fecha: 2024
Versión: 1.0
