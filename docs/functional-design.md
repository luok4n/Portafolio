# Diseño funcional

Qué muestra el sitio, cómo se navega, cómo se comporta en dos idiomas y qué consume cada sección.
Se define antes de escribir código para que el modelo de datos (Fase 4) y los componentes (Fase 5)
no se inventen sobre la marcha.

- **Fecha:** 2026-08-24
- **Fase:** 2
- **Depende de:** [ADR-0001](adr/0001-bilingual-content.md) (i18n), [ADR-0002](adr/0002-frontend-rendering.md) (prerender), [ADR-0003](adr/0003-content-privacy.md) (privacidad)

---

## 1. Para quién es

| Audiencia | Qué necesita en 10 segundos | Qué necesita en 2 minutos |
|---|---|---|
| Recruiter técnico | Rol, seniority, stack, disponibilidad | Empresas, años, ubicación, CV descargable |
| Engineering manager / tech lead | Profundidad real: arquitectura, cloud, escala | Proyectos concretos, decisiones técnicas, este mismo repositorio |
| Colega o contacto | Quién es y cómo escribirle | LinkedIn, GitHub |

Consecuencia de diseño: **el hero debe responder lo de la primera columna sin scroll**, y todo lo
demás existe para la segunda. El portafolio no es un blog ni un CV maquetado; es una prueba de que
la persona construye software real.

## 2. Mapa de rutas

```text
/                                   → redirección permanente a /en
/en                                 → home inglés
/es                                 → home español
/en/projects/{slug}                 → detalle de proyecto, inglés
/es/proyectos/{slug}                → detalle de proyecto, español
/en/engineering                     → cómo está construido el sitio, inglés
/es/ingenieria                      → cómo está construido el sitio, español
/en/404, /es/404                    → no encontrado, localizado
sitemap.xml, robots.txt             → sin locale
```

### Por qué el segmento va traducido

`/es/proyectos/argos-one` en vez de `/es/projects/argos-one`: la URL es contenido indexable y una
ruta en inglés dentro de la versión en español es una señal incoherente para buscadores y para el
lector. El costo es un mapa de rutas central en vez de un string literal — unas veinte líneas de
configuración, y hace que el generador de rutas de prerender y el selector de idioma tengan una
única fuente de verdad.

El **slug del proyecto no se traduce**: `argos-one` es un nombre propio. Traducirlo rompería los
enlaces si el nombre cambia de idioma y no aporta nada.

### Home de una sola página

El home es una página con secciones ancladas (`#experience`, `#projects`...), no una ruta por
sección. Es el patrón que un recruiter espera y evita fragmentar el contenido para SEO. Las páginas
de detalle de proyecto sí son rutas reales: tienen contenido propio suficiente para justificarse y
le dan al prerender algo que generar.

## 3. Selector de idioma

Reglas, en orden:

1. El idioma se decide por **URL**. No hay redirección automática por `Accept-Language`: rompería
   las URLs canónicas del prerender y le quita al usuario el control.
2. El selector **preserva la ruta y el ancla**. Estar en `/en/projects/linkvest` y cambiar a español
   lleva a `/es/proyectos/linkvest`, no al home. Estar en `/en#experience` lleva a `/es#experience`.
3. La elección se **persiste** en `localStorage`. En una visita posterior a `/`, la redirección
   respeta esa preferencia; sin preferencia guardada, va a `/en`.
4. `<html lang>` se actualiza con el idioma activo. No es cosmético: los lectores de pantalla
   cambian de voz según ese atributo.
5. El selector muestra **el idioma al que se va**, no el actual — es lo que el usuario busca.

Casos que hay que probar (Fase 6): locale desconocido en la URL, cambio de idioma en una página de
detalle, cambio de idioma con ancla, y primera visita con y sin preferencia guardada.

## 4. Secciones

### 4.1 Hero

```text
┌──────────────────────────────────────────────────────────┐
│  [ ES | EN ]                                             │
│                                                          │
│  Sebastián Vélez Ramírez                                 │
│  Senior .NET Developer                                   │
│                                                          │
│  ● Open to opportunities                                 │
│                                                          │
│  8+ years across energy, fintech, real estate,           │
│  public health and education.                            │
│  C# · .NET · ASP.NET Core · Azure · Angular · SQL Server  │
│                                                          │
│  [ Get in touch ]  [ Download CV ]   in ⟋ GitHub         │
└──────────────────────────────────────────────────────────┘
```

- Los **8+ años se calculan**, nunca se escriben. Misma regla que el CV: meses únicos ÷ 12.
- El badge de disponibilidad se renderiza solo si `profile.availability === "open-to-work"`. Cuando
  cambie de estado, desaparece cambiando un valor en `content/`, no tocando una plantilla.
- Las tecnologías del hero son un subconjunto curado, no la lista completa de skills.

### 4.2 About

