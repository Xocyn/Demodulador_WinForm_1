# 📋 EJEMPLO: Enriquecimiento de MSocorro() con RegistrarCampo()

## 🎯 Objetivo

Demostrar cómo usar `_logger.RegistrarCampo()` en el método `MSocorro()` de la clase `Metodos` para guardar automáticamente todos los campos de emergencia.

---

## 📝 CÓDIGO ACTUAL vs MEJORADO

### ❌ ANTES (Solo UI)

```csharp
public void MSocorro(List<int> mensaje)
{
    string mmsi_receptor = General.newMMSI(mensaje, 4);
    string mmsi_transmisor = General.newMMSI(mensaje, 16);
    string tipo_emergencia = Socorro.Peligro(mensaje[38]);

    _log($"MMSI Transmisor: {mmsi_transmisor}\n");
    _log($"MMSI Receptor: {mmsi_receptor}\n");
    _log($"Tipo de Emergencia: {tipo_emergencia}\n");

    // Los datos se muestran pero NO se guardan en archivo
}
```

**Resultado**: Archivo guarda solo datos genéricos (Tipo, Formato ID, Timestamp)

---

### ✅ DESPUÉS (UI + Persistencia)

```csharp
public void MSocorro(List<int> mensaje)
{
    string mmsi_receptor = General.newMMSI(mensaje, 4);
    string mmsi_transmisor = General.newMMSI(mensaje, 16);
    string tipo_emergencia = Socorro.Peligro(mensaje[38]);
    string coordenadas = Geografica.Posicion(Geografica.Coordenadas(mensaje, 40).Item1);
    string utc = Geografica.newUTC(mensaje, 50);
    string sig_comunicaciones = Socorro.PosteriorCom(mensaje[54]);
    string ack = General.ACK(mensaje[56]);

    // Mostrar en UI
    _log($"MMSI Transmisor: {mmsi_transmisor}\n");
    _logger.RegistrarCampo("MMSI Transmisor", mmsi_transmisor);

    _log($"MMSI Receptor: {mmsi_receptor}\n");
    _logger.RegistrarCampo("MMSI Receptor", mmsi_receptor);

    _log($"Tipo de Emergencia: {tipo_emergencia}\n");
    _logger.RegistrarCampo("Tipo de Emergencia", tipo_emergencia);

    _log($"Coordenadas: {coordenadas}\n");
    _logger.RegistrarCampo("Coordenadas", coordenadas);

    _log($"UTC: {utc}\n");
    _logger.RegistrarCampo("UTC", utc);

    _log($"Siguiente Comunicación: {sig_comunicaciones}\n");
    _logger.RegistrarCampo("Siguiente Comunicación", sig_comunicaciones);

    _log($"{ack}\n");
    _logger.RegistrarCampo("ACK", ack);

    // Ahora también se guardan en el archivo
}
```

**Resultado**: Archivo guarda TODOS los campos del mensaje de emergencia

---

## 📊 COMPARACIÓN DE ARCHIVOS GENERADOS

### ANTES

```
bin/Mensajes/DSC_140125_143025_123_SOCORRO.txt

╔════════════════════════════════════════════════════════════════════╗
║                    MENSAJE DSC DECODIFICADO                        ║
║                        SOCORRO                                      ║
╚════════════════════════════════════════════════════════════════════╝

Tipo:              SOCORRO
Formato ID:        112
Timestamp:         14/01/2025 14:30:25.123

Registrado:        14/01/2025 14:30:25.123
═══════════════════════════════════════════════════════════════════════
```

❌ **Faltan todos los datos específicos del mensaje!**

---

### DESPUÉS (Con RegistrarCampo)

```
bin/Mensajes/DSC_140125_143025_123_SOCORRO.txt

╔════════════════════════════════════════════════════════════════════╗
║                    MENSAJE DSC DECODIFICADO                        ║
║                        SOCORRO                                      ║
╚════════════════════════════════════════════════════════════════════╝

Tipo:                       SOCORRO
Formato ID:                 112
Timestamp:                  14/01/2025 14:30:25.123

MMSI Transmisor:            123456789
MMSI Receptor:              987654321
Tipo de Emergencia:         FUEGO
Coordenadas:                40°N 10°E
UTC:                        12:35
Siguiente Comunicación:     RADIOTELÉFONO
ACK:                        SÍ

Registrado:                 14/01/2025 14:30:25.123
═══════════════════════════════════════════════════════════════════════
```

