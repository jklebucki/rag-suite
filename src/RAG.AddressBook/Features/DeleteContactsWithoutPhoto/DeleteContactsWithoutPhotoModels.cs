namespace RAG.AddressBook.Features.DeleteContactsWithoutPhoto;

public record DeleteContactsWithoutPhotoResponse
{
    public int DeletedCount { get; init; }
}
