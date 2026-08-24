# Plan de desarrollo — Portafolio profesional .NET + Angular + Docker + Kubernetes

## 1. Objetivo del proyecto

Construir un portafolio profesional, moderno y demostrable técnicamente, cuyo objetivo no sea solamente presentar la experiencia laboral, sino también servir como **proyecto de referencia para entrevistas de Software Engineer / Senior Backend / Technical Lead**.

El proyecto tendrá:

- Frontend en Angular.
- Backend principal en .NET / ASP.NET Core.
- Arquitectura basada en microservicio(s), manteniendo el alcance pequeño y justificable.
- API REST documentada con OpenAPI / Swagger.
- Persistencia mediante PostgreSQL.
- Contenedores Docker.
- Orquestación Kubernetes.
- Desarrollo reproducible localmente.
- CI/CD.
- Despliegue en Azure como primera opción.
- Observabilidad básica.
- Seguridad básica.
- Contenido generado a partir del CV existente en el equipo local.

El resultado debe parecer un proyecto profesional real, no una demo académica.

---

## 2. Restricción y criterio principal

No se debe sobrearquitecturar el proyecto solamente para poder decir que utiliza microservicios.

El sistema debe demostrar que se conocen:

- separación de responsabilidades;
- APIs REST;
- persistencia;
- Docker;
- Kubernetes;
- CI/CD;
- escalabilidad básica;
- observabilidad;
- seguridad;
- configuración por ambiente;
- manejo de errores;
- despliegues;
- arquitectura distribuida.

Por eso se debe comenzar con **un bounded context principal / microservicio de Portfolio**, y solo separar otro servicio cuando exista una razón arquitectónica clara.

---

## 3. Arquitectura objetivo

### 3.1 Arquitectura inicial

```text
                    Internet
                       |
                       v
                +--------------+
                |   Angular    |
                |  Frontend    |
                +--------------+
                       |
                    HTTPS/REST
                       |
                       v
              +--------------------+
              | Portfolio API      |
              | ASP.NET Core       |
              | .NET               |
              +--------------------+
                       |
                       v
              +--------------------+
              | PostgreSQL         |
              +--------------------+

        Kubernetes
        --------------------------------
        Namespace: portfolio

        frontend deployment
        api deployment
        postgres deployment/stateful workload
        services
        ingress
        configmaps
        secrets
        probes
```

### 3.2 Evolución opcional

Si posteriormente existe suficiente funcionalidad, puede aparecer un segundo microservicio:

```text
Angular
   |
   +---- Portfolio API
   |
   +---- Contact API
```

No crear este segundo servicio desde el principio sin necesidad real.

---

# 4. Stack tecnológico propuesto

## Frontend

- Angular.
- TypeScript.
- Angular Router.
- Reactive Forms si se crea formulario de contacto.
- HTTP Client.
- Responsive design.
- CSS/SCSS.
- Componentes reutilizables.
- Lazy loading donde tenga sentido.

## Backend

- ASP.NET Core Web API.
- .NET LTS disponible en el momento de implementación.
- C#.
- Entity Framework Core.
- PostgreSQL.
- FluentValidation o validación equivalente.
- Swagger / OpenAPI.
- Serilog o logging estructurado equivalente.
- Health Checks.
- API versioning solo si realmente aporta valor.

## Infraestructura

- Docker.
- Docker Compose para desarrollo local.
- Kubernetes.
- kubectl.
- Helm opcional después de tener manifests funcionando directamente.
- Ingress.
- ConfigMaps.
- Secrets.
- Readiness probes.
- Liveness probes.
- Resource requests/limits.

## CI/CD

Primera opción:

- GitHub.
- GitHub Actions.

Alternativa:

- Azure DevOps Pipelines.

El pipeline debe construir, probar, analizar y publicar las imágenes Docker.

## Cloud

Primera opción recomendada: Azure.

Motivo:

