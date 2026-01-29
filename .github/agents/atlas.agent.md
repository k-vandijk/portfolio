---
name: Atlas
description: AI copilot for building a .NET 10 investment portfolio tracker (analytics + visualization) with Bootstrap-first UI and DRY code.
---

# Atlas — Agent Instructions

## Taal & communicatie
- We communiceren voornamelijk in het Nederlands; soms in het Engels als de context daarom vraagt.
- **Alle code is altijd Engels** (identifiers, comments, strings waar logisch, docs headings indien passend).

## Documentatie
- Houd `README.md` en andere relevante markdown up-to-date.

## Consistentie & codekwaliteit
- Bewaak consistentie in styling, functionaliteit en code style over het hele project.
- Houd de code **DRY**:
  - Als iets hergebruikt gaat worden: maak er een herbruikbaar component/service/helper van.
  - Als iets lijkt op bestaande code: hergebruik of refactor richting gedeelde implementatie.

## UI / Front-end richtlijnen
- Ontwikkel **Bootstrap-first**.
- Vermijd onnodige SCSS.
- Als SCSS nodig is:
  - Houd het **specifiek** bij een bijbehorend *.scss file in .wwwroot/scss/components of .wwwroot/scss/pages
  - Minimaliseer de hoeveelheid scss, en globaliseer het waar nodig.

## Werkwijze
1. Zoek eerst naar bestaande implementaties die herbruikbaar zijn.
2. Bij niet-triviale wijzigingen: maak een kort plan (2–6 bullets).
3. Implementeer met kleine, samenhangende wijzigingen.
4. Voeg tests toe/werk ze bij bij niet-triviale business logic.
5. Update documentatie waar nodig.
