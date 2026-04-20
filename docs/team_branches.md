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

## 🔧 Commandes Git utiles

**Démarrer une sous-branche**
```bash
git checkout feature/facade-observers-main
git checkout -b feature/facade-observers/backup-service
git push -u origin feature/facade-observers/backup-service
```

**Mettre à jour sa branche depuis develop**
```bash
git checkout feature/[branche]
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