- AKS permite demostrar Kubernetes real.
- Azure tiene una cuenta gratuita con crédito inicial para nuevos usuarios y varios servicios gratuitos, pero el cómputo de los nodos de AKS no es gratuito permanentemente.
- El objetivo debe ser desplegar únicamente cuando sea necesario y controlar los costos.

AWS queda como alternativa para una segunda implementación utilizando EKS, entendiendo que EKS tiene costos asociados al cluster y además se pagan los recursos de los nodos. [Verificar costos actuales antes de crear infraestructura.]

---

# 5. Estructura del repositorio

La estructura inicial recomendada:

```text
portfolio/
│
├── README.md
├── LICENSE
├── .gitignore
├── .editorconfig
├── docker-compose.yml
├── docker-compose.dev.yml
│
├── docs/
│   ├── architecture.md
│   ├── api.md
│   ├── deployment.md
│   ├── adr/
│   │   ├── 001-microservice-boundary.md
│   │   ├── 002-postgresql.md
│   │   ├── 003-kubernetes.md
│   │   └── 004-cloud-provider.md
│   └── diagrams/
│
├── src/
│   ├── frontend/
│   │   └── portfolio-web/
│   │
│   └── services/
│       └── portfolio-api/
│           ├── Portfolio.Api/
│           ├── Portfolio.Application/
│           ├── Portfolio.Domain/
│           ├── Portfolio.Infrastructure/
│           └── Portfolio.Tests/
│
├── infra/
│   ├── docker/
│   ├── kubernetes/
│   │   ├── base/
│   │   └── overlays/
│   │       ├── local/
│   │       └── azure/
│   └── scripts/
│
└── .github/
    └── workflows/
        ├── ci.yml
        └── cd.yml
```

---

# 6. Arquitectura del backend

No convertir el backend en una implementación excesivamente ceremoniosa.

Usar una estructura inspirada en **Clean Architecture**, con separación clara entre:

```text
API
  |
Application
  |
Domain
  |
Infrastructure
```

### Domain

Debe contener:

- entidades;
- value objects cuando sean necesarios;
- reglas de negocio;
- interfaces que pertenezcan realmente al dominio.

Debe evitar depender de:

- EF Core;
- PostgreSQL;
- ASP.NET Core;
- detalles de infraestructura.

### Application

Debe contener:

- casos de uso;
- DTOs;
- commands/queries si se decide usar CQRS ligero;
- validaciones;
- contratos de aplicación.

No implementar CQRS complejo desde el inicio.

### Infrastructure

Debe contener:

- DbContext;
- configuraciones EF Core;
- repositorios solamente cuando sean útiles;
- migraciones;
- acceso a servicios externos.

Evitar crear un `IGenericRepository<T>` solamente para ocultar Entity Framework Core.

### API

Debe encargarse de:

- HTTP;
- routing;
- autenticación/autorización si aplica;
- serialización;
- filtros/middleware;
- status codes;
- OpenAPI.

Los controllers no deben contener lógica de negocio importante.

---

# 7. Modelo de datos inicial

El modelo debe ser derivado del CV y del contenido real del portafolio.

Antes de crear entidades definitivas, el agente debe leer y analizar el CV.

Posibles entidades:

```text
Profile
Experience
Company
Technology
Project
Education
Certification
Achievement
Skill
SocialLink
```

No crear automáticamente todas estas tablas.

El agente debe seleccionar el modelo mínimo necesario después de analizar el CV.

Ejemplo:

```text
Profile
   |
   +---- Experience
   |       |
   |       +---- Technology
   |
   +---- Education
   |
   +---- Certification
   |
   +---- Project
   |
   +---- SocialLink
```

Debe evitarse duplicar texto del CV en múltiples tablas.

---

# 8. Proceso para utilizar el CV existente

## Regla fundamental

El CV localizado en el equipo del usuario es la **fuente primaria de verdad para el contenido profesional**.

El agente NO debe inventar:

- empresas;
- cargos;
- fechas;
- tecnologías;
- proyectos;
- certificaciones;
- responsabilidades;
- métricas;
- logros.

