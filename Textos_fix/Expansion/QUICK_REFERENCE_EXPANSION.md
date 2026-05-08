# 🚀 Quick Reference - Expansion Class

## Resumen Rápido

La clase `Expansion` ahora es una **clase de instancia** que decodifica extensiones DSC y escribe en la UI mediante callbacks thread-safe.

---

## Instanciación

```csharp
// En Procesamiento.cs
private readonly Expansion _expansion;

public Procesamiento(RichTextBox mainDisplay)
{
    _expansion = new Expansion(LogToDisplay);  // ← Callback thread-safe
}
```

---

## Uso

```csharp
// Automático cuando hay extensión
if (extension)
{
    _expansion.Decodificar(MENSAJE_EXT);  // ← Escribe en MAINDISPLAY
}
```

---

## Tipos Soportados

| Código | Función |
|--------|---------|
| 100 | Resolución mejorada de posición |
| 101 | Origen y punto de referencia GPS |
| 102 | Velocidad actual del barco |
| 103 | Ruta actual del barco |
| 104 | Identificador adicional (10 chars) |
| 105 | Zona geográfica ampliada (12 bytes) |
| 106 | Número de personas a bordo |

---

## Callback Pattern

```csharp
// El callback es la clave del diseño
Action<string> logCallback = (message) => 
{
    // Thread-safe: automáticamente usará Invoke() si está en UI thread
    _mainDisplay.AppendText(message);
};

var expansion = new Expansion(logCallback);
```

---

## Ejemplos Rápidos

### Petición de Datos
```
Input:  [100, 110, 127]
Output: "Peticion de datos\n"
```

### Sin Datos
```
Input:  [102, 126, 127]
Output: "Ningun dato disponible\n"
```

### Velocidad
```
Input:  [102, 25, 30, 127]
Output: "Velocidad actual del barco: 25,30 nudos\n"
```

---

## Testing

```csharp
// Sin dependencias de UI
var messages = new List<string>();
var expansion = new Expansion(msg => messages.Add(msg));

expansion.Decodificar(new List<int> { 102, 25, 30, 127 });

Assert.Contains("Velocidad actual del barco", messages[0]);
```

---

## Thread-Safety

✅ Automático mediante callback
✅ Usa `Invoke()` si está en thread de audio
✅ Seguro para captura desde audio device

---

## Documentación Completa

- 📖 `EXPANSION_CLASS_REFACTOR.md` - Detalles técnicos
- 💡 `EXPANSION_EXAMPLES.md` - Casos de uso
- 📋 `EXPANSION_FINAL_SUMMARY.md` - Resumen ejecutivo

---

**Estado**: ✅ COMPILADO Y FUNCIONANDO
