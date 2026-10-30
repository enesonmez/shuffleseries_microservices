---
trigger: always_on
description: Consult the graphify knowledge graph at graphify-out/ for codebase and architecture questions.
---

## graphify

This project has a graphify knowledge graph at graphify-out/.

Rules:
- **ZORUNLU VE DEMİR KURAL (Sıfır Grep/Find İlkesi):** Kod tabanındaki bileşenleri, sınıfları, metodları ve dosya konumlarını taramak için `grep`, `find` veya körü körüne toplu dosya okuma KESİNLİKLE YASAKLANMIŞTIR.
- Her oturumda ve her aramada mutlaka doğrudan `graphify query "<arama>"` (veya `graphify explain "<sembol>"`, `graphify path "<A>" "<B>"`) komutunu çalıştır. Bu komut sistem genelinde (/opt/homebrew/bin/graphify) tanımlıdır; hem `uv` ortamını hem de sandbox-içi sıfır-maliyetli AST grafını otomatik yönetir.
- If graphify-out/wiki/index.md exists, navigate it instead of reading raw files
- Read graphify-out/GRAPH_REPORT.md only for broad architecture review or when query/path/explain do not surface enough context
- After modifying code files in this session, run `uv tool run --from graphifyy graphify . --update --code-only` to keep the graph current (AST-only, no API cost).
