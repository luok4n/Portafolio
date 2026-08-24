# Client and project research

Public-source background for the clients and projects behind each role, gathered per
[ADR-0003](../docs/adr/0003-content-privacy.md): only publicly available information, every claim
traceable to a cited URL.

- **Researched:** 2026-08-24
- **Author-supplied project list:** confirmed by Sebastián Vélez on 2026-08-24
- **Status legend:** ✅ verified against a public source · ⚠️ author-supplied, no public source found

Nothing here describes internal architecture, credentials, contract terms, incidents or named
individuals from any client.

---

## Adagetech S.A.S. — Dec 2022 – Jul 2026

Role: Senior .NET Developer. Both projects below were delivered from Adagetech.

### Argos ONE ✅

Order-management platform of **Cementos Argos**, the Colombian multinational building-materials
company (Grupo Argos). Argos ONE lets construction and hardware-store customers place, track and
modify cement and concrete orders in real time, receive delivery-status notifications from plant
loading to job site, consult product technical sheets and certifications, and review invoices and
payment history. It runs as a web platform and as mobile apps, with a separate app published for
the **United States** market alongside the Colombian one — consistent with the author's multi-country
support and development work.

Publicly reported adoption: an NPS above 80% and roughly 1,850 customers interacting monthly.

- Sector: construction materials / B2B commerce
- Author's contribution: support and module development (Angular, .NET, SQL Server, Azure) for
  operations across the United States, Colombia and additional countries.
- Sources:
  - https://argos.co/en/argos-one/ (checked 2026-08-24)
  - https://colombia.argos.co/ferreteros/que-es-argos-one-y-como-beneficia-a-tu-negocio-ferretero/ (checked 2026-08-24)
  - https://www.argosone.com/ (checked 2026-08-24)
  - https://play.google.com/store/apps/details?id=co.argosone.usa (ArgosONE USA, checked 2026-08-24)

### Linkvest ✅

**Linkvest Capital** is a Miami-based alternative co-investment platform and vertically integrated
real estate firm, founded in 2013, operating in Florida and the southeastern United States. Its
business lines are short-term commercial real estate loans, equity in mixed-use developments, and
acquisition and management of commercial, industrial and triple-net assets. Its affiliate **LV
Lending** is a licensed private lender in Florida.

- Sector: real estate investment / private lending (United States)
- Author's contribution: full-stack development (Angular, .NET, SQL Server), specifically **monthly
  and quarterly reporting on each client's investments**.
- Sources:
  - https://linkvestcapital.com/ (checked 2026-08-24)
  - https://linkvestcapital.com/financing/ (checked 2026-08-24)

---

## AES Chivor & Cía. S.C.A. E.S.P. — Mar 2022 – Dec 2022 (freelance)

Role: Software Engineer (freelance), overlapping intentionally with the LendingFront role.

### Company ✅

AES Chivor operates the **Chivor hydroelectric plant**, part of **AES Colombia** (subsidiary of AES
Andes S.A., of the U.S. AES Corporation). AES Colombia runs a 100% renewable portfolio in Colombia
with more than 25 years in the market and is the country's fifth largest generator with roughly 6%
market share. Chivor is among the largest plants in Colombia and covers approximately 6% of national
demand.

Colombia's **Mercado de Energía Mayorista (MEM)** — the wholesale energy market — is where
generators, transmitters, distributors, traders and large consumers exchange energy blocks on the
National Interconnected System. It is administered by **XM S.A. E.S.P.**, a subsidiary of ISA, which
also operates the grid through the National Dispatch Center.

- Sources:
  - https://www.aescol.com/en/press-release/aes-colombia-reinicia-operaciones-de-la-central-hidroelectrica-chivor (checked 2026-08-24)
  - https://www.xm.com.co/nuestra-empresa/nosotros/quienes-somos (checked 2026-08-24)
  - https://www.superservicios.gov.co/Empresas-vigiladas/Energ%C3%ADa-y-gas-combustible/Energ%C3%ADa/Unidad-de-Monitoreo-de-Mercados-de-Energ%C3%ADa-y-Gas-Natural/Mercado-de-energ%C3%ADa-mayorista-Informes-hist%C3%B3ricos (checked 2026-08-24)

### MOA platform ✅ (client and market sourced; product name is not)

Internal AES platform for buying and selling energy in the Colombian wholesale market, including
**Excel report generation** for market operations. "MOA" is an internal product name; no public
documentation of the platform or of the acronym was found, so the portfolio describes it by
function only, exactly as the CV does — a wholesale energy trading platform with a .NET and
SQL Server backend on a microservices architecture.

Marked `publiclySourced: true` because everything the description actually asserts — the client, the
Chivor plant and the wholesale market it trades on — is documented by the AES Colombia and XM
sources above. The flag tracks whether a reader can check the claims made, not whether every
internal name appears on the open web.

---

## LendingFront — Jan 2022 – Dec 2022

### Company ✅

New York City startup founded in 2014, Series A, building **white-label small-business lending
software** for banks and financial institutions. The platform covers application intake and
workflow, document management, data aggregation, rules-based decisioning, underwriting, offer
presentation, monitoring and servicing, and integrates into POS systems, mobile and desktop banking
and vertical SaaS platforms. Its founding team came from American Express, Capital One and OnDeck
Capital; clients range from online-only credit providers to institutions above $10bn.

- Sector: fintech / small-business credit
- Author's contribution: Marketplace BackOffice (Python, React, Redux, PostgreSQL, AWS). Worked
  across three teams: **development, innovation and optimisation**.
