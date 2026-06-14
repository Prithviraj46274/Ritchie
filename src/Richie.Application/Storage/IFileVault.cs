namespace Richie.Application.Storage;

/// <summary>
/// Stores file bytes encrypted at rest under the app's local storage (PRD §19.3). Callers pass a
/// relative subfolder (e.g. photos/assets); the vault returns an opaque stored file name.
/// </summary>
public interface IFileVault
{
    string Save(byte[] content, string subfolder);
    byte[] Read(string subfolder, string storedFileName);
    void Delete(string subfolder, string storedFileName);
}
