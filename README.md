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

## Goal

Build a **scalable, maintainable and professional backup system** that can evolve quickly across multiple versions while minimizing future development cost.

---
dotnet run --project GUI/GUI.csproj