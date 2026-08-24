# Architecture Decision Records

Every decision that is expensive to reverse, or that a reviewer would reasonably question, is
recorded here as a numbered ADR. Each one states the context that forced the decision, the decision
itself, and the consequences — including the bad ones.

An ADR is never edited to hide history. Superseded records keep their number and gain a
`Superseded by ADR-XXXX` note.

| # | Decision | Status |
|---|---|---|
| [0001](0001-bilingual-content.md) | Bilingual content, English default, Spanish parity | Accepted |
| [0002](0002-frontend-rendering.md) | Prerendered Angular (SSG) with static content fallback | Accepted |
| [0003](0003-content-privacy.md) | What personal and client information is published | Accepted |
| [0004](0004-backend-architecture.md) | Pragmatic Clean Architecture, minimal APIs, one storage seam | Accepted |
| [0005](0005-engineering-section.md) | The engineering section ships with the app; its numbers are generated | Accepted |
| 0006 | Hosting and whether Kubernetes is justified | Pending — phase 11 |

## Format

```markdown
# ADR-XXXX — Title

- **Status:** Proposed | Accepted | Superseded by ADR-YYYY
- **Date:** YYYY-MM-DD

## Context
## Decision
## Consequences
```
