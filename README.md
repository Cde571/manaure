# MANAURE — Isla dinámica para Windows

MANAURE transforma la interacción con Windows en una experiencia dinámica, inteligente y personalizada. Una isla dinámica de código abierto y uso gratuito que integra inteligencia artificial local opcional, mascotas animadas, herramientas de productividad y controles multimedia. Disponible bajo licencia MIT.

## Instalación

Descarga **Manaure-Setup.exe** desde [Releases](https://github.com/Cde571/manaure/releases). Introduce tu nombre y el nombre de tu asistente. No requiere administrador. Windows 10/11 de 64 bits, .NET Framework 4.8. Instalador sin firma digital.

Incluye música y letras, agenda, Pomodoro/Focus, monitor, mascotas, personalización y modos Gaming y Programación. El caballero es un dibujo vectorial propio, sin recursos oficiales de videojuegos. Las sesiones multimedia y el audio dependen de Windows. El clima original solo cubre Indonesia.

## Hora, ubicación y alarma de oración

La pestaña del reloj permite **Usar hora de Windows** con un clic o **Elegir zona horaria** buscando entre las zonas instaladas en el equipo. No hay países ni ciudades preseleccionados. El reloj de la isla y la alarma usan la zona elegida, con sus cambios de horario de verano.

**Usar mi ubicación** detecta y guarda una ciudad aproximada a partir de la IP, solo al pulsarlo. Requiere conexión a internet y no cambia la zona horaria elegida. Puede mostrar la ubicación de una VPN. El servicio de clima original sigue limitado a Indonesia.

La única alarma de oración es **Dale gracias a Jesús**, todos los días a las **3:00 p. m. (15:00)**, con un timbre sencillo y un interruptor para desactivarla. Funciona con MANAURE abierto y el equipo despierto. Ya no se calculan horarios religiosos por coordenadas ni se muestran avisos previos, recordatorios de ayuno o fechas religiosas. Las preferencias anteriores de activar o desactivar oración se conservan; el aviso no se repite al reiniciar la app durante el mismo día.

## IA local opcional

Instala [Python 3.10+](https://www.python.org/downloads/) (con el lanzador py) y [Ollama](https://ollama.com/download/windows). Ejecuta `ollama pull qwen3.5:4b` y, opcionalmente, `ollama pull qwen2.5:3b`. Inicia Ollama y reinicia MANAURE. El servicio local utiliza 127.0.0.1:18765. Los pesos de modelos no están incluidos; consulta sus licencias. YouTube abre una búsqueda en el navegador; los controles multimedia se envían a Windows. Para voz, instala los componentes de reconocimiento y síntesis en español de Windows.

## Privacidad

La configuración permanece en `%APPDATA%\Manaure`; el historial y workspace en `%LOCALAPPDATA%\Manaure\Data`. El instalador no incluye cuentas, datos ni claves personales. El saludo por micrófono, el historial del portapapeles, el contenido de notificaciones y la ubicación automática están desactivados inicialmente. La captura de pantalla requiere confirmación. Letras, calendario, geolocalización voluntaria y servicios multimedia pueden conectarse a proveedores externos. La IA puede leer archivos del workspace y crear archivos nuevos solo con autorización; no ejecuta comandos arbitrarios. Desinstalar conserva tus datos.

## Compilar

Ejecuta `build.cmd` y `powershell -File build-installer.ps1`. Consulta LICENSE para la licencia MIT. Esta versión preliminar puede presentar diferencias de compatibilidad según el equipo.

Para comprobar la alarma, la zona horaria, los cambios de horario de verano y los reinicios, ejecuta `powershell -File test-alarm.ps1`.
