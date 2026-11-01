# ADR-001: PostgreSQL only (no graph database for MVP)

- **Status:** Accepted, 2026-11-01
- **Deciders:** @cssedi (product owner), orchestrator

## Context
Search must match bursaries to a student's specific university qualification through a taxonomy (Qualification → Field of study (CESM) ← Bursary), filtered by level, marks and dates. We considered (A) PostgreSQL only, (B) PostgreSQL + Neo4j, and (C) PostgreSQL + Apache AGE.

At MVP scale the graph is small (about 20k nodes, about 100k edges) and queries are 2–3 hops deep.

## Decision
**A: PostgreSQL only.** The taxonomy is modelled as relational join tables (graph-shaped). Matching sits behind `IBursaryMatchingService` and `IBursaryRepository.SearchAsync`, so another backend can be added later without changing callers.

## Consequences
- ✅ One datastore, nothing to sync, cheapest hosting (no extra cost on the VPS; Neo4j AuraDB Professional would add about R1,100/month).
- ✅ EF Core + LINQ, which smaller implementer models handle reliably; one Testcontainers dependency.
- ❌ Graph-native features (similarity, recommendations) would need extra work. `pgvector` is reserved for "related qualifications" if needed.
- **Revisit when:** recommendation features are prioritised, or matching queries go past about 4 hops or get slow (p95 > 500 ms).
