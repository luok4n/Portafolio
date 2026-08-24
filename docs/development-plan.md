# Plan de desarrollo revisado

Este documento es el plan **activo**. Sustituye a
[`original-development-plan.md`](original-development-plan.md), que se conserva sin modificar para
poder rastrear qué cambió y por qué.

- **Fecha de revisión:** 2026-08-24
- **Fuente de contenido:** `Sebastian_Velez_CV_Updated.pdf` (local, no versionado)
- **Repositorio:** https://github.com/luok4n/Portafolio

---

## 1. Qué cambió respecto al plan original

| # | Cambio | Motivo | Referencia |
|---|---|---|---|
| 1 | El soporte bilingüe EN/ES pasa de "fuera del MVP" (§36) a **requisito de primer nivel** | Impacta modelo de datos, API, rutas y SEO; añadirlo después es caro | [ADR-0001](adr/0001-bilingual-content.md) |
| 2 | Kubernetes, registry y cloud se mueven al final, tras una **evaluación explícita de hosting** | Decisión del autor: verificar primero si Kubernetes aporta valor real al proyecto | Fases 11–13 |
| 3 | El frontend pasa de SPA a **prerender (SSG)** con fallback de contenido estático | SEO, previews en LinkedIn/WhatsApp, y resiliencia ante un backend dormido | [ADR-0002](adr/0002-frontend-rendering.md) |
| 4 | Se elimina la sección **Certifications** del MVP | El CV no contiene certificaciones; una sección vacía resta | — |
| 5 | El **formulario de contacto** pasa a fase opcional | Requiere proveedor de email y antispam; en el MVP basta email + LinkedIn | Fase 14 |
| 6 | Se añade **política de privacidad de contenido**: sin teléfono público, sin PDF del CV en el repo, clientes solo con información pública y trazable | El repositorio es público e indexable | [ADR-0003](adr/0003-content-privacy.md) |
| 7 | Se fijan versiones concretas: **.NET 10 LTS, Angular 22, PostgreSQL 17** | El plan decía "LTS disponible"; conviene dejarlo escrito | [environment.md](environment.md) |
| 8 | Se añade `content/` con archivos bilingües y trazabilidad de fuentes | Necesario para el seed y para la revisión de contenido | Fase 1 |
| 9 | Se añade una sección **Engineering** que documenta cómo está construido el sitio, con cifras generadas desde el repositorio | Pedido del autor: es un portafolio de un ingeniero backend y debe demostrar el flujo completo, no solo presentarlo | [ADR-0005](adr/0005-engineering-section.md) |

Todo lo demás del plan original se mantiene: Clean Architecture pragmática, un solo bounded context,
sin sobrearquitectura, Conventional Commits, y el protocolo Inspect → Understand → Plan → Implement
→ Test → Review → Document → Report.

## 2. Decisiones tomadas con el autor (2026-08-24)

| Decisión | Elección |
|---|---|
| Idioma por defecto | **Inglés** (`/en`), español con paridad completa (`/es`) |
| Datos de contacto públicos | **Email + LinkedIn**; el teléfono no se publica |
| Archivos fuente en el repo | El plan sí; el PDF original del CV no; se genera una versión redactada descargable |
| Renderizado del frontend | **Prerender / SSG + nginx** |
| Commits | Conventional Commits, **sin** trailer de coautoría, con cuerpo que documente el cambio |

## 3. Fases

Cada fase termina con un estado ejecutable o verificable, un commit y una entrada en el
[CHANGELOG](../CHANGELOG.md). No se avanza de fase si la anterior no compila o no tiene forma
reproducible de ejecutarse.

### Fase 0 — Repositorio y entorno ✅

**Objetivo:** dejar el repositorio listo para trabajar y documentar el punto de partida.

- [x] `git init`, rama `main`, remoto `origin`.
- [x] `.gitignore`, `.editorconfig`, `LICENSE`, `README.md`, `CHANGELOG.md`.
- [x] Estructura `content/`, `docs/`, `infra/`, `src/`.
- [x] `docs/environment.md` con versiones verificadas.
- [x] ADR-0001, ADR-0002, ADR-0003.
- [x] Plan original preservado + plan revisado.

**Criterio de aceptación:** el repositorio se clona y se entiende sin contexto adicional.

### Fase 1 — Contenido desde el CV ✅

**Objetivo:** contenido estructurado, bilingüe, trazable y aprobado por el autor.

- [x] `content/cv-source.md` — texto del CV extraído, sin modificar.
- [x] `content/profile.{en,es}.json`, `experience`, `projects`, `skills`, `education`, `social-links`.
- [x] Enriquecimiento de clientes y proyectos con fuentes públicas citadas.
- [x] `content/content-review.md` — inconsistencias detectadas y su resolución.
- [x] Aprobación explícita del autor sobre la traducción al español.

