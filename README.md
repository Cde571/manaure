# MANAURE — Isla dinámica para Windows

Edición comunitaria gratuita y de código abierto basada en [GILI](https://github.com/maulanarizky16/gili), de Maulana. Se conserva la licencia MIT original.

## Instalación

Descarga **Manaure-Setup.exe** desde [Releases](https://github.com/Cde571/manaure/releases). Introduce tu nombre y el nombre de tu asistente. No requiere administrador. Windows 10/11 de 64 bits, .NET Framework 4.8. Instalador sin firma digital.

Incluye música y letras, agenda, Pomodoro/Focus, monitor, mascotas, personalización y modos Gaming y Programación. El caballero es un dibujo vectorial propio, sin recursos oficiales de videojuegos. Las sesiones multimedia y el audio dependen de Windows. El clima original solo cubre Indonesia.

## IA local opcional

Instala [Python 3.10+](https://www.python.org/downloads/) (con el lanzador py) y [Ollama](https://ollama.com/download/windows). Ejecuta `ollama pull qwen3.5:4b` y, opcionalmente, `ollama pull qwen2.5:3b`. Inicia Ollama y reinicia MANAURE. El servicio local utiliza 127.0.0.1:18765. Los pesos de modelos no están incluidos; consulta sus licencias. YouTube abre una búsqueda en el navegador; los controles multimedia se envían a Windows. Para voz, instala los componentes de reconocimiento y síntesis en español de Windows.

## Privacidad

La configuración permanece en `%APPDATA%\Manaure`; el historial y workspace en `%LOCALAPPDATA%\Manaure\Data`. El instalador no incluye cuentas, datos ni claves personales. El saludo por micrófono, el historial del portapapeles, el contenido de notificaciones y la ubicación automática están desactivados inicialmente. La captura de pantalla requiere confirmación. Letras, calendario, geolocalización voluntaria y servicios multimedia pueden conectarse a proveedores externos. La IA puede leer archivos del workspace y crear archivos nuevos solo con autorización; no ejecuta comandos arbitrarios. Desinstalar conserva tus datos.

## Compilar

Ejecuta `build.cmd` y `powershell -File build-installer.ps1`. Consulta LICENSE para la licencia MIT. Esta versión preliminar puede presentar diferencias de compatibilidad según el equipo.
