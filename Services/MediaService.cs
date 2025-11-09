using Microsoft.Extensions.Logging;

namespace AdvancedNoteApp.Services;

public class MediaService
{
    private readonly ILogger<MediaService> logger;
    public MediaService(ILogger<MediaService> logger)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<string?> CapturePhotoAsync()
    {
        try
        {
            var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
            if (status != PermissionStatus.Granted)
                status = await Permissions.RequestAsync<Permissions.Camera>();

            if (status != PermissionStatus.Granted)
            {
                await Application.Current.MainPage.DisplayAlert("Hiba", "A kamera használatához engedély szükséges.", "OK");
                return null;
            }

            var result = await MediaPicker.Default.CapturePhotoAsync();
            if (result == null)
                return null;

            await using var sourceStream = await result.OpenReadAsync();
            var destFile = CreateImagePath(result.FileName);
            Directory.CreateDirectory(Path.GetDirectoryName(destFile)!);

            await using var destStream = File.OpenWrite(destFile);
            await sourceStream.CopyToAsync(destStream);

            return destFile;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "CapturePhotoAsync failed");
            return null;
        }
    }

    private static string CreateImagePath(string? originalFileName)
    {
        var ext = Path.GetExtension(originalFileName ?? ".jpg");
        var fileName = $"{Guid.NewGuid()}{ext}";
        var imagesDir = Path.Combine(FileSystem.AppDataDirectory, "images");
        return Path.Combine(imagesDir, fileName);
    }

}