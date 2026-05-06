# Copilot Instructions

## Directrices del proyecto
- Implementación de visualización de onda en tiempo real: Se agregó WaveViewerControl personalizado para visualizar audio en waveViewer1. Captura muestras en el callback DataAvailable, las convierte de bytes a shorts, y las visualiza con downsampling automático (~20 FPS). El control es thread-safe y usa Invoke() para actualizar desde thread de audio. Configuración: 4096 muestras, actualización cada 50ms. Colores: onda verde (Lime) sobre fondo negro.