**Criterio de aceptación:** cada bloque de contenido tiene origen identificable (CV o fuente pública
con URL). Nada inventado.

### Fase 2 — Diseño funcional e i18n ✅

Entregable: [functional-design.md](functional-design.md).

- [x] Secciones definitivas: Hero, About, Experience, Skills, Projects, **Engineering**, Education, Contact.
- [x] Mapa de rutas por locale (`/en/...`, `/es/...`) y comportamiento del selector de idioma.
- [x] Claves de traducción de UI y convención de nombres.
- [x] Metadatos SEO por locale, `hreflang`, canonical, sitemap.
- [x] Wireframe textual y jerarquía visual.

### Fase 3 — Backend .NET 10 ✅

Entregable: `src/services/portfolio-api`. Decisiones en [ADR-0004](adr/0004-backend-architecture.md).

- [x] Solución con `Portfolio.Api`, `Portfolio.Application`, `Portfolio.Domain`, `Portfolio.Infrastructure`, `Portfolio.Tests`.
- [x] Endpoints de lectura resueltos por locale.
- [x] OpenAPI/Scalar, manejo global de excepciones, correlation id, logging estructurado, validación, CORS, health checks (`/health/live`, `/health/ready`).

**Criterio de aceptación:** `dotnet run` levanta la API con Swagger y datos en memoria.

### Fase 4 — PostgreSQL + EF Core ✅

- [x] Modelo de 20 tablas con traducciones `(entity_id, language_code)`.
- [x] Configuraciones Fluent API, migración inicial, índices y check constraints.
- [x] Seed reproducible alimentado desde `content/`, con huella para no reescribir sin cambios.
- [x] Verificación de paridad entre fuente de archivos y base de datos (`tools/api/parity-check.mjs`).
- [ ] Tests de integración automatizados contra PostgreSQL — Fase 6, con Testcontainers.

**Criterio de aceptación:** base recreable desde cero con un comando, sin inserts manuales.

### Fase 5 — Frontend Angular 22

- [ ] Aplicación con layout y secciones de la fase 2, incluida la de Engineering con sus diagramas SVG.
- [ ] Transloco para strings de UI; contenido desde la API.
- [ ] Prerender por ruta y locale; snapshot de contenido como fallback.
- [ ] Estados de carga, error y "contenido en caché".
- [ ] Responsive y accesibilidad básica.

**Criterio de aceptación:** el sitio se ve completo en ambos idiomas con la API apagada.

### Fase 6 — Tests

- [ ] Backend: unitarios (dominio, aplicación, validaciones) e integración (endpoints, persistencia, cadena de fallback de locale).
- [ ] Frontend: servicios, componentes críticos, estados de error/carga y cambio de idioma.

### Fase 7 — Docker y Compose

- [ ] Dockerfile multi-stage para API y para frontend (build → nginx), usuario no-root.
- [ ] `docker-compose.yml` con frontend, API y PostgreSQL.

**Criterio de aceptación:** `docker compose up --build` levanta el sistema completo.

### Fase 8 — CI (GitHub Actions)

- [ ] Build, tests, analizadores/lint, build de imágenes, escaneo de secretos y dependencias.
- [ ] El pipeline falla si no compila, si fallan tests o si hay hallazgos críticos.

### Fase 9 — Observabilidad y seguridad

- [ ] Logging estructurado con requestId, endpoint, status y duración.
- [ ] Métricas: request count, error count, latencia.
- [ ] `docs/security.md`: secretos, CORS, HTTPS, validación, rate limiting, headers, imágenes actualizadas, mínimo privilegio.

### Fase 10 — Pulido del portafolio

- [ ] SEO bilingüe, Open Graph, favicon, sitemap, robots.
- [ ] Revisión Lighthouse, accesibilidad y mobile.
- [ ] Diagrama de arquitectura en `docs/diagrams/`.
- [ ] `docs/architecture.md` completo y ADRs al día.

### Fase 11 — Decisión de hosting  🔸 *punto de decisión*

**Objetivo:** decidir con datos, no por defecto, dónde vive el sitio y si Kubernetes aporta valor.

- [ ] Comparativa de opciones gratuitas y de pago para: frontend estático, API .NET y PostgreSQL.
- [ ] Costo mensual estimado de cada opción, con fecha de verificación de precios.
- [ ] Evaluación honesta de Kubernetes: qué demuestra en una entrevista frente a lo que cuesta operar.
- [ ] `docs/adr/0006-hosting.md` con la decisión y el motivo.

**Salida posible:** que Kubernetes no se justifique y el proyecto lo documente como decisión
consciente — lo cual es en sí mismo una buena respuesta de arquitectura.

### Fase 12 — Kubernetes y CD  *(condicional a la fase 11)*