Resumen profesional, sectores e idiomas hablados. Es el `summaryTemplate` con los años sustituidos —
el mismo texto que el CV, por construcción.

### 4.3 Experience

Timeline descendente. Cada entrada: cargo, empresa, fechas, duración calculada, viñetas y chips de
tecnologías.

El caso especial es 2022: **LendingFront y AES Chivor corren en paralelo**. Se marcan visualmente
como concurrentes con una etiqueta explícita de trabajo freelance simultáneo. Sin eso, un lector
atento asume que hay un error de datos en el portafolio de un ingeniero, que es peor que no
mostrarlo.

La UTP (Ene–Oct 2019) va como cualquier otra posición. No hay hueco que explicar.

### 4.4 Skills

Las 11 categorías de `skills.json`, con las etiquetas traducidas y los nombres de tecnología sin
traducir. Sin barras de porcentaje ni "nivel 4 de 5": son invenciones no verificables y un
entrevistador técnico las lee como ruido.

### 4.5 Projects

Grilla de tarjetas: nombre, cliente, sector, tecnologías y un resumen corto. Cada tarjeta enlaza a
su página de detalle.

El detalle usa el `summary` largo con la investigación, la contribución del autor, las tecnologías y
**los enlaces a las fuentes públicas**. Mostrar las fuentes no es adorno: es la diferencia entre
"trabajé en una plataforma multinacional" y una afirmación que el lector puede verificar en un clic.

Los proyectos con `publiclySourced: false` (MOA, Teach at Home, sitio de la Gobernación) se muestran sin
bloque de fuentes. No se marcan con ninguna insignia negativa: la ausencia de fuentes es la señal
correcta y suficiente.

### 4.6 Engineering — cómo está construido este sitio

La sección que justifica el proyecto. Un portafolio de un ingeniero **backend** que solo muestra
tarjetas bonitas se contradice a sí mismo; este explica el sistema que lo sirve.

**Qué responde, en este orden:**

1. **Por qué frontend + API + base de datos**, y no un HTML estático. Porque el objetivo no era
   publicar un CV, sino recorrer el flujo completo — modelo de datos, capa de aplicación, contrato
   HTTP, cliente, despliegue — y poder explicar cada decisión.
2. **Dónde está el peso.** El autor se enfoca en backend, y el sitio lo refleja: el trabajo profundo
   está en el dominio, el modelo bilingüe, el contrato de la API y la infraestructura. El frontend
   es correcto y sobrio a propósito, no el escaparate.
3. **La arquitectura**, con diagrama: Angular prerenderizado → nginx → API .NET → PostgreSQL.
4. **Las decisiones**, cada una con su problema, su alternativa descartada y su costo. Enlazan a los
   ADR del repositorio.
5. **Los flujos**: resolución de idioma, carga de contenido con su fallback, y el ciclo
   `content/` → seed → base de datos → API → prerender.
6. **El modelo de datos**: tablas base con los hechos, tablas de traducción por
   `(entity_id, language_code)`.
7. **Las pruebas**: qué se prueba, por qué esos casos, y qué encontró la verificación de paridad.
8. **Operación**: health checks, logging estructurado, correlation ids, CI, seguridad.

**Regla que hace la diferencia:** las cifras de esta sección —número de pruebas, de tablas, de
endpoints, de ADR— **se generan desde el repositorio en tiempo de build**, no se escriben a mano.
Una sección de ingeniería que presume 52 pruebas cuando hay 30 es peor que no tenerla, y es
exactamente el tipo de detalle que un entrevistador técnico verifica. Ver
[ADR-0005](adr/0005-engineering-section.md).

**Estructura visual:**

```text
┌──────────────────────────────────────────────────────────┐
│  How this site is built                                  │
│                                                          │
│  Why a frontend, an API and a database                   │
│  ─ prosa corta, 2 párrafos                               │
│                                                          │
│  [ 52 tests ] [ 20 tables ] [ 8 endpoints ] [ 5 ADRs ]   │
│    ↑ generados desde el repo, no escritos a mano         │
│                                                          │
│  ── Architecture ──────────────────────────────────────  │
│  [ diagrama SVG: Angular → nginx → API → PostgreSQL ]    │
│                                                          │
│  ── Decisions ─────────────────────────────────────────  │
│  ▸ Bilingual content            problema / decisión /    │
│  ▸ Prerendering instead of SPA    alternativa / costo    │
│  ▸ One storage seam                       → enlace ADR   │
│  ▸ …                                                     │
│                                                          │
│  ── How a request resolves a language ─────────────────  │
│  [ diagrama de flujo ]                                   │
│                                                          │
│  ── Data model ────────────────────────────────────────  │
│  [ diagrama: base ← traducciones ]                       │
│                                                          │
│  ── Testing ───────────────────────────────────────────  │
│  ── Operations ────────────────────────────────────────  │
│                                                          │
│  [ Ver el repositorio ]                                  │
└──────────────────────────────────────────────────────────┘
```

