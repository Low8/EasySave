# EasySave – System Programming Project (ProSoft)

## Context

EasySave is a backup software developed as part of the **System Programming course (PGE A3 FISE INFO – Génie Logiciel)**.

The project is developed in collaboration with the fictional company **ProSoft**, which specializes in software publishing.  
The objective is to design and evolve a professional backup solution across multiple versions, while respecting strict constraints in terms of architecture, code quality, and maintainability.

---

## Technologies

- Language: **C#**
- Framework: **.NET 8.0**
- IDE: Visual Studio 2022+
- Version control: **Git / GitHub**
- Architecture: UML-based design (recommended: ArgoUML)

---

## Project Overview

EasySave is a backup application designed to evolve across multiple versions:

### Version 1.0 (Console Application)

- Console-based application
- Up to 5 backup jobs
- Backup types:
  - Full backup
  - Differential backup
- Multilingual support (French / English)
- Execution of:
  - Single backup job
  - Sequential jobs (e.g., 1-3 or 1;3 via CLI)
- Supports local, external, and network drives
- Recursive backup of directories (files + subfolders)

### Logging System

- Daily log file (real-time updates)
- Includes:
  - Timestamp
  - Backup name
  - Source / destination paths (UNC format)
  - File size
  - Transfer time (ms)
- Implemented via **EasyLog.dll**

### Status File

- Real-time backup progress tracking
- Stored in a JSON file
- Includes:
  - Job name
  - Status (Active / Inactive)
  - Progress (files, size, remaining)

---

## Constraints

- Clean and maintainable code (no duplication)
- English-readable code and documentation
- Respect naming conventions
- Limited function size
- JSON format for logs and status files
- Avoid hardcoded paths (e.g., `C:\temp`)

---

## Future Versions

### Version 1.1
- Choice of log format (JSON / XML)

### Version 2.0
- Graphical interface (WPF or Avalonia)
- Unlimited backup jobs
- File encryption via external tool (CryptoSoft)
- Business software detection (pause/stop backups)
- Improved logging (encryption time)

### Version 3.0 (planned)
- Full GUI control per backup (Play / Pause / Stop)

---

## Development Rules

- GitHub used for full versioning
- UML diagrams required before each deliverable
- Tutor must be invited to repository
- Focus on modular architecture and scalability

---

## Version 2.0 Features (Implemented)

### Single-Instance Application
- Prevents multiple instances using Mutex Windows
- Separate enforcement for Console and GUI applications
- Displays error with existing instance PID

### Priority File Management
- Parallel backup with priority queuing system
- Priority files transfer before non-priority files
- Configurable file size limits for parallel transfers
- Support for priority file extensions

### Business Software Detection
- Real-time monitoring of business processes (Excel, Outlook, etc.)
- Automatic backup pause/resume when business software detected
- Enhanced process guard with event-driven architecture
- Configurable business software list

### Docker Logging Centralization
- Centralized log storage server
- HTTP API for remote log submission
- Hybrid logging (local + remote)
- Docker volume persistence for log storage

---

## Docker Setup & Usage

### Prerequisites
- Docker Desktop installed and running
- .NET 8.0 SDK

### Starting the Centralized Log Server
```bash
cd docker
docker-compose up --build
```

### Docker Configuration
The Docker container provides:
- **Web Server**: ASP.NET Core application on ports 5000/5001
- **Log Storage**: Persistent volume at `/app/logs`
- **API Endpoints**:
  - `POST /api/logs` - Single log entry
  - `POST /api/logs/batch` - Multiple log entries
  - `GET /health` - Health check

### Client Configuration for Remote Logging
Edit `settings.json`:
```json
{
  "LogDestination": "Hybrid",
  "RemoteLogServerUrl": "http://localhost:5000",
  "RemoteLogServerApiKey": "your-api-key",
  "RemoteLogTimeoutMs": 5000
}
```

### Log Destination Options
- **Local**: Only local log files
- **Centralized**: Only remote server storage
- **Hybrid**: Both local and remote storage

### Docker Management
```bash
# Start in background
docker-compose up -d

# Stop server
docker-compose down

# View logs
docker logs easysave-log-server

# Access stored logs
docker exec easysave-log-server ls /app/logs
docker exec easysave-log-server cat /app/logs/centralized-*.json
```

---

## Testing Guide

### Quick Start Testing
```bash
# Test Console Application
dotnet run --project EasySave.Console/EasySave.Console.csproj

# Test GUI Application
dotnet run --project GUI/GUI.csproj
```

### Test Scenarios

#### 1. Basic Backup Functionality
```powershell
# List backup jobs
list

# Run single backup
run 1

# Run sequential backups
run 1-3
run 1;3
```

#### 2. Single-Instance Protection
```powershell
# Open two PowerShell windows and run:
dotnet run --project EasySave.Console/EasySave.Console.csproj
# Second instance should show error and exit
```

#### 3. Business Software Detection
1. Configure in `settings.json`:
```json
{
  "BusinessSoftwareNames": ["excel", "outlook", "CalculatorApp.exe"]
}
```
2. Start Excel/Outlook
3. Run backup - should pause
4. Close business software - backup should resume

#### 4. Priority File Management
Configure in `config.json`:
```json
{
  "PriorityExtensions": [".docx", ".xlsx", ".pdf"]
}
```
Priority files will be transferred before non-priority files.

#### 5. Docker Logging Test
```bash
# Start Docker server
cd docker && docker-compose up -d

# Configure hybrid logging in settings.json
{
  "LogDestination": "Hybrid",
  "RemoteLogServerUrl": "http://localhost:5000"
}

# Run backup and check both local and remote logs
```

### Log Analysis
```powershell
# View today's logs
Get-Content logs\daily\2026-05-12.json | ConvertFrom-Json | Format-Table Timestamp, BackupName, SourcePath, FileSize, TransferMs

# Check live backup status
Get-Content logs\live\*.json

# View Docker server logs
docker exec easysave-log-server cat /app/logs/centralized-*.json
```

### Configuration Files

#### Backup Jobs (config.json)
```json
[
  {
    "Name": "TestBackup",
    "SourceDir": "C:\\test\\source",
    "TargetDir": "C:\\test\\backup",
    "Type": 0,  // 0=Full, 1=Differential
    "IsActive": true,
    "PriorityExtensions": [".docx", ".pdf"]
  }
]
```

#### Application Settings (settings.json)
```json
{
  "LogFormat": "Json",
  "Language": "en",
  "LogDestination": "Local",
  "BusinessSoftwareNames": ["excel", "outlook"],
  "CryptoSoftPath": "",
  "EncryptionKey": ""
}
```

### Troubleshooting
- **Docker not running**: Start Docker Desktop
- **Port conflicts**: Change ports in docker-compose.yml
- **Missing directories**: Create test source/target folders
- **Permission errors**: Run PowerShell as Administrator

### Reset Test Environment
```bash
docker-compose down
Remove-Item logs\* -Recurse -Force
```

---

## Goal

Build a **scalable, maintainable and professional backup system** that can evolve quickly across multiple versions while minimizing future development cost.

---

## Quick Start Commands

```bash
# Start GUI Application
dotnet run --project GUI/GUI.csproj

# Start Console Application
dotnet run --project EasySave.Console/EasySave.Console.csproj

# Start Docker Log Server
cd docker && docker-compose up --build