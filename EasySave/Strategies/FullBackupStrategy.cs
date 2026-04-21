using System.IO;
using System.Diagnostics; 
using System.Collections.Generic; 
using EasySave.Models; 

namespace EasySave.Strategies;

public class FullBackupStrategy : IBackupStrategy
{
    public List<BackupResult> Execute(string sourceDir, string targetDir)
    {
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
            
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            
            File.Copy(filePath, destFile, true);
            
            stopwatch.Stop();
            
            long size = new FileInfo(filePath).Length;
            
            results.Add(new BackupResult
            {
                SourcePath = filePath,
                DestPath = destFile,
                FileSize = size,
                TransferMs = stopwatch.ElapsedMilliseconds
            });
        }
        
        foreach (string directoryPath in Directory.GetDirectories(sourceDir))
        {
            string dirName = Path.GetFileName(directoryPath);
            string destDir = Path.Combine(targetDir, dirName);
            
            results.AddRange(Execute(directoryPath, destDir)); 
        }
        
        return results;
    }
}