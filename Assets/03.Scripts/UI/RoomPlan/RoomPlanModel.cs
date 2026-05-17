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

public class Room3DNotificationDto
{
    public string notificationType;
    public long memberId;
    public long room3dId;
    public string drawingImageUrl;
    public string xmlFileUrl;
    public string status;
    public string message;
    public long timestamp;
}

[System.Serializable]
public class PlacedFurniture
{
    public int modelId;
    public string link;
    public float dimWidth;
    public float dimLength;
    public float dimHeight;
    public float posX, posY, posZ;
    public float rotY;
    public float userScale = 1f;
}