- Sources:
  - https://www.crunchbase.com/organization/lendingfront (checked 2026-08-24)
  - https://debanked.com/2019/01/trained-at-ondeck-lendingfront-founders-help-banks-lend-to-small-businesses/ (checked 2026-08-24)

---

## MVM Ingeniería de Software — Oct 2019 – Dec 2021

### Company ✅

Custom software development company founded in December 1995, headquartered in **Medellín**, with
offices in Bogotá and Miami. CMMI level 4 and ISO 9001 certified. Serves clients primarily in
energy, public services, finance and telecommunications.

- Source: https://www.bnamericas.com/en/company-profile/mvm-ingenieria-de-software (checked 2026-08-24)

### CHIVOR / XM ✅ (context)

Backend work with .NET Core and SQL Server for energy management processes, in the same Colombian
wholesale-market context described above, administered by XM.

- Source: https://www.xm.com.co/nuestra-empresa/nosotros/quienes-somos (checked 2026-08-24)

### Slang ✅

Colombian **EdTech** platform for professional and technical English, born as an MIT research
project and now a regional leader in Latin America. B2B model with a machine-learning powered
adaptive engine, 200+ professional English courses, presence in Colombia, Mexico, Brazil and the
United States, a US$14M Series A, and recognition by the World Economic Forum in 2022 as one of
the world's 100 most important technology pioneers — the only Colombian startup on that list.

The work was delivered under an **alliance between Slang and Comfama** that let the fund's affiliates
access the English courses with member benefits — so this project and the Comfama account below are
the same engagement seen from two sides, not two unrelated clients.

- Author's contribution: web functionality with ASP.NET and Angular.
- Sources:
  - https://forbes.co/2021/12/20/emprendedores/ensenando-ingles-para-tecnologia-y-negocios-slang-recauda-us14-millones (checked 2026-08-24)
  - https://www.endeavor.org.co/novedades/ecosistemas/slang-a-un-paso-de-ser-parte-de-la-red-endeavor/ (checked 2026-08-24)

### Comfama ✅

Confirmed by the author as **Comfama**, the *Caja de Compensación Familiar de Antioquia*: a private
non-profit founded on 29 November 1954 that performs social-security functions in Antioquia,
providing credit, subsidies, health, education, housing, recreation, culture and tourism services
across the department. Its Medellín base fits MVM's client profile.

Three pieces of work under this account, per the author:

1. **The Slang alliance** described above — access to Slang's English courses for Comfama affiliates
   with member benefits.
2. **Internal management of Comfama's website.**
3. **A grading platform for teachers**, used to record students' marks across their courses.
   Education is one of the services Comfama provides to affiliated workers and their families, which
   is consistent with a first-party teaching platform.

- Sector: social security / family welfare fund, with an education component
- Source: https://www.comfama.com/conoce-comfama/ (checked 2026-08-24)

---

## Woldev S.A.S. — Feb 2018 – Jan 2019

### Company ✅

Software development company based in **Pereira, Colombia**, working on web design, e-commerce,
digital strategy, mobile applications, server administration and video game development.

- Source: https://woldev.co/ (checked 2026-08-24)

### Teach at Home ⚠️

Author-supplied project; no public source found. Described by function only: an education platform
the author contributed to on both frontend and backend.

### Gobernación de Risaralda website ✅

Author-confirmed: the engagement was the **Gobernación de Risaralda** institutional website, an
older PHP codebase. The author recalls no further detail, and no public record of the project itself
was found, so it is described by client and technology only — website work for the departmental
government of Risaralda on a legacy PHP application.

- Sector: public sector / departmental government
- Source: https://www.risaralda.gov.co/ (checked 2026-08-24)
- **Not** SimuDat Salud Risaralda. That connection was considered and ruled out by the author; it is
  recorded here only so the question is not re-opened later.

---

## Universidad Tecnológica de Pereira — Jan 2019 – Oct 2019 ✅

**Missing from the current CV.** The author confirmed this role on 2026-08-24 and asked for the CV
to be updated: it sits exactly in what previously looked like an employment gap between Woldev
(ends Jan 2019) and MVM (starts Oct 2019).

- Stack: **Java, Spring Boot** (backend) and **Angular** (frontend). Java and Spring Boot are not
  listed in the CV's technical skills and must be added.
- Sector: **public health** — a sector the CV summary does not currently mention.
- ⚠️ Still to capture: job title and the specific responsibilities to list as bullets.

### RIAS — Ministry of Health ✅

**RIAS** (*Rutas Integrales de Atención en Salud*) are Colombia's integrated health-care pathways,
established by the Ministry of Health and Social Protection in **Resolution 3202 of 2016** and
extended by **Resolution 3280 of 2018**, within the MIAS comprehensive care model. They shift the
system from a treatment-centred to a preventive approach across life stages, covering pathways such
as health promotion and maintenance (mandatory), maternal-perinatal, cardio-cerebro-vascular,
cancer, nutritional disorders, substance-use disorders and infectious diseases.

This is a national public-health programme, which makes it the largest-reach project in the
portfolio by affected population.

- Sources:
  - https://www.minsalud.gov.co/paginas/rutas-integrales-de-atencion-en-salud.aspx (checked 2026-08-24)
  - https://www.minsalud.gov.co/sites/rid/Lists/BibliotecaDigital/RIDE/DE/COM/enlace-minsalud-81-rias.pdf (checked 2026-08-24)
