# IA.md — Uso de Inteligencia Artificial

## Prompts de Felipe (ítems 6-10, benchmark)
**Herramienta utilizada:** Claude (Anthropic), vía chat web.
**Alcance:** Orientación paso a paso para la implementación de los ítems 6–10 (Core HTTP Protocol Improvements), guía para ejecutar e interpretar los benchmarks (docs/benchmark-before.md / docs/benchmark-after.md) y ayuda para solucionar errores encontrados.

A continuación, la lista de prompts y consultas utilizadas, en orden cronológico.

## 1. Análisis inicial del proyecto
* "Tengo que hacer este taller el cual mi compañero ya avanzo, indicandome que ya hizo la mitad. Me podrias revisar el codigo, ver el enunciado del taller y darme una guia de que deberia hacer en cada apartado?, la parte bonus la dejaremos para mas adelante"

## 2. Ítem 6 — Compresión (Brotli + Gzip)
* "ya mira, tengo que hacer el punto 6 pero la verdad no tengo idea de como empezar, me das un paso a paso de q deberia hacer?"
* "revisalo a ver si lo hice bien"
* "como puedo probar esto en la consola para confirmar q de verdad esta comprimiendo?"
* "dame el commit para subirlo a git"
*(Varios prompts pidiendo ayuda para entender por qué curl no funcionaba bien en PowerShell y cómo arreglar el puerto, hasta confirmar que servía).*

## 3. Benchmark — "before"
* "ahora necesito hacer eso del before y after, que es el apache bench y q tengo que hacer con eso?"
* "estoy tratando de instalarlo pero no me resulta, me guias un poco con esto del WSL?"
*(Varios prompts de troubleshooting donde la IA guio en la instalación de Apache Bench en Windows/WSL).*
* "ya logre sacar los datos, pero me ayudas a entender que significan todos esto? salio bien?"
* "donde guardo esta evidencia y como seria el commit solo para subir el before?"

## 4. Ítem 7 — CORS
* "empecemos con el item 7, q hay q hacer aqui exactamente?"
* "esta bien esto?
* "como se prueba esto?"

## 5. Ítem 8 — Content negotiation de errores
* "sigamos con el 8, orientame un poco de como se hace esto"
* "como puedo revisar si lo q hice esta todo bien?"
* "dame el commit para esto"

## 6. Ítem 9 — Cookie hardening y resolución de bugs
* "vamos con el 9"
* "como pruebo esto?"
* "oye probando esto, me tira el error net::ERR_INCOMPLETE_CHUNKED_ENCODING. q paso? como lo arreglarlo?"
*(La IA explicó que era un problema de escritura de headers y guio para arreglar el ResponseTimingMiddleware.cs).*
* "dame los commits para el 9 y para este fix"

## 7. Ítem 10 — Status codes condicionales (301/307/302)
* "pasemos al 10, q tengo q hacer con los status codes?"
* "me tiro un error, que significa y como lo arreglo?"
* "como pruebo esto?"
* "dame el commit final de este punto"

## 8. Benchmark — "after" y corrección final
* "ahora toca la parte del after, q tengo q hacer?"
* "me puedes revisar esto y decirme si esta bien?"
* "como arreglo eso?"
* "ya lo arregle y saque los datos nuevos. me ayudas a comparar los resultados del before y el after para entender q mejoro y analizar q significan estos resultados?"
* "por ultimo, armame el commit de esto"

## 9. Bonus — Health check y crawler control (ítems 14 y 15)
* "indicame que tengo que hacer en el item 14 y 15"
* "esto es lo que hice, ayudame a revisar si todo funciona bien"
* "dame los commits para subirlo"
---

### Resumen de uso
Debido a la falta de conocimiento previo en varias de las tecnologías y conceptos requeridos, la IA se utilizó como un tutor interactivo y guía paso a paso para:

* **Orientación inicial:** Explicar de forma sencilla qué pedía el taller en cada apartado y dar los primeros pasos lógicos para empezar a programar.
* **Validación constante:** Revisar el código escrito por mí antes de probarlo para confirmar que la lógica estuviera correcta o detectar errores de novato.
* **Guía de pruebas (Testing):** Entregar los comandos exactos (ej. `curl`) y enseñar cómo interpretar los resultados en consola para comprobar que la compresión, los CORS y los status codes funcionaban.
* **Troubleshooting y resolución de bugs:** Explicar el origen de errores técnicos complejos (como el choque entre el middleware del compañero y la compresión) y dar la solución paso a paso.
* **Manejo de Git:** Redactar todos los mensajes de commit en inglés siguiendo el estándar del proyecto.
* **Análisis de Benchmarks:** Ayudar a instalar las herramientas de medición, explicar qué significaban las métricas obtenidas y redactar la comparativa técnica final.

## Prompts de Jorge (ítems 1–5 y 11–12)
**Herramienta utilizada:** ChatGPT (OpenAI), vía chat web.
**Alcance:** Validación de la implementación de los ítems 1–5 y 11–12 (Core HTTP Protocol Improvements), resolución de errores y apoyo para pruebas.

## 1. Ítem 1 — Security headers
* "¿Cómo implemento los security headers en ASP.NET Core?"
* "¿Qué headers son los recomendados y para qué sirve cada uno?"
* "¿Cómo puedo comprobar con curl que los headers realmente se están enviando?"

## 2. Ítem 2 — Response timing
* "¿Qué middleware debería crear para agregar un header X-Response-Time?"
* "¿Cómo puedo validar que el tiempo de respuesta se está enviando correctamente?"

## 3. Ítem 3 — Cookie-backed authentication
* "¿Cómo inicio sesión utilizando CookieAuthentication?"
* "¿Cómo protejo las páginas que requieren autenticación?"
* "¿Está bien configurado el Login y Logout?"
* "¿Cómo puedo comprobar que la cookie se crea correctamente?"

## 4. Ítem 4 — Server-side session storage
* "¿Qué significa utilizar un SessionStore para las cookies?"
* "¿Cómo implemento un TicketStore en memoria?"

## 5. Ítem 5 — Rate limiting
* "¿Cómo verifico que devuelve HTTP 429 y Retry-After?"

## 6. Ítem 11 — HTTP/2 server push analysis + preload
* "¿Qué debería escribir en el documento http2-preload-analysis.md?"

## 7. Ítem 12 — Request tracing (X-Request-Id)
* "¿Cómo hago para reutilizar el Request ID si el cliente ya lo envía?"
* "¿Cómo agrego el RequestId al contexto de Serilog?"
* "¿Cómo modifico el outputTemplate para que aparezca el RequestId?"
* "¿Cómo puedo comprobar que el mismo RequestId aparece en los logs y en la respuesta HTTP?"

---

### Resumen de uso
La IA fue utilizada principalmente como una herramienta de apoyo para comprender conceptos de ASP.NET Core y aplicar correctamente las funcionalidades solicitadas por el taller.

* **Comprensión de conceptos:** Explicar el funcionamiento de middlewares, autenticación basada en cookies, SessionStore, Rate Limiter, Request Tracing y Preload de recursos.
* **Revisión del código:** Verificar que las implementaciones cumplieran con los requisitos del enunciado antes de realizar las pruebas.
* **Resolución de errores:** Ayudar a identificar problemas de configuración, errores de compilación y conflictos surgidos durante la integración con cambios realizados por el compañero.
* **Validación de funcionamiento:** Proporcionar comandos (`curl`) y procedimientos para comprobar el correcto funcionamiento de headers, autenticación, rate limiting, preload y request tracing.
