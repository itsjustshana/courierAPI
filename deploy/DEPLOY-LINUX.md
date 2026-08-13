# MekMiCourier API deployment

This application shares the PriceBoard Linux server but uses its own resources:

- application directory: `/opt/mekmicourier-api`
- systemd service: `mekmicourier-api`
- local listener: `127.0.0.1:5265`
- public route prefix: `/apicour`

From PowerShell in the `WarehouseApi` directory:

```powershell
ssh root@216.55.143.70 "mkdir -p /opt/mekmicourier-api"
scp -r .\publish\linux-x64\* root@216.55.143.70:/opt/mekmicourier-api/
scp .\deploy\mekmicourier-api.service root@216.55.143.70:/etc/systemd/system/mekmicourier-api.service
ssh root@216.55.143.70 "systemctl daemon-reload && systemctl enable --now mekmicourier-api && systemctl status mekmicourier-api --no-pager"
```

The server reverse proxy must preserve the `/apicour` path and send it to
`http://127.0.0.1:5265`. After adding the proxy rule, reload Apache and verify:

```powershell
curl.exe -i https://gsyntaxserver.com/apicour/settings/public
```
