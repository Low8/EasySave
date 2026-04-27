using EasySave.Models;

namespace EasySave.Services.Formatters;

public interface IStateFormatter
{
    string FileExtension { get; }
    string Format(List<BackupState> states);
}
