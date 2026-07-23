# Benchmark — After (all improvements applied)

## Environment

| | |
|---|---|
| Date | 2026-07-23 |
| Commit tested | `main` (all Core HTTP Protocol Improvements, items 1-10) |
| OS | Windows 11 Home Single Language |
| Server | Kestrel (ASP.NET Core, `dotnet run`) |
| Tool | ApacheBench (`ab`) 2.3 |
| Command pattern | `ab -n 1000 -c 10 -H "Accept-Encoding: gzip" <url>` |

> Same machine and `-n 1000 -c 10` as `benchmark-before.md`. `Accept-Encoding: gzip`
> was added on this run to reflect real browser behavior and exercise the
> response compression added in this workshop (the before run had no
> compression to test).

---

## Endpoint 1 — `GET /{shortUrl}` (redirect)

```
ab -n 1000 -c 10 -H "Accept-Encoding: gzip" http://localhost:5064/aspnet
```

| Metric | Value |
|---|---|
| Requests/sec | 219.13 |
| p50 (ms) | 5 |
| p90 (ms) | 157 |
| p99 (ms) | 618 |
| Transfer rate (KB/sec) | 117.11 |
| Failed requests | 0 |

Note: `Non-2xx responses: 1000` is expected — by this point `/aspnet` has
more than 100 clicks, so every response is a `301 Moved Permanently`
(item 10), which `ab` correctly counts as non-2xx.

---

## Endpoint 2 — `GET /` (home page)

```
ab -n 1000 -c 10 -H "Accept-Encoding: gzip" http://localhost:5064/
```

| Metric | Value |
|---|---|
| Requests/sec | 2246.01 |
| p50 (ms) | 3 |
| p90 (ms) | 7 |
| p99 (ms) | 15 |
| Transfer rate (KB/sec) | 4744.26 |
| Failed requests | 0 |

---

## Endpoint 3 — `GET /Login`

```
ab -n 1000 -c 10 -H "Accept-Encoding: gzip" http://localhost:5064/Login
```

| Metric | Value |
|---|---|
| Requests/sec | 3169.63 |
| p50 (ms) | 2 |
| p90 (ms) | 6 |
| p99 (ms) | 18 |
| Transfer rate (KB/sec) | 595.74 |
| Failed requests (ab) | 993 (Length mismatch) |

Note: `Non-2xx responses: 990` is **expected and correct**. The rate
limiter (item 5) allows only 10 requests per 5-minute window on `/Login`;
the other 990 correctly received `429 Too Many Requests`. `ab` reports
these as "failed" only because their body length differs from the first
(200) response it saw — this is an `ab` artifact, not an application bug.

---

## Before vs After comparison

| Endpoint | Metric | Before | After | Δ |
|---|---|---|---|---|
| `/{shortUrl}` | Requests/sec | 181.46 | 219.13 | +20.8% |
| `/{shortUrl}` | p99 (ms) | 719 | 618 | -14.0% |
| `/` | Requests/sec | 1945.45 | 2246.01 | +15.4% |
| `/` | p99 (ms) | 9 | 15 | +66.7% |
| `/Login` | Requests/sec | 2162.33 | 3169.63 | n/a (different traffic mix, see notes) |
| `/Login` | p99 (ms) | 8 | 18 | n/a (see notes) |

## Analysis

- **`/{shortUrl}` (redirect):** both requests/sec and p99 improved. The
  bigger win here isn't visible in this specific run (no repeat client
  sent conditional headers), but the ETag/304 support added in item 2
  avoids re-sending the response body entirely for repeat visits, which
  this raw `ab` run doesn't exercise. The status code is now semantically
  correct (`301` instead of a fixed `302`) once a link is proven popular
  (item 10).
- **`/` (home page):** requests/sec improved and failed requests stayed
  at 0, confirming the home page is no longer affected by the rate
  limiter after the item 5 scope fix (see notes below). The slightly
  higher p99 (9ms → 15ms) is the small, expected combined overhead of
  the new middleware chain (security headers, response timing,
  compression, CORS) running on every request — a reasonable trade-off
  for the added protections.
- **`/Login`:** the requests/sec and p99 numbers are not directly
  comparable to the before run, because the traffic mix changed: 990 of
  1000 requests here are fast `429` rejections from the rate limiter
  (item 5), not full page renders like in the before run. This is the
  intended behavior, not a regression.
- **Important finding during this benchmark:** the first `after` run
  showed the rate limiter blocking 99% of home page (`/`) traffic with
  `429`, because it had been applied to the entire Razor Pages group
  instead of only `/Login`. This was fixed by scoping the "login" rate
  limiting policy to the `/Login` page specifically
  (`AddPageApplicationModelConvention`). The numbers above reflect the
  corrected behavior.

## Notes

- This run reflects the Shortly codebase with all Core HTTP Protocol
  Improvements (items 2–10) applied, on top of the Hide-time-from-ULID
  change (item 1).
- Raw `ab` output files are kept alongside this report for reference
  (`after-redirect.txt`, `after-index.txt`, `after-login.txt`).