using System.Collections.Generic;

public class Room3DDto
{
    public long id;
    public long memberId;
    public string roomName;
    public string description;
    public string drawingImageUrl;
    public string drawingXmlUrl;
    public bool? success;
    public string createdAt;
    public string updatedAt;
}

public class PageDto<T>
{
    public List<T> content;
    public int totalPages;
    public long totalElements;
    public int size;
    public int number;
}
