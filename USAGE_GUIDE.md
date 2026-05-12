# 🎯 USAGE GUIDE - EasySave Advanced Features

## **1. Single-Instance Enforcement**

### Description
Empêche plusieurs instances de l'application de s'exécuter simultanément sur le même ordinateur.

### How It Works
- Utilise un **Mutex Windows** scoped à l'utilisateur actuel
- Format: `Global\EasySave_{ApplicationName}_{UserName}`
- Instances séparées pour Console et GUI

### Behavior
```
Première instance (Console):
  → EnsureSingleInstance() → Retourne manager → Continue l'exécution

Deuxième instance (Console):
  → EnsureSingleInstance() → Affiche erreur → Quit(1)

Première instance (GUI) + Première instance (Console):
  → Tous les deux fonctionnent (instances différentes)
```

### Configuration
Aucune - Automatique via `ConsoleApplicationHelper.EnsureSingleInstance()` et `GuiApplicationHelper.EnsureSingleInstance()`

---

## **2. Priority File Management**

### Description
Gère les transferts de fichiers parallèles avec contraintes de priorité et de taille.

### Key Rules
1. **Aucun fichier non-prioritaire** ne peut être transféré tant que des **prioritaires attendent**
2. **Maximum 2 fichiers > n KB** en parallèle (évite saturation bande)
3. **Petit fichier** peut être transféré en parallèle avec un **gros fichier**

### Configuration in Config

```json
{
  "Name": "ImportantFilesBackup",
  "SourceDir": "C:\\Documents",
  "TargetDir": "D:\\Backup",
  "Type": "Full",
  "IsActive": true,
  "PriorityExtensions": [".docx", ".xlsx", ".pptx"]
}
```

### Usage Example

```csharp
var manager = new ParallelTransferManager(
    maxParallelFileSizeKB: 1024,  // Files > 1 MB considered "large"
    maxParallelTransfers: 3);      // Max 3 concurrent transfers

// Subscribe to events
manager.TransferStarted += (s, task) => 
    Console.WriteLine($"🟢 Started: {Path.GetFileName(task.SourcePath)}");

manager.TransferCompleted += (s, task) => 
    Console.WriteLine($"✅ Completed: {Path.GetFileName(task.SourcePath)}");

manager.TransferFailed += (s, e) => 
    Console.WriteLine($"❌ Failed: {e.task.SourcePath} - {e.error.Message}");

// Get files from backup job
var files = Directory.GetFiles(jobConfig.SourceDir, "*", SearchOption.AllDirectories);

// Enqueue all files
foreach (var file)
{
    var isPriority = jobConfig.PriorityExtensions.Contains(Path.GetExtension(file));

    manager.EnqueueTask(new FileTransferTask
    {
        SourcePath = file,
        DestPath = Path.Combine(jobConfig.TargetDir, 
                   Path.GetRelativePath(jobConfig.SourceDir, file)),
        FileSize = new FileInfo(file).Length,
        IsPriority = isPriority,
        JobIndex = 0
    });
}

// Process queue
while (true)
{
    var task = manager.DequeueNextTask();
    if (task == null)
    {
        await Task.Delay(100);
        continue;
    }

    try
    {
        // Transfer file
        File.Copy(task.SourcePath, task.DestPath, overwrite: true);
        manager.CompleteTask(task);
    }
    catch (Exception ex)
    {
        manager.FailTask(task, ex);
    }
}
```

### Scenario Example
```
Queue:
  [P] small.txt (100 KB, priority)      ← En attente
  [NP] large-video.mp4 (2 GB)           ← En attente
  [P] document.docx (5 MB, priority)    ← En attente
  [NP] backup.zip (500 MB)              ← En attente

Exécution:
  1. large-video.mp4 commence (> 1 GB)
  2. small.txt commence (< 1 MB) ← Peut démarrer avec large-video
  3. On ATTEND: document.docx ne peut PAS démarrer
       → large-video continue
       → small.txt continue
       → document.docx attend (prioritaire bloqué)
  4. small.txt termine
  5. document.docx commence maintenant (prioritaire en attente)
  6. large-video termine
  7. backup.zip commence (non-prioritaire)
```

---

## **3. Auto-Resume Business Software**

### Description
Pause automatiquement les backups à la détection de logiciels métier et reprend à leur fermeture.

### Configuration in Settings

```json
{
  "BusinessSoftwareNames": ["excel", "outlook", "msaccess", "notepad"]
}
```

### How It Works
```
⏱️ Timer every 1 second (configurable):
   1. Check if any business software is running
   2. If detected AND backup running → Pause backup
   3. If not detected AND backup paused → Resume backup
   4. Emit events for UI notification
```

### Usage Example

```csharp
// Initialize guard
var guard = new EnhancedProcessBusinessSoftwareGuard(
    businessSoftwareNames: appSettings.BusinessSoftwareNames,
    checkIntervalMs: 1000);

// Subscribe to events
guard.SoftwareDetected += (s, softwareName) => 
{
    Console.WriteLine($"⚠️ BACKUP PAUSED: {softwareName} is running");
    uiViewModel.Status = "Paused - Business software detected";
};

guard.SoftwareShutdown += (s, msg) => 
{
    Console.WriteLine($"✅ BACKUP RESUMING: {msg}");
    uiViewModel.Status = "Resumed";
};

// In backup loop
var cts = new CancellationTokenSource();

await foreach (var result in backupJob.Execute(cts.Token))
{
    if (guard.IsRunning())
    {
        Console.WriteLine("Pausing backup...");
        cts.Cancel();

        // Wait for software to close
        while (guard.IsRunning())
        {
            await Task.Delay(500);
        }

        // Resume
        cts = new CancellationTokenSource();
        Console.WriteLine("Resuming backup...");
        continue;
    }

    // Process backup...
}

guard.Dispose();  // Stop monitoring on exit
```

