# EasySave Docker Centralized Logging

## Overview
This Docker setup provides centralized log storage for EasySave backups across multiple machines.

## Quick Start

### Build and Run
```bash
# Navigate to the solution root directory
cd C:\Users\...\EasySave

# Build and start containers
docker-compose -f docker/docker-compose.yml up --build -d

# View logs
docker-compose -f docker/docker-compose.yml logs -f easysave-logs

# Stop containers
docker-compose -f docker/docker-compose.yml down
```

### Verify Server is Running
```bash
# Health check
curl http://localhost:5000/health

# Expected response: 200 OK
```

## Configuration

### Client Settings (settings.json)
```json
{
  "LogDestination": "Hybrid",
  "RemoteLogServerUrl": "http://localhost:5000",
  "RemoteLogServerApiKey": "your-secret-key",
  "RemoteLogTimeoutMs": 5000
}
```

### Environment Variables
- `ASPNETCORE_ENVIRONMENT`: Production (default) or Development
- `LOG_STORAGE_PATH`: Path for log storage (default: /app/logs)
- `ASPNETCORE_URLS`: Server URL binding (default: http://+:5000)

## API Reference

### POST /api/logs
Send a single log entry.

**Request:**
```json
{
  "Timestamp": "2026-05-15T10:30:00Z",
  "BackupName": "DocumentsBackup",
  "SourcePath": "C:\\Documents\\file.txt",
  "DestPath": "D:\\Backup\\file.txt",
  "FileSize": 5242880,
  "TransferMs": 1234,
  "EncryptionMs": 0
}
```

**Response:** 
- `200 OK` - Entry received and stored
- `400 Bad Request` - Invalid format
- `401 Unauthorized` - Missing/invalid API key

### POST /api/logs/batch
Send multiple log entries at once.

**Request:**
```json
[
  { ...log1... },
  { ...log2... },
  { ...log3... }
]
```

**Response:** 
- `200 OK` - All entries stored
- `400 Bad Request` - Invalid format

### GET /health
Health check endpoint.

**Response:**
```json
{
  "status": "healthy",
  "timestamp": "2026-05-15T10:30:00Z"
}
```

## Storage

### File Structure
```
/app/logs/
├── centralized-2026-05-15.json  (~1-10 MB)
├── centralized-2026-05-16.json
└── centralized-2026-05-17.json
```

### Data Format
Each file contains a JSON array of log entries:
```json
[
  {
    "Timestamp": "2026-05-15T10:30:00Z",
    "BackupName": "DocumentsBackup",
    ...
  }
]
```

### Backup Storage
Log files are stored in a Docker volume: `easysave-logs-volume`

To backup logs locally:
```bash
docker cp easysave-log-server:/app/logs ./backup-logs
```

## Monitoring

### Container Status
```bash
docker ps
docker stats easysave-log-server
```

### View Server Logs
```bash
# Last 100 lines
docker logs --tail 100 easysave-log-server

# Follow in real-time
docker logs -f easysave-log-server
```

### Check Stored Logs
```bash
# List files in volume
docker exec easysave-log-server ls -lah /app/logs

# View specific log file
docker exec easysave-log-server cat /app/logs/centralized-2026-05-15.json | jq .
```

## Troubleshooting

### Container Won't Start
```bash
# Check logs
docker logs easysave-log-server

# Check port conflicts
netstat -ano | findstr :5000

# Free the port if needed
taskkill /PID <PID> /F
```

### Logs Not Received
```bash
# Test API endpoint
curl -X POST http://localhost:5000/api/logs \
  -H "Content-Type: application/json" \
  -H "X-API-Key: your-api-key" \
  -d '{"Timestamp":"2026-05-15T10:30:00Z","BackupName":"Test"}'

# Verify client can reach server
curl http://localhost:5000/health
```

### Disk Space Issues
```bash
# Check volume size
docker exec easysave-log-server du -sh /app/logs

# Delete old logs (inside container)
docker exec easysave-log-server rm /app/logs/centralized-*.json

# Or use cleanup (if implemented)
# docker exec easysave-log-server dotnet cleanup --days 30
```

## Production Deployment

### Using Docker Swarm
```yaml
version: '3.8'
services:
  easysave-logs:
    image: easysave-logs:latest
    deploy:
      replicas: 1
      restart_policy:
        condition: on-failure
        delay: 5s
        max_attempts: 3
    ports:
      - "5000:5000"
    volumes:
      - easysave-logs-volume:/app/logs
    networks:
      - easysave-network

volumes:
  easysave-logs-volume:
    driver: local

networks:
  easysave-network:
    driver: overlay
```

### Using Kubernetes
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: easysave-logs
spec:
  replicas: 1
  selector:
    matchLabels:
      app: easysave-logs
  template:
    metadata:
      labels:
        app: easysave-logs
    spec:
      containers:
      - name: easysave-logs
        image: easysave-logs:latest
        ports:
        - containerPort: 5000
        volumeMounts:
        - name: logs
          mountPath: /app/logs
      volumes:
      - name: logs
        persistentVolumeClaim:
          claimName: easysave-logs-pvc
---
apiVersion: v1
kind: Service
metadata:
  name: easysave-logs-service
spec:
  selector:
    app: easysave-logs
  ports:
  - protocol: TCP
    port: 5000
    targetPort: 5000
```

### Using Reverse Proxy (nginx)
```nginx
upstream easysave {
    server easysave-log-server:5000;
}

server {
    listen 80;
    server_name logs.example.com;

    location / {
        proxy_pass http://easysave;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;

        # Add auth if needed
        satisfy all;
        auth_basic "EasySave Logs";
        auth_basic_user_file /etc/nginx/.htpasswd;
    }
}
```

## Performance Tuning

### Increase Batch Size
In client settings.json:
```json
{
  "RemoteLogTimeoutMs": 10000  // Allow more time to collect batch
}
```

### Docker Resource Limits
```yaml
services:
  easysave-logs:
    deploy:
      resources:
        limits:
          cpus: '2'
          memory: 1G
        reservations:
          cpus: '1'
          memory: 512M
```

## Security

### API Key Authentication
Always use a strong API key:
```bash
# Generate a random key
openssl rand -base64 32
```

### Network Security
- Run on private network (not exposed to internet)
- Use reverse proxy with HTTPS in production
- Implement rate limiting
- Log access attempts

### Data Privacy
- Encrypt logs at rest (outside scope of this version)
- Use VPN or SSH tunneling for remote clients
- Regular backups of log volumes
- GDPR compliance (log retention policies)

---

**Last Updated:** 2026-05-15  
**Status:** Production Ready ✅
