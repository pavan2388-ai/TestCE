using Microsoft.AspNetCore.Mvc;

public class AttachmentsController : Controller
{
    private readonly BlobAttachmentService _blobService;

    public AttachmentsController(
        BlobAttachmentService blobService)
    {
        _blobService = blobService;
    }




    public async Task<IActionResult> Index()
    {
        var attachments = await _blobService.GetAllAsync();
        return View(attachments);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            TempData["Message"] = "Please select a file.";
            return RedirectToAction(nameof(Index));
        }

        // Basic example limit: 10 MB.
        if (file.Length > 10 * 1024 * 1024)
        {
            TempData["Message"] = "Maximum file size is 10 MB.";
            return RedirectToAction(nameof(Index));
        }

        var blobName = Guid.NewGuid().ToString("N");

        // Keep the original filename as metadata, not as the blob path.
        var extension = Path.GetExtension(file.FileName);
        blobName += extension;

        await using var stream = file.OpenReadStream();

        await _blobService.UploadAsync(
            stream,
            blobName,
            string.IsNullOrWhiteSpace(file.ContentType)
                ? "application/octet-stream"
                : file.ContentType);

        TempData["Message"] = "File uploaded successfully.";

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Download(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return BadRequest();

        // Production code must verify that this user can access
        // the attachment associated with the requested case/matter.
        var stream = await _blobService.DownloadAsync(id);

        return File(stream, "application/octet-stream",
            Path.GetFileName(id));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return BadRequest();

        // Production code must verify authorization and ownership.
        await _blobService.DeleteAsync(id);

        TempData["Message"] = "Attachment deleted successfully.";

        return RedirectToAction(nameof(Index));
    }
}