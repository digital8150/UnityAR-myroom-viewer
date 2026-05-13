using System.Threading.Tasks;

public class GalleryService : BaseService
{
    public static async Task<(long, string)> GetSharedSearch(int page, int size, string sort = "", string name = null)
    {
        string url = $"{Utils.Settings.BaseUrl}/api/model3ds/shared/search?name={name}&page={page}&size={size}&sort={sort}";
        return await SendRequest(url, "GET");
    }

    public static async Task<(long, string)> GetMyBookmarks(int page, int size, string sort = "")
    {
        string url = $"{Utils.Settings.BaseUrl}/api/model3ds/bookmarks/my?page={page}&size={size}&sort={sort}";
        return await SendRequest(url, "GET");
    }
}