✅ **¡Todos los datos persistidos automáticamente!**

---

## 🔄 PATRÓN RECOMENDADO

### Estructura Consistente

```csharp
public void MiMetodo(List<int> mensaje)
{
    // 1. Extraer todos los datos
    string campo1 = Extract1(mensaje);
    string campo2 = Extract2(mensaje);
    string campo3 = Extract3(mensaje);

    // 2. Para cada dato: UI + Persistencia
    _log($"Campo 1: {campo1}\n");
    _logger.RegistrarCampo("Campo 1", campo1);

    _log($"Campo 2: {campo2}\n");
    _logger.RegistrarCampo("Campo 2", campo2);

    _log($"Campo 3: {campo3}\n");
    _logger.RegistrarCampo("Campo 3", campo3);
}
```

---

## 💡 MÉTODOS CANDIDATOS

### MSocorro() ⭐ PRIORITARIO

Campos a registrar:
```csharp
_logger.RegistrarCampo("MMSI Transmisor", mmsi_transmisor);
_logger.RegistrarCampo("MMSI Receptor", mmsi_receptor);
_logger.RegistrarCampo("Categoría", categoria);
_logger.RegistrarCampo("Tipo de Emergencia", tipoEmergencia);
_logger.RegistrarCampo("Coordenadas", coordenadas);
_logger.RegistrarCampo("UTC", utc);
_logger.RegistrarCampo("Siguiente Comunicación", sig_comunicaciones);
_logger.RegistrarCampo("ACK", ack);
```

### MGeografica()

Campos a registrar:
```csharp
_logger.RegistrarCampo("Área", area);
_logger.RegistrarCampo("Categoría", categoria);
_logger.RegistrarCampo("MMSI", mmsi);
_logger.RegistrarCampo("Primer Telemando", primer_tel);
_logger.RegistrarCampo("Frecuencia Canal 1", frec_canal_1);
_logger.RegistrarCampo("Frecuencia Canal 2", frec_canal_2);
_logger.RegistrarCampo("ACK", ack);
```

### MIndividual()

Campos a registrar:
```csharp
_logger.RegistrarCampo("MMSI Transmisor", mmsi_transmisor);
_logger.RegistrarCampo("MMSI Receptor", mmsi_receptor);
_logger.RegistrarCampo("Categoría", categoria);
_logger.RegistrarCampo("Primer Telemando", primer_tel);
_logger.RegistrarCampo("Segundo Telemando", segundo_tel);
_logger.RegistrarCampo("ACK", ack);
```

### MGrupos()

Campos a registrar:
```csharp
_logger.RegistrarCampo("MMSI Transmisor", mmsi_transmisor);
_logger.RegistrarCampo("Categoría", categoria);
_logger.RegistrarCampo("Primer Telemando", primer_tel);
_logger.RegistrarCampo("ACK", ack);
```

### MAllShips()

Campos a registrar:
```csharp
_logger.RegistrarCampo("MMSI Transmisor", mmsi_transmisor);
_logger.RegistrarCampo("Categoría", categoria);
_logger.RegistrarCampo("Primer Telemando", primer_tel);
_logger.RegistrarCampo("ACK", ack);
```

---

## 📈 IMPACTO

| Aspecto | Sin RegistrarCampo | Con RegistrarCampo |
|---------|-------------------|-------------------|
| **Datos en UI** | ✅ Sí | ✅ Sí (igual) |
| **Datos en archivo** | ❌ No | ✅ Sí |
| **Búsqueda** | ❌ Imposible | ✅ Posible |
| **Análisis** | ❌ Limitado | ✅ Completo |
| **Auditoría** | ❌ Parcial | ✅ Completa |

---

## 🚀 PASOS PARA IMPLEMENTAR

### 1. Identificar el Método
```
Ejemplo: public void MSocorro(List<int> mensaje)
```

### 2. Localizar Todos los _log() Calls
```csharp
_log($"MMSI Transmisor: {mmsi_transmisor}\n");
_log($"MMSI Receptor: {mmsi_receptor}\n");
// ... etc
```

### 3. Agregar RegistrarCampo() Después de Cada _log()
```csharp
_log($"MMSI Transmisor: {mmsi_transmisor}\n");
_logger.RegistrarCampo("MMSI Transmisor", mmsi_transmisor);

_log($"MMSI Receptor: {mmsi_receptor}\n");
_logger.RegistrarCampo("MMSI Receptor", mmsi_receptor);
```

