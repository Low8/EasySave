# 🗂️ EasySave — Branch Assignments

## 🔀 Workflow
```
master
└── develop
    ├── feature/facade-observers-main     → Ewan
    ├── feature/backup-engine-main        → Ethan
    ├── feature/console-layer-main        → Louison
    └── feature/unit-tests-main           → Wilfried
```

**Règle** : on ne commit jamais directement sur `develop` ni sur `master`.
Chaque feature terminée → PR vers `develop` → review par 1 coéquipier → merge.

---

## 👤 Ewan — Facade & Observers

**Branche principale** : `feature/facade-observers-main`

| Sous-branche | Contenu |
|---|---|
| `feature/facade-observers/backup-service` | `BackupService.cs` — Facade, orchestration des jobs, logging, notification |
| `feature/facade-observers/state-observer` | `IStateObserver`, `IStateSubject`, `StateFileWriter` |

**Fichiers** :
- `EasySave.Services/BackupService.cs`
- `EasySave.Services/Interfaces/IStateObserver.cs`
- `EasySave.Services/Interfaces/IStateSubject.cs`
- `EasySave.Services/StateFileWriter.cs`

---

## 👤 Ethan — Backup Engine

**Branche principale** : `feature/backup-engine-main`

| Sous-branche | Contenu |
|---|---|
| `feature/backup-engine/strategies` | `BackupJob`, `FullBackupStrategy`, `DifferentialBackupStrategy`, `IBackupStrategy` |
| `feature/backup-engine/easylog` | `EasyLogger`, `LogEntry` |

**Fichiers** :
- `EasySave.Services/BackupJob.cs`
- `EasySave.Services/Strategies/IBackupStrategy.cs`
- `EasySave.Services/Strategies/FullBackupStrategy.cs`
- `EasySave.Services/Strategies/DifferentialBackupStrategy.cs`
- `EasyLog/EasyLogger.cs`
- `EasyLog/LogEntry.cs`

---

## 👤 Louison — Console Layer

**Branche principale** : `feature/console-layer-main`

| Sous-branche | Contenu |
|---|---|
| `feature/console-layer/cli-parser` | `CommandLineParser`, `CommandLineRunner` |
| `feature/console-layer/interactive-shell` | `InteractiveShell`, `ConsoleObserver`, `Program.cs` |

**Fichiers** :
- `EasySave/Program.cs`
- `EasySave/CommandLineParser.cs`
- `EasySave/CommandLineRunner.cs`
- `EasySave/InteractiveShell.cs`
- `EasySave/ConsoleObserver.cs`
- `EasySave/Resources/Messages.resx`
- `EasySave/Resources/Messages.fr.resx`

---

## 👤 Wilfried — Unit Tests

**Branche principale** : `feature/unit-tests-main`

| Sous-branche | Contenu |
|---|---|
| `feature/unit-tests/service-tests` | Tests `BackupService`, `IStateObserver` wiring |
| `feature/unit-tests/parser-tests` | Tests `CommandLineParser`, `LocalizationService` |

**Fichiers** :
- `EasySave.Tests/BackupServiceTests.cs`
- `EasySave.Tests/CommandLineParserTests.cs`
- `EasySave.Tests/LocalizationServiceTests.cs`
- `EasySave.Tests/DifferentialStrategyTests.cs`

⚠️ **Wilfried** : attends qu'Ewan merge `IStateObserver` et qu'Ethan merge `IBackupStrategy` sur `develop` avant de coder les tests — utilise des mocks (`FakeRepository`, `SpyObserver`) en attendant.

---

## 🔁 Commandes utiles

**Démarrer sa journée**
```bash
git checkout feature/[ta-branche]
git pull origin develop
```

**Merger une sous-branche vers la branche parent**
```bash
git checkout feature/backup-engine-main
git merge --squash feature/backup-engine/strategies
git commit -m "feat: backup strategies implemented"
git push origin feature/backup-engine-main
```

**Merger une feature terminée vers develop (via PR GitHub)**
→ Ouvrir une Pull Request sur GitHub : `feature/xxx-main` → `develop`
→ 1 review obligatoire avant merge

---

## ⚠️ Règles communes

- Ne jamais push directement sur `develop` ou `master`
- Nommage des commits : `feat:`, `fix:`, `chore:`, `test:`
- Le fichier `.claude/` est dans le `.gitignore` — ne pas le commiter
- Tout string affiché à l'utilisateur passe par `Messages.resx` — aucun texte hardcodé