- [ ] Namespace, Deployments, Services, ConfigMaps, Secrets, Ingress, probes, requests/limits.
- [ ] Cluster local (Docker Desktop, kind o minikube).
- [ ] Container registry con tags por `git-sha`, nunca solo `latest`.
- [ ] Workflow de CD.

### Fase 13 — Despliegue, dominio y HTTPS

- [ ] Despliegue en el proveedor elegido.
- [ ] DNS, HTTPS y renovación automática de certificados.
- [ ] Smoke tests post-despliegue.
- [ ] Procedimiento documentado para apagar o eliminar recursos y su costo asociado.

### Fase 14 — Opcionales

Solo si aportan valor real: formulario de contacto con proveedor de email y antispam, analytics
respetuoso con la privacidad, blog, panel de administración.

## 4. Datos confirmados por el autor (2026-08-24)

| Punto | Resolución |
|---|---|
| Nombre de la empresa | **Adagetech S.A.S.** |
| *Argos One* y *Linkvest* | Pertenecen a **Adagetech** |
| Solapamiento LendingFront / AES Chivor | Se representa como **trabajo paralelo** (freelance) |
| Hueco Feb – Sep 2019 | **Ya no existe**: lo cubre la UTP (Ene – Oct 2019), ausente del CV |
| Situación actual | **Buscando activamente** → el hero lo refleja |
| Año de grado (UTP) | **2018** |
| Años de experiencia | **Meses únicos trabajados ÷ 12**, calculado en tiempo de build. Con la UTP incluida: 102 meses → **8 años** (ago 2026) |
| Cliente Comfama (MVM) | **Comfama**, Caja de Compensación Familiar de Antioquia |
| Proyecto Woldev / Gobernación | **Sitio web de la Gobernación de Risaralda**, PHP heredado. No es SimuDat Salud |
| UTP | **Ene – Oct 2019**, Java + Spring Boot y Angular, programa RIAS del Ministerio de Salud. Entra al timeline y **al CV** |

**Proyectos por empresa** (fuente: autor + investigación pública en
[`content/clients-research.md`](../content/clients-research.md)):

- **Adagetech S.A.S.** — *Argos ONE* (soporte y desarrollo para Estados Unidos, Colombia y otros
  países) y *Linkvest* (reportes mensuales y trimestrales sobre las inversiones de cada cliente).
- **AES Chivor** — *MOA*, plataforma de compra y venta de energía con generación de reportes en Excel.
- **LendingFront** — *Marketplace BackOffice*; equipos de **desarrollo, innovación y optimización**.
- **MVM Ingeniería de Software** — *CHIVOR XM*, *Slang* y **Comfama**.
- **Woldev S.A.S.** — *Teach at Home* y un proyecto con la **Gobernación de Risaralda**.
- **Universidad Tecnológica de Pereira** — *RIAS* (Rutas Integrales de Atención en Salud) para el
  **Ministerio de Salud**. ⚠️ No figura en el CV.

## 5. Preguntas abiertas (bloquean la fase 1)

1. **Cargo y responsabilidades en la UTP.** Falta el título del puesto y las viñetas de
   responsabilidades para el CV y el timeline.
2. **Aporte concreto en Comfama** (cliente de MVM), para poder describirlo.
3. **Aprobación de la traducción al español** de todo el contenido, una vez redactado.

## 5.1 Tarea derivada — actualizar el CV

El autor pidió actualizar el CV, que omite la experiencia en la UTP. El CV corregido debe:

- Añadir **Universidad Tecnológica de Pereira, Ene – Oct 2019** (Java, Spring Boot, Angular; RIAS).
- Añadir **Java** y **Spring Boot** a Technical Skills.
- Añadir el sector **salud pública** (*public health*) al resumen profesional.
- Actualizar **"7+ years"** a **"8+ years"** (102 meses únicos a agosto de 2026).
- Mantener el resto del contenido y el formato del PDF original.

Se genera desde `content/` como HTML con estilos de impresión y se convierte a PDF con Chrome
headless, de modo que el CV sea reproducible y quede versionado como código, no como binario
editado a mano. Se producen dos variantes: una completa para postulaciones y otra **sin teléfono**
para publicar en el sitio, según [ADR-0003](adr/0003-content-privacy.md).

## 6. Convenciones

**Commits** — Conventional Commits, en inglés, sin trailer de coautoría, con cuerpo que explique
qué cambió y por qué:

```text
feat(api): add locale-aware experience endpoint
docs(adr): record bilingual content decision
chore(repo): scaffold repository structure
```

**Documentación** — README y ADRs en inglés (audiencia del repositorio); planes de trabajo en
español. Toda decisión arquitectónica relevante se registra como ADR numerado.

**Ramas** — `main` siempre desplegable; el trabajo de cada fase entra por rama y Pull Request una
vez exista CI (fase 8).