**Diagramas:** SVG en línea, escritos a mano, con colores por token de tema. Sin librería de
diagramas: se prerenderizan, funcionan sin JavaScript, y no añaden una dependencia para dibujar
cuatro cajas.

**Ruta propia:** además del resumen en el home, `/en/engineering` y `/es/ingenieria` son páginas
completas prerenderizadas. Es contenido con entidad suficiente para indexarse por su cuenta y es el
enlace que se comparte en una entrevista.

### 4.7 Education, Contact, Footer

- **Education**: título, institución, año. Una línea.
- **Contact**: email, LinkedIn, GitHub y descarga del CV en el idioma activo. **Sin teléfono**
  (ADR-0003). Sin formulario en el MVP.
- **Footer**: enlace al repositorio del propio sitio. Es el argumento más fuerte de la página.

## 5. Estados

| Estado | Cuándo | Qué se ve |
|---|---|---|
| Prerenderizado | Siempre en la primera pintura | Contenido completo, sin JavaScript |
| Revalidando | La app consulta la API tras hidratar | Nada. El usuario no debe notarlo |
| Actualizado | La API respondió con contenido más nuevo | El contenido se reemplaza sin salto de layout |
| En caché | La API falló, expiró o está dormida | El contenido prerenderizado se queda, con un aviso discreto |
| Error duro | Una ruta de detalle no existe | 404 localizada |

**Nunca hay un spinner de página completa ni una pantalla de error.** El contenido ya está en el
HTML; la API solo puede mejorarlo, no impedir que se vea. Ese es todo el punto de
[ADR-0002](adr/0002-frontend-rendering.md).

## 6. SEO

Por locale:

- `<title>`: `Sebastián Vélez Ramírez — Senior .NET Developer` / `— Desarrollador .NET Senior`.
- `<meta name="description">`: primera frase del resumen, recortada.
- `hreflang` recíproco entre `/en` y `/es`, más `x-default` apuntando a `/en`.
- `canonical` autorreferencial por locale.
- Open Graph y Twitter Card con imagen propia, para que el enlace se vea bien en LinkedIn y WhatsApp.
- `sitemap.xml` con ambos locales y sus alternates.
- JSON-LD `Person` con `jobTitle`, `knowsAbout`, `alumniOf` y `sameAs` hacia LinkedIn y GitHub.

En las páginas de detalle, el `<title>` incluye el nombre del proyecto y el cliente.

## 7. Claves de traducción de UI

El texto de chrome (botones, encabezados de sección, etiquetas de estado) vive en Transloco, separado
del contenido profesional que viene de la API.

```text
nav.language.switchTo
hero.availability.openToWork
hero.cta.contact
hero.cta.downloadCv
section.about.title
section.experience.title
experience.parallelRole
experience.duration
section.projects.title
projects.viewDetail
projects.sources
state.cachedContent
error.notFound.title
```

Convención: `área.elemento.detalle`, en minúscula camel, siempre en inglés como clave. Nada de usar
la frase en inglés como clave — cuando el texto cambie, la clave deja de tener sentido y nadie la
renombra.

Archivos: `public/i18n/en.json` y `public/i18n/es.json`. El validador de contenido se extiende en la
Fase 5 para verificar que ambos tengan exactamente las mismas claves.

## 8. Accesibilidad y responsive

- Landmarks reales (`header`, `nav`, `main`, `footer`) y un solo `h1` por página.
- Skip link al contenido principal.
- Foco visible; el selector de idioma y las tarjetas son navegables por teclado.
- Contraste AA como mínimo, verificado en la Fase 10.
- `prefers-reduced-motion` respetado en cualquier animación del timeline.
- Breakpoints: móvil primero; el timeline pasa de dos columnas a una debajo de 768 px.
- El CV se descarga, no se abre en un visor embebido.

## 9. Contrato de API que esto implica

Insumo para la Fase 3. Todos los endpoints resuelven locale por `?lang=` y, si no viene, por
`Accept-Language`, con caída final a `en`, y declaran en la respuesta qué locale resolvieron.

```http
GET /api/profile?lang=es
GET /api/experience?lang=es
GET /api/skills?lang=es
GET /api/projects?lang=es
GET /api/projects/{slug}?lang=es
GET /api/education?lang=es
GET /api/social-links
GET /health/live
GET /health/ready
```

`GET /api/content?lang=es` devuelve todo el paquete de una vez: es lo que consume el build para
generar el snapshot y lo que pide el cliente al revalidar. Una sola petición en vez de siete para
una carga de contenido que siempre se pide completa.

## 10. Fuera del MVP

Formulario de contacto, blog, panel de administración, analytics, modo oscuro conmutable por el
usuario, y cualquier animación que no aporte legibilidad.