Puede mejorar la redacción únicamente cuando el dato esté respaldado por el CV.

## Flujo

```text
CV local
   |
   v
Extracción de texto
   |
   v
Normalización
   |
   v
Identificación de secciones
   |
   v
Modelo estructurado
   |
   v
Revisión de inconsistencias
   |
   v
Contenido del portafolio
   |
   v
Seed inicial de base de datos
```

## Archivos intermedios recomendados

```text
content/
├── cv-source.md
├── profile.json
├── experience.json
├── education.json
├── skills.json
├── projects.json
└── content-review.md
```

Estos archivos no deben contener información inventada.

El agente debe conservar trazabilidad entre el contenido y el CV de origen.

---

# 9. Fase 0 — Preparación del entorno del agente

## Objetivo

Garantizar que el agente pueda trabajar de forma autónoma sobre el repositorio.

### Checklist

- [ ] Crear repositorio Git.
- [ ] Crear estructura base.
- [ ] Configurar `.gitignore`.
- [ ] Configurar `.editorconfig`.
- [ ] Definir README inicial.
- [ ] Verificar Git instalado.
- [ ] Verificar Docker instalado.
- [ ] Verificar Node.js/npm instalados.
- [ ] Verificar .NET SDK instalado.
- [ ] Verificar Angular CLI.
- [ ] Verificar kubectl.
- [ ] Verificar un cluster local: Docker Desktop Kubernetes, kind o Minikube.
- [ ] Documentar versiones utilizadas.

---

# 10. Fase 1 — Lectura y extracción del CV

## Objetivo

Construir el contenido estructurado que alimentará el sistema.

### Tareas del agente

- [ ] Localizar el CV indicado por el usuario.
- [ ] Leer el archivo sin modificar el original.
- [ ] Extraer texto.
- [ ] Identificar perfil profesional.
- [ ] Identificar experiencia laboral.
- [ ] Identificar responsabilidades.
- [ ] Identificar tecnologías por empresa.
- [ ] Identificar proyectos.
- [ ] Identificar educación.
- [ ] Identificar certificaciones.
- [ ] Identificar habilidades.
- [ ] Identificar links profesionales.
- [ ] Detectar posibles inconsistencias de fechas.
- [ ] Generar archivos estructurados.
- [ ] Generar un documento de revisión de contenido.

### Criterio de aceptación

El agente debe poder explicar de dónde proviene cada bloque de contenido antes de incorporarlo al portafolio.

---

# 11. Fase 2 — Diseño funcional del portafolio

Crear primero el contenido y UX, después implementar.

## Secciones recomendadas

```text
Home
 |
 +-- Hero
 +-- About
 +-- Experience
 +-- Skills
 +-- Projects
 +-- Education
 +-- Certifications
 +-- Contact
```

### Home

Debe responder rápidamente:

- quién soy;
- qué hago;
- principales tecnologías;
- años/áreas de experiencia si el CV lo permite;
- enlaces importantes.

### Experience

Mostrar:

- empresa;
- cargo;
- fechas;
- responsabilidades;
- tecnologías;
- logros respaldados por el CV.

### Skills

Separar por categorías, por ejemplo:

```text
Backend
Cloud
Databases
Frontend
DevOps
Architecture
Tools
```

### Projects

Preferir proyectos reales o proyectos demostrables.

Cada proyecto puede incluir:

- objetivo;
- contexto;
- tecnologías;
- arquitectura;
- decisiones técnicas;
- resultados;
- repositorio/demo cuando exista.

### Contact

Debe ser sencillo y no requerir una arquitectura compleja.

---

# 12. Fase 3 — Creación del backend .NET

## Objetivo

Crear una API mantenible y testeable.

### Endpoints iniciales

Los endpoints definitivos deben ajustarse al modelo generado desde el CV.

Posible API:

```http
GET /api/profile
GET /api/experience
GET /api/experience/{id}
GET /api/skills
GET /api/projects
GET /api/projects/{id}
GET /api/education
GET /api/certifications
GET /api/social-links
```

Si se incluye contacto:

