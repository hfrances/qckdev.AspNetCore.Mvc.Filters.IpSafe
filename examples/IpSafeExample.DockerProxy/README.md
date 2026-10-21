# IpSafeExample.DockerProxy

ASP.NET Core API behind Nginx, with a public endpoint and two named IpSafe schemes.

## Configuration

- Nginx (`172.28.0.10`) is the trusted proxy.
- Nginx sends `X-Forwarded-For: 198.51.100.10` to simulate the client IP.
- `Office` allows `198.51.100.10`.
- `Partner` allows `198.51.100.20`.
- The attacker container (`172.28.0.30`) is outside both schemes and is not a trusted proxy.

Allowed-client rules are in `Security:IpSafeSchemes`. Trusted proxies are in `Security:ForwardedHeaders`. The existing weather endpoints use the default `Security:IpSafeList`.

## Run

From this folder:

```bash
docker compose up --build -d
```

## Endpoints

| URL through Nginx | IP rule | Expected status |
|---|---|---|
| http://localhost:8080/ipsafe/public | Any IP | `200` |
| http://localhost:8080/ipsafe/office | `Office` | `200` |
| http://localhost:8080/ipsafe/partner | `Partner` | `403` |

The public action uses `[AllowAnyIpAddress]`. The protected actions use `[IpSafeFilter("Office")]` and `[IpSafeFilter("Partner")]`.

```bash
curl -i http://localhost:8080/ipsafe/public
curl -i http://localhost:8080/ipsafe/office
curl -i http://localhost:8080/ipsafe/partner
```

## Automated checks

```bash
docker compose exec -T attacker sh /checks/verify.sh
```

The script waits for the API and checks this matrix:

| Request from attacker | Public | Office | Partner |
|---|---|---|---|
| Through Nginx | `200` | `200` | `403` |
| Direct to API with forged `X-Forwarded-For: 198.51.100.10` | `200` | `403` | `403` |
| Direct to API with forged `X-Forwarded-For: 198.51.100.20` | `200` | `403` | `403` |

It exits with code `1` on a failed check and prints `All 9 checks passed.` on success.

The API is also available directly on port `8081` for manual spoofing checks:

```bash
curl -i -H "X-Forwarded-For: 198.51.100.10" http://localhost:8081/ipsafe/office
```

Expected status: `403`. Forwarded headers from callers other than Nginx are ignored.

## Stop

```bash
docker compose down
```
