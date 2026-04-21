using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using EasySave.Models;

namespace EasySave.Strategies;

public class DifferentialBackupStrategy : IBackupStrategy
{
    public List<BackupResult> Execute(string sourceDir, string targetDir)
    {
        // 1. On prépare la liste de résultats
        List<BackupResult> results = new List<BackupResult>();

        if (!Directory.Exists(sourceDir))
        {
            throw new DirectoryNotFoundException($"Le dossier source n'existe pas : {sourceDir}");
        }

        Directory.CreateDirectory(targetDir);

        foreach (string filePath in Directory.GetFiles(sourceDir))
        {
            string fileName = Path.GetFileName(filePath);
            string destFile = Path.Combine(targetDir, fileName);

            bool shouldCopy = true;

            if (File.Exists(destFile))
            {
                DateTime sourceTime = File.GetLastWriteTime(filePath);
                DateTime destTime = File.GetLastWriteTime(destFile);

                if (destTime >= sourceTime)
                {
                    shouldCopy = false;
                }
            }

            // 2. Si on doit copier, on lance le chrono et on enregistre le résultat
            if (shouldCopy)
            {
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                File.Copy(filePath, destFile, true);

                stopwatch.Stop();
                long size = new FileInfo(filePath).Length;

                // On ajoute le fichier copié à notre liste
                results.Add(new BackupResult
                {
                    SourcePath = filePath,
                    DestPath = destFile,
                    FileSize = size,
                    TransferMs = stopwatch.ElapsedMilliseconds
                });
            }
        }

        foreach (string directoryPath in Directory.GetDirectories(sourceDir))
        {
            string dirName = Path.GetFileName(directoryPath);
            string destDir = Path.Combine(targetDir, dirName);
            
            // 3. On ajoute les résultats des sous-dossiers
            results.AddRange(Execute(directoryPath, destDir));
        }

        // 4. On retourne la liste
        return results;
    }
}