```http
POST /api/contact
```

Debe existir documentación OpenAPI.

### Cross-cutting concerns

Implementar:

- manejo global de excepciones;
- correlation/request ID;
- logging estructurado;
- validación;
- health checks;
- configuración por ambiente;
- response codes consistentes;
- CORS correctamente configurado;
- rate limiting si se expone un endpoint público sensible.

---

# 13. Fase 4 — Persistencia PostgreSQL

### Tareas

- [ ] Crear DbContext.
- [ ] Definir entidades.
- [ ] Crear configuraciones Fluent API.
- [ ] Crear migraciones.
- [ ] Crear seed inicial.
- [ ] Crear índices necesarios.
- [ ] Definir estrategia de actualización de schema.
- [ ] Crear pruebas de integración básicas.

### Regla

El contenido del CV debe poder cargarse desde un seed reproducible o desde un proceso de importación controlado.

No depender de inserts manuales.

---

# 14. Fase 5 — Frontend Angular

## Objetivo

Crear una interfaz profesional, rápida y responsive.

### Componentes iniciales

```text
layout
├── navbar
├── hero
├── about
├── experience
├── skills
├── projects
├── education
├── certifications
├── contact
└── footer
```

### Reglas

- No hardcodear toda la información profesional en los templates.
- Consumir la API.
- Modelar interfaces TypeScript.
- Manejar loading/error states.
- Separar servicios HTTP de componentes visuales.
- Usar environment configuration correctamente.
- Mantener accesibilidad básica.
- Responsive desde mobile hasta desktop.

---

# 15. Fase 6 — Docker

## Backend

Crear Dockerfile multi-stage.

Objetivo conceptual:

```text
SDK image
   |
   | build
   v
runtime image
```

No enviar el SDK completo al runtime final si no es necesario.

## Frontend

Opción recomendada:

```text
Angular build
     |
     v
Static files
     |
     v
Nginx
```

## Docker Compose

Crear un entorno local:

```text
Angular/Nginx
      |
Portfolio API
      |
PostgreSQL
```

Debe ser posible iniciar todo con un comando.

Criterio de aceptación:

```bash
docker compose up --build
```

debe levantar el sistema completo.

---

# 16. Fase 7 — Kubernetes local

## Objetivo

Demostrar que el sistema puede ejecutarse como workload Kubernetes.

### Recursos mínimos

```text
Namespace
Deployment
Service
ConfigMap
Secret
Ingress
HorizontalPodAutoscaler (opcional inicialmente)
```

Para PostgreSQL, priorizar una solución simple de desarrollo local. No construir una plataforma compleja de alta disponibilidad para una base de datos cuyo objetivo es alimentar un portafolio personal.

### Deployments

```text
portfolio-web
portfolio-api
```

### Services

```text
portfolio-web
portfolio-api
```

### Configuración

Variables como:

```text
ConnectionStrings__Default
ApiBaseUrl
AllowedOrigins
```

no deben quedar hardcodeadas en el código.

### Health checks

El backend debe tener:

```text
/health/live
/health/ready
```

Kubernetes debe utilizarlos como:

```text
livenessProbe
readinessProbe
```

---

# 17. Fase 8 — Kubernetes bien diseñado

Después de que la aplicación funcione, agregar:

- resource requests;
- resource limits;
- rolling updates;
- pod disruption considerations;
- configuración de replicas;
- labels consistentes;
- namespaces;
- secrets separados de configmaps;
- estrategia de rollback;
- NetworkPolicy si el entorno lo permite y aporta valor.

No implementar características de Kubernetes solamente para aumentar la cantidad de archivos.

---

# 18. Fase 9 — CI

Crear pipeline que ejecute en cada Pull Request.

```text
Checkout
   |
Restore dependencies
   |
Build
   |
Unit tests
   |
Lint/analyzers
   |
Frontend build/test
   |
Docker build
```

El pipeline debe fallar si:

- no compila;
- fallan tests;
- hay problemas críticos de calidad configurados como errores.

