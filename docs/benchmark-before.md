# Benchmark — Before (baseline)

## Environment

| | |
|---|---|
| Date | 2026-07-22 |
| Commit tested | `73c9333` (base project, before any workshop changes) |
| OS | Windows 11 Home Single Language |
| Server | Kestrel (ASP.NET Core, `dotnet run`) |
| Tool | ApacheBench (`ab`) 2.3 |
| Command pattern | `ab -n 1000 -c 10 <url>` |

> All three endpoints below were measured under identical conditions
> (same machine, same `-n 1000 -c 10`, no other heavy processes running)
> so the before/after comparison is valid.

---

## Endpoint 1 — `GET /{shortUrl}` (redirect)

```
ab -n 1000 -c 10 http://localhost:5064/aspnet
```

| Metric | Value |
|---|---|
| Requests/sec | 181.46 |
| p50 (ms) | 6 |
| p90 (ms) | 159 |
| p99 (ms) | 719 |
| Transfer rate (KB/sec) | 29.24 |
| Failed requests | 0 |

Note: `Non-2xx responses: 1000` is expected here — the redirect endpoint
replies with a `302`, which `ab` does not follow, so every response is
correctly counted as non-2xx.

---

## Endpoint 2 — `GET /` (home page)

```
ab -n 1000 -c 10 http://localhost:5064/
```

| Metric | Value |
|---|---|
| Requests/sec | 1945.45 |
| p50 (ms) | 3 |
| p90 (ms) | 4 |
| p99 (ms) | 9 |
| Transfer rate (KB/sec) | 10010.35 |
| Failed requests | 0 |

---

## Endpoint 3 — `GET /Login`

```
ab -n 1000 -c 10 http://localhost:5064/Login
```

| Metric | Value |
|---|---|
| Requests/sec | 2162.33 |
| p50 (ms) | 4 |
| p90 (ms) | 6 |
| p99 (ms) | 8 |
| Transfer rate (KB/sec) | 12308.79 |
| Failed requests | 0 |

---

## Notes

- This run reflects the original Shortly codebase (commit `73c9333`),
  before any of the HTTP protocol improvements from this workshop were
  applied — no compression, no security headers, no timing middleware,
  no conditional GET, no rate limiting, and no hashed short codes.
- `/{shortUrl}` is noticeably slower and has much higher tail latency
  (p99 = 719ms) than the other two endpoints. This makes sense: it hits
  the database to look up the link and increment its click counter on
  every single request, while `/` and `/Login` mostly serve static Razor
  Pages content.
- Raw `ab` output files are kept alongside this report for reference
  (`before-redirect.txt`, `before-index.txt`, `before-login.txt`).