# RAG Suite - Quick Start dla Administratorów

## 🚀 Szybka instalacja (1 komenda)

```bash
# Instalacja kompletna z HTTPS
curl -sSL https://raw.githubusercontent.com/jklebucki/rag-suite/main/quick-install.sh | sudo bash -s asystent.ad.citronex.pl admin@citronex.pl y
```

## 📋 Co robi ta komenda?

1. **Automatycznie tworzy** `/var/www/rag-suite`
2. **Klonuje** repozytorium z GitHub
3. **Instaluje** .NET 8, Node.js 20, Nginx
4. **Konfiguruje** serwis systemd dla API
5. **Buduje** aplikację .NET i React
6. **Konfiguruje** Nginx z rate limiting
7. **Instaluje** SSL z istniejących certyfikatów wildcard (jeśli wybrano)

## ✅ Wymagania przed instalacją

- Ubuntu 20.04+ z dostępem root
- Domena `asystent.ad.citronex.pl` wskazująca na serwer
- Otwarte porty 80 i 443 w firewall
- Dostęp do internetu

## 🔧 Po instalacji

### Sprawdź status
```bash
sudo systemctl status rag-api nginx
```

### Zobacz logi
```bash
sudo journalctl -fu rag-api
sudo tail -f /var/log/nginx/rag-suite.error.log
```

### Aktualizuj aplikację
```bash
cd /var/www/rag-suite
sudo ./deploy.sh
```

## 🌐 Dostęp do aplikacji

- **Frontend**: https://asystent.ad.citronex.pl
- **API Health**: https://asystent.ad.citronex.pl/health
- **System Health**: https://asystent.ad.citronex.pl/healthz/system

## ⚙️ Konfiguracja zewnętrznych serwisów

Edytuj: `/var/www/rag-suite/build/api/appsettings.Production.json`

```json
{
  "ConnectionStrings": {
    "ElasticSearch": "http://elasticsearch.ad.citronex.pl:9200"
  },
  "LLM": {
    "BaseUrl": "http://llm.ad.citronex.pl:11434",
    "Model": "llama3.1:8b"
  }
}
```

Po zmianie:
```bash
sudo systemctl restart rag-api
```

## 🚨 Rozwiązywanie problemów

### API nie działa
```bash
sudo journalctl -u rag-api --no-pager -n 50
sudo systemctl restart rag-api
```

### Nginx zwraca 502
```bash
curl http://localhost:5000/health
sudo nginx -t
sudo systemctl reload nginx
```

### SSL nie działa
```bash
sudo certbot certificates
sudo certbot renew --dry-run
```

## 📞 Wsparcie

Pełna dokumentacja: `PRODUCTION-DEPLOYMENT.md`

---
*RAG Suite - Inteligentny asystent dla Citronex*