---

# 19. Fase 10 — Container Registry

Primera opción en Azure:

- Azure Container Registry.

Flujo:

```text
Git push
   |
GitHub Actions
   |
Docker build
   |
Security/basic image scan
   |
Push image
   |
ACR
```

Usar tags reproducibles, por ejemplo:

```text
portfolio-api:<git-sha>
portfolio-web:<git-sha>
```

No utilizar únicamente `latest` para despliegues.

---

# 20. Fase 11 — Azure AKS

## Objetivo

Desplegar el mismo sistema que funciona localmente.

### Arquitectura

```text
Internet
   |
   v
Ingress / Load Balancer
   |
   +---------+
   |         |
   v         v
Angular     API
              |
              v
          PostgreSQL
```

## Estrategia de costos

El proyecto debe tratar Azure como un entorno controlado.

Antes de crear recursos:

- [ ] Confirmar la modalidad de la cuenta Azure.
- [ ] Verificar crédito restante.
- [ ] Verificar precios actuales de los recursos.
- [ ] Definir región.
- [ ] Definir presupuesto máximo.
- [ ] Crear alertas de costo.
- [ ] Documentar cómo detener/eliminar recursos.

AKS ofrece una modalidad de administración del cluster sin cargo adicional, pero los recursos de cómputo de los nodos sí generan costos. La cuenta gratuita de Azure incluye actualmente crédito inicial y cantidades gratuitas de varios servicios para nuevos usuarios, por lo que el proyecto debe diseñarse para aprovechar esas cantidades sin asumir que un AKS público será permanentemente gratuito. 

Referencia oficial: Azure Free Account y Azure Kubernetes Service.

---

# 21. Alternativa AWS

Mantener el proyecto portable para poder añadir posteriormente:

```text
EKS
ECR
ALB
CloudWatch
```

Pero no convertir el primer despliegue en AWS + Azure simultáneamente.

La primera implementación cloud debe ser una sola.

EKS debe considerarse de pago: Amazon cobra por el cluster y adicionalmente por recursos como nodos EC2 y otros componentes utilizados. Verificar precios vigentes antes de desplegar.

---

# 22. Fase 12 — Dominio y HTTPS

Después de que el deployment funcione:

- [ ] Registrar o utilizar un dominio si ya existe.
- [ ] Configurar DNS.
- [ ] Configurar HTTPS.
- [ ] Automatizar certificados cuando sea razonable.
- [ ] Redireccionar HTTP a HTTPS.

La solución debe funcionar con una URL pública fácil de compartir en el CV.

---

# 23. Fase 13 — Observabilidad

Implementar inicialmente:

### Logs

Logging estructurado:

```text
timestamp
level
message
requestId
endpoint
statusCode
duration
```

### Métricas

Al menos:

- request count;
- error count;
- latency;
- status codes.

### Health

```text
/health/live
/health/ready
```

### Opcional

Agregar OpenTelemetry cuando el resto del sistema esté estable.

---

# 24. Fase 14 — Seguridad

Debe existir una sección de seguridad en la documentación explicando:

- secretos fuera del repositorio;
- CORS;
- HTTPS;
- validación de entradas;
- rate limiting donde tenga sentido;
- headers de seguridad;
- mínimo privilegio;
- imágenes Docker actualizadas;
- usuario no-root cuando sea viable;
- dependency scanning;
- protección contra exposición accidental de información del CV.

Nunca almacenar:

- contraseñas;
- tokens;
- claves cloud;
- connection strings reales;
- secretos API

en Git.

---

# 25. Fase 15 — Tests

## Backend

### Unit tests

Probar:

- reglas de negocio;
- servicios de aplicación;
- validaciones;
- mapeos importantes.

### Integration tests

Probar:

- endpoints;
- persistencia;
- escenarios de error.

## Frontend

Probar al menos:

- servicios principales;
- componentes críticos;
- estados de error/loading.

## Smoke tests

Después del deployment:

```text
GET /health/live
GET /health/ready
GET /api/profile
GET /api/experience
GET /api/projects
```

