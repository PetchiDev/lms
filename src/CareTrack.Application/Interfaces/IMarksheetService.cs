namespace CareTrack.Application.Interfaces;

public interface IMarksheetService
{
    Task<(byte[] PdfBytes, string FileName)> RenderForCurrentStudentAsync(Guid quizId, CancellationToken cancellationToken = default);
}