### Process Matching
- Non sensible à la casse
- Automatique `.exe` removal
- Liste: ["excel"] → Détecte `EXCEL.EXE` ou `excel.exe`

---

## **4. Centralized Log Storage with Docker**

### Description
Centralise tous les logs de backup (multi-client) sur un serveur Docker unique pour gestion simplifiée.

### Configuration Options

#### Mode 1: Local Only (Default)
```json
{
  "LogDestination": "Local",
  "RemoteLogServerUrl": ""
}
```
- Logs uniquement locaux: `/logs/daily/YYYY-MM-DD.json`

#### Mode 2: Centralized Only
```json
{
  "LogDestination": "Centralized",
  "RemoteLogServerUrl": "http://your-server:5000",
  "RemoteLogServerApiKey": "secret-key-here",
  "RemoteLogTimeoutMs": 5000
}
```
- Logs uniquement sur serveur: `/app/logs/centralized-YYYY-MM-DD.json`

#### Mode 3: Hybrid (Recommended)
```json
{
  "LogDestination": "Hybrid",
  "RemoteLogServerUrl": "http://your-server:5000",
  "RemoteLogServerApiKey": "secret-key-here",
  "RemoteLogTimeoutMs": 5000
}
```
- Logs locaux + serveur (plus de sécurité)

### Docker Deployment

#### Prerequisites
- Docker et Docker Compose installés
- Port 5000 disponible

#### Start Server
```bash
cd docker
docker-compose up --build -d

# Vérifier status
docker-compose ps
docker logs easysave-log-server

# Health check
curl http://localhost:5000/health
```

#### Stop Server
```bash
docker-compose down

# Keep volumes
docker-compose down -v  # Also remove logs
```

### API Endpoints

#### Single Log Entry
```bash
curl -X POST http://localhost:5000/api/logs \
  -H "Content-Type: application/json" \
  -H "X-API-Key: your-api-key" \
  -d '{
    "Timestamp": "2026-05-15T10:30:00Z",
    "BackupName": "DocumentsBackup",
    "SourcePath": "C:\\Documents\\file.txt",
    "DestPath": "D:\\Backup\\file.txt",
    "FileSize": 5242880,
    "TransferMs": 1234,
    "EncryptionMs": 0
  }'
```

#### Batch Entries
```bash
curl -X POST http://localhost:5000/api/logs/batch \
  -H "Content-Type: application/json" \
  -H "X-API-Key: your-api-key" \
  -d '[{...}, {...}, ...]'
```

#### Health Check
```bash
curl http://localhost:5000/health
# Response: 200 OK
```

### Storage Structure
```
/app/logs/
├── centralized-2026-05-15.json
├── centralized-2026-05-16.json
└── centralized-2026-05-17.json
```

Each file contains:
```json
[
  {
    "Timestamp": "2026-05-15T10:30:00Z",
    "BackupName": "DocumentsBackup",
    "SourcePath": "C:\\Documents\\file.txt",
    "DestPath": "D:\\Backup\\file.txt",
    "FileSize": 5242880,
    "TransferMs": 1234,
    "EncryptionMs": 0
  },
  ...
]
```

### Data Retention

```csharp
var storageService = new CentralizedLogStorageService("/app/logs");

// Delete logs older than 30 days
int deletedCount = storageService.DeleteOldLogs(daysToKeep: 30);
Console.WriteLine($"Deleted {deletedCount} old log files");
```

### Monitoring & Backup

#### Get logs for specific date
```csharp
var logs = storageService.GetLogsForDate(new DateTime(2026, 5, 15));
```

#### Get logs in date range
```csharp
var logs = storageService.GetLogsForDateRange(
    startDate: new DateTime(2026, 5, 1),
    endDate: new DateTime(2026, 5, 31));
```

#### Backup logs
```bash
# Copy Docker volume to local
docker cp easysave-log-server:/app/logs ./backup-logs

# Or use volume mount in docker-compose
volumes:
  - /home/backups/easysave-logs:/app/logs
```

### Networking

#### Local Development
```
Client (localhost:any) → Server (localhost:5000)
```

#### Production Network
```yaml
version: '3.8'
services:
  easysave-logs:
    container_name: easysave-log-server
    ports:
      - "5000:5000"
    networks:
      - easysave-network

  # Optional: Reverse proxy (nginx)
  nginx:
    image: nginx:alpine
    ports:
      - "80:80"
    depends_on:
      - easysave-logs
```

Client configuration:
```json
{
  "RemoteLogServerUrl": "http://easysave-logs.example.com"
}
```

---

## **🔧 Troubleshooting**

### Single-Instance Issues
```
❌ "Another instance is already running"
✅ Solution: Fermer la première instance avant de relancer
```

### Priority Files Not Working
```
❌ "All files transfer at same time"
✅ Solution: Vérifier PriorityExtensions dans config.json
✅ Solution: Vérifier maxParallelTransfers et maxParallelFileSizeKB
```

### Auto-Resume Not Triggering
```
❌ "Backup never pauses when software starts"
✅ Solution: Vérifier BusinessSoftwareNames (case-insensitive)
✅ Solution: Vérifier processus exact: tasklist | findstr excel
```

### Docker Logs Not Receiving
```
❌ "Connection refused"
✅ Solution: docker ps (vérifier container running)
✅ Solution: curl http://localhost:5000/health
✅ Solution: Vérifier RemoteLogServerUrl dans settings.json

❌ "401 Unauthorized"
✅ Solution: Vérifier X-API-Key header
```

---

**Version:** 1.0  
**Last Updated:** 2026-05-15  
**Status:** Ready for Production ✅