---

# 26. Fase 16 — Documentación para entrevistas

El proyecto debe explicar no solo cómo funciona, sino **por qué**.

Crear ADRs para decisiones importantes.

## Preguntas que la documentación debe responder

### ¿Por qué microservicio?

Explicar el objetivo demostrativo y la frontera funcional del servicio.

### ¿Por qué no hacer muchos microservicios?

Explicar costo operativo, complejidad y tamaño real del dominio.

### ¿Por qué Clean Architecture?

Explicar separación de dominio, aplicación, infraestructura y API.

### ¿Por qué PostgreSQL?

Explicar modelo relacional, sencillez operativa y adecuación al dominio.

### ¿Por qué Docker?

Explicar reproducibilidad y empaquetado.

### ¿Por qué Kubernetes?

Explicar orchestration, deployments, health checks, scaling y configuración.

### ¿Por qué AKS?

Explicar integración con Azure, experiencia Kubernetes y portabilidad.

### ¿Qué ocurre si la API deja de responder?

Explicar readiness/liveness, replicas y rollout/rollback.

### ¿Qué pasa si hay dos replicas?

Explicar stateless API y sesión/configuración externa.

### ¿Qué pasa si PostgreSQL falla?

Explicar límites del diseño y qué sería necesario para alta disponibilidad real.

### ¿Cómo se despliega una nueva versión?

Explicar CI/CD, image tags y rolling updates.

---

# 27. Fase 17 — Performance y escalabilidad

No optimizar prematuramente.

Primero establecer métricas.

Después medir:

- tiempo promedio;
- p50;
- p95;
- p99;
- throughput;
- tasa de errores.

Realizar una pequeña prueba de carga.

Ejemplo conceptual:

```text
10 req/s
50 req/s
100 req/s
```

El objetivo no es alcanzar una cifra espectacular, sino demostrar que se sabe medir, identificar un cuello de botella y explicar la capacidad del sistema.

---

# 28. Fase 18 — Ingeniería de IA para el agente

El agente de escritorio debe usar IA como asistente de ingeniería, no como autoridad absoluta.

## Reglas del agente

1. Leer el código antes de modificarlo.
2. Entender la arquitectura antes de crear nuevas capas.
3. No inventar datos profesionales.
4. No borrar código existente sin justificarlo.
5. Hacer cambios pequeños y verificables.
6. Ejecutar tests después de modificaciones relevantes.
7. Revisar errores de compilación antes de continuar.
8. No introducir dependencias innecesarias.
9. Documentar decisiones arquitectónicas importantes.
10. No exponer secretos.
11. No ejecutar comandos destructivos sin justificación clara.
12. Mantener el proyecto ejecutable en todo momento.

---

# 29. Protocolo de trabajo del agente

Para cada tarea:

```text
1. Inspect
2. Understand
3. Plan
4. Implement
5. Test
6. Review
7. Document
8. Report
```

## Inspect

Revisar archivos relacionados.

## Understand

Explicar internamente qué parte de la arquitectura será afectada.

## Plan

Determinar cambios mínimos.

## Implement

Aplicar cambios pequeños.

## Test

Ejecutar pruebas y build.

## Review

Revisar:

- seguridad;
- performance;
- duplicación;
- errores;
- naming;
- arquitectura.

## Document

Actualizar documentación si cambió comportamiento o arquitectura.

## Report

Informar:

- qué cambió;
- archivos afectados;
- pruebas ejecutadas;
- problemas pendientes.

---

# 30. Reglas de commits

Usar Conventional Commits.

Ejemplos:

```text
feat(api): add experience endpoint
feat(web): add experience section
fix(api): handle invalid project id
refactor(api): separate portfolio application service
test(api): add experience integration tests
docs(architecture): explain microservice boundary
ci: add docker build workflow
```

No usar commits gigantes como:

```text
update everything
final version
changes
fix stuff
```

---

# 31. Roadmap de implementación

## Milestone 1 — Skeleton