### 4. Compilar y Verificar
```powershell
dotnet build
```

### 5. Testear
- Capturar mensaje de tipo respectivo
- Verificar archivo generado
- Confirmar que incluya TODOS los campos

---

## ✅ CHECKLIST DE IMPLEMENTACIÓN

Para cada método en Metodos:

- [ ] Identificado
- [ ] Localizados todos los _log() calls
- [ ] Agregado _logger.RegistrarCampo() correspondiente
- [ ] Compilación exitosa
- [ ] Testing completado
- [ ] Documentado

---

## 📝 EJEMPLO COMPLETO: MSocorro()

```csharp
public void MSocorro(List<int> mensaje)
{
    string mmsi_receptor = General.newMMSI(mensaje, 4);
    string categoria = General.Categoria(mensaje[14]);
    string mmsi_transmisor = General.newMMSI(mensaje, 16);
    string primer_tel = General.PrimerTelemando(mensaje[26], out bool sol_posicion);

    string mmsi_socorro = General.newMMSI(mensaje, 28);
    string tipoEmergencia = Socorro.Peligro(mensaje[38]);
    string coordenadas = Geografica.Posicion(Geografica.Coordenadas(mensaje, 40).Item1);
    string utc = Geografica.newUTC(mensaje, 50);
    string sig_comunicaciones = Socorro.PosteriorCom(mensaje[54]);
    string ack = General.ACK(mensaje[56]);

    // Mostrar y guardar
    _log($"Formato: {FormatSpecifier.Formato(112)}\n");
    _logger.RegistrarCampo("Formato", "SOCORRO");

    _log($"MMSI Transmisor: {mmsi_transmisor}\n");
    _logger.RegistrarCampo("MMSI Transmisor", mmsi_transmisor);

    _log($"MMSI Receptor: {mmsi_receptor}\n");
    _logger.RegistrarCampo("MMSI Receptor", mmsi_receptor);

    _log($"Categoría: {categoria}\n");
    _logger.RegistrarCampo("Categoría", categoria);

    _log($"Primer Telemando: {primer_tel}\n");
    _logger.RegistrarCampo("Primer Telemando", primer_tel);

    _log($"MMSI Socorro: {mmsi_socorro}\n");
    _logger.RegistrarCampo("MMSI Socorro", mmsi_socorro);

    _log($"Tipo de Emergencia: {tipoEmergencia}\n");
    _logger.RegistrarCampo("Tipo de Emergencia", tipoEmergencia);

    _log($"Coordenadas: {coordenadas}\n");
    _logger.RegistrarCampo("Coordenadas", coordenadas);

    _log($"UTC: {utc}\n");
    _logger.RegistrarCampo("UTC", utc);

    _log($"Siguiente Comunicación: {sig_comunicaciones}\n");
    _logger.RegistrarCampo("Siguiente Comunicación", sig_comunicaciones);

    _log($"{ack}\n");
    _logger.RegistrarCampo("ACK", ack);
}
```

---

## 🎯 RESULTADO ESPERADO

```
Cuando se decodifique un mensaje de SOCORRO:

MAINDISPLAY (igual que antes):
├─ Formato: SOCORRO
├─ MMSI Transmisor: 123456789
├─ MMSI Receptor: 987654321
├─ Categoría: Buque de carga
├─ ... etc ...
└─ ✓ ECC correcto

ARCHIVO GENERADO (MEJORADO):
├─ Tipo: SOCORRO
├─ Formato ID: 112
├─ Timestamp: 14/01/2025 14:30:25.123
├─ MMSI Transmisor: 123456789    ← NUEVO!
├─ MMSI Receptor: 987654321      ← NUEVO!
├─ Categoría: Buque de carga     ← NUEVO!
├─ Tipo de Emergencia: FUEGO     ← NUEVO!
├─ Coordenadas: 40°N 10°E        ← NUEVO!
├─ UTC: 12:35                    ← NUEVO!
├─ Siguiente Comunicación: RADIO ← NUEVO!
├─ ACK: SÍ                       ← NUEVO!
└─ Registrado: 14/01/2025 14:30:25.123
```

---

**Status**: ✅ EJEMPLO Y PATRÓN DEFINIDO  
**Próximo Paso**: Implementar en MSocorro() y otros métodos  
**Date**: 2025-01-14
