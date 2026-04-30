using EasySave.Services.Encryption;

namespace EasySave.Tests;

public class CryptoSoftEncryptionServiceTests
{
    [Fact]
    public void ShouldEncrypt_ReturnsFalse_WhenCryptoSoftPathIsEmpty()
    {
        // Arrange — chemin vide : la garde doit court-circuiter avant de vérifier l'extension
        var service = new CryptoSoftEncryptionService(
            cryptoSoftPath: "",
            encryptionKey: "key",
            encryptedExtensions: [".txt"]);

        // Act
        var result = service.ShouldEncrypt("document.txt");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ShouldEncrypt_ReturnsTrue_ForMatchingExtension()
    {
        // Arrange — extension dans la liste, passée en majuscules pour vérifier ToLowerInvariant
        var service = new CryptoSoftEncryptionService(
            cryptoSoftPath: @"C:\tools\CryptoSoft.exe",
            encryptionKey: "key",
            encryptedExtensions: [".txt"]);

        // Act
        var result = service.ShouldEncrypt("document.TXT");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ShouldEncrypt_ReturnsFalse_ForNonMatchingExtension()
    {
        // Arrange
        var service = new CryptoSoftEncryptionService(
            cryptoSoftPath: @"C:\tools\CryptoSoft.exe",
            encryptionKey: "key",
            encryptedExtensions: [".txt"]);

        // Act
        var result = service.ShouldEncrypt("image.png");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Encrypt_ReturnsSuccessZero_WhenExtensionNotTargeted()
    {
        // Arrange — .pdf n'est pas dans la liste, Process.Start ne doit pas être appelé
        var service = new CryptoSoftEncryptionService(
            cryptoSoftPath: @"C:\tools\CryptoSoft.exe",
            encryptionKey: "key",
            encryptedExtensions: [".txt"]);

        // Act
        var (success, ms) = service.Encrypt("image.pdf");

        // Assert
        Assert.True(success);
        Assert.Equal(0, ms);
    }

    [Fact]
    public void Encrypt_ReturnsFailureZero_WhenCryptoSoftPathDoesNotExist()
    {
        // Arrange — extension ciblée mais exécutable absent → échec sans lancer de processus
        var service = new CryptoSoftEncryptionService(
            cryptoSoftPath: @"C:\nonexistent\CryptoSoft.exe",
            encryptionKey: "key",
            encryptedExtensions: [".txt"]);

        // Act
        var (success, ms) = service.Encrypt("document.txt");

        // Assert
        Assert.False(success);
        Assert.Equal(0, ms);
    }
}