- [ ] Repository.
- [ ] Angular app.
- [ ] .NET API.
- [ ] PostgreSQL.
- [ ] Docker Compose.
- [ ] Basic README.

## Milestone 2 — CV

- [ ] CV parseado.
- [ ] Modelo de contenido.
- [ ] Seed.
- [ ] API completa.

## Milestone 3 — UI

- [ ] Home.
- [ ] Experience.
- [ ] Skills.
- [ ] Projects.
- [ ] Education.
- [ ] Certifications.
- [ ] Contact.

## Milestone 4 — Production readiness

- [ ] Tests.
- [ ] Logging.
- [ ] Health checks.
- [ ] Docker hardening.
- [ ] Configuration.
- [ ] Security baseline.

## Milestone 5 — Kubernetes

- [ ] Namespace.
- [ ] Deployments.
- [ ] Services.
- [ ] ConfigMaps.
- [ ] Secrets.
- [ ] Ingress.
- [ ] Probes.
- [ ] Resource limits.

## Milestone 6 — CI/CD

- [ ] CI workflow.
- [ ] Docker build.
- [ ] Container registry.
- [ ] CD workflow.

## Milestone 7 — Azure

- [ ] Azure account verification.
- [ ] Resource group.
- [ ] Registry.
- [ ] AKS.
- [ ] Deploy.
- [ ] HTTPS.
- [ ] Monitoring.
- [ ] Cost controls.

## Milestone 8 — Portfolio polish

- [ ] SEO.
- [ ] Social preview metadata.
- [ ] Favicon.
- [ ] Lighthouse review.
- [ ] Accessibility review.
- [ ] Mobile review.
- [ ] Architecture diagram.
- [ ] Public README.
- [ ] Demo URL.

---

# 32. Definition of Done

El proyecto se considera terminado cuando:

- [ ] El portafolio funciona localmente.
- [ ] La información proviene del CV y fue revisada.
- [ ] Angular consume la API.
- [ ] API .NET funciona con PostgreSQL.
- [ ] API tiene Swagger/OpenAPI.
- [ ] Existen tests automatizados.
- [ ] Todo puede levantarse con Docker Compose.
- [ ] Todo puede desplegarse en Kubernetes local.
- [ ] Existen readiness/liveness probes.
- [ ] Existen ConfigMaps/Secrets adecuados.
- [ ] Existe CI.
- [ ] Las imágenes se publican a un registry.
- [ ] Existe una estrategia CD reproducible.
- [ ] Existe deployment cloud.
- [ ] HTTPS funciona.
- [ ] Los secretos no están en Git.
- [ ] Hay documentación arquitectónica.
- [ ] Existe un diagrama de arquitectura.
- [ ] Hay URL pública para mostrar en el CV.
- [ ] El README explica cómo ejecutar el sistema.
- [ ] El README explica decisiones técnicas.
- [ ] Se conoce el costo aproximado del entorno cloud.
- [ ] Existe un procedimiento para apagar/eliminar recursos cloud.

---

# 33. Orden exacto recomendado para el agente

El agente debe seguir este orden y no saltarse directamente a Kubernetes:

```text
1. Inspect environment
        ↓
2. Read CV
        ↓
3. Define content model
        ↓
4. Create repository
        ↓
5. Create .NET API
        ↓
6. Create PostgreSQL model
        ↓
7. Implement API
        ↓
8. Create Angular application
        ↓
9. Connect Angular → API
        ↓
10. Add tests
        ↓
11. Dockerize
        ↓
12. Docker Compose
        ↓
13. Kubernetes local
        ↓
14. CI
        ↓
15. Container Registry
        ↓
16. Azure AKS
        ↓
17. HTTPS/domain
        ↓
18. Observability
        ↓
19. Security review
        ↓
20. Performance test
        ↓
21. Documentation
        ↓
22. Final polish
```

---

# 34. Primer prompt para el agente de escritorio

Usar el siguiente prompt como punto de partida:

```text
Actúa como un Senior Software Engineer / Solution Architect responsable de construir mi portafolio profesional.

Objetivo:
Construir una aplicación real de portafolio profesional usando Angular como frontend y ASP.NET Core/.NET como backend, ejecutable con Docker y desplegable en Kubernetes.

Principios:
- No inventes información profesional.
- El CV que existe en mi equipo es la fuente principal para el contenido.
- Antes de escribir código debes inspeccionar el repositorio y los archivos relevantes.
- Mantén una arquitectura simple y justificable.
- Utiliza Clean Architecture de forma pragmática.
- No crees capas o patrones sin necesidad.
- No uses Generic Repository solamente para ocultar EF Core.
- Comienza con un microservicio de Portfolio y solo crea otros microservicios cuando exista una razón clara.
- La aplicación debe poder ejecutarse localmente antes de intentar desplegarla en la nube.
- Cada cambio importante debe tener tests.
- No almacenes secretos en Git.
- Usa Docker multi-stage.
- Utiliza Kubernetes con Deployments, Services, ConfigMaps, Secrets, Ingress y health probes.
- Mantén el sistema stateless donde sea posible.
- Documenta las decisiones arquitectónicas importantes.

Stack objetivo:
- Angular
- TypeScript
- ASP.NET Core / .NET LTS
- C#
- Entity Framework Core
- PostgreSQL
- Docker
- Kubernetes
- GitHub Actions
- Azure como primer cloud

Workflow obligatorio para cada tarea:
1. Inspect
2. Understand
3. Plan
4. Implement
5. Test
6. Review
7. Document
8. Report

Antes de crear entidades definitivas, localiza y analiza el CV.
Genera primero un modelo estructurado del contenido y una lista de posibles inconsistencias para revisión.
No modifiques el CV original.

No avances a la siguiente fase si la fase actual no compila o no tiene una forma reproducible de ejecutarse.
```

---

# 35. Primer objetivo técnico del agente

La primera iteración no debe ser el portafolio completo.

Debe terminar con:

```text
Angular
   |
   | HTTP
   v
.NET API
   |
   v
PostgreSQL
```

Todo ejecutándose con:

```text
docker compose up
```

Después se agrega Kubernetes.

Esto reduce el riesgo de intentar depurar simultáneamente:

```text
Angular + .NET + EF + PostgreSQL + Docker + Kubernetes + Azure
```

antes de tener una aplicación funcional.

---

# 36. Mejoras posteriores opcionales

Estas funcionalidades NO forman parte del MVP:

- autenticación para administración del contenido;
- panel administrativo;
- CMS;
- multiidioma avanzado;
- blog;
- búsqueda;
- chatbot IA;
- recomendador de proyectos;
- generación automática de CV;
- analytics avanzados;
- arquitectura multi-region;
- service mesh;
- Kafka;
- Redis distribuido;
- event sourcing;
- CQRS completo.

Solo deben agregarse si existe una razón real y aportan valor al portafolio técnico.

---

# 37. Resultado final esperado

La página debe poder mostrarse como:

```text
https://<dominio>
```

y simultáneamente el repositorio debe demostrar:

```text
Frontend Engineering
        +
Backend Engineering
        +
Software Architecture
        +
Docker
        +
Kubernetes
        +
Cloud
        +
CI/CD
        +
Observability
        +
Security
```

La mayor ventaja del proyecto será que cada decisión técnica pueda explicarse durante una entrevista de Software Engineer / Senior Engineer / Technical Lead.

---

# 38. Fuentes cloud a verificar antes del despliegue

Las condiciones y precios cloud cambian. Antes de crear recursos productivos, el agente debe consultar las páginas oficiales de precios de Azure/AWS y registrar en `docs/deployment.md` la fecha de verificación.

Fuentes principales utilizadas para este plan:

- Microsoft Azure — Free Account / Azure Free Account.
- Microsoft Azure — Azure Kubernetes Service (AKS).
- Amazon Web Services — Amazon EKS documentation/pricing.

Estado de referencia de este plan: **23 de agosto de 2026**.
