using System.Threading.Tasks;

public class ProjectsService : BaseService
{
    public static async Task<(long, string)> GetMemberSearch(int memberId, int page, int size, string sort = "", string name = "")
    {
        string url = $"{Utils.Settings.BaseUrl}/api/model3ds/member/{memberId}/search?name={name}&page={page}&size={size}&sort={sort}";

        // BaseService의 SendRequest 활용 (GET 방식, 기본 application/json)
        return await SendRequest(url, "GET");
    }

    public static async Task<(long, string)> GetSingleModel3D(int modelId)
    {
        string url = $"{Utils.Settings.BaseUrl}/api/model3ds/{modelId}";

        // BaseService의 SendRequest 활용
        return await SendRequest(url, "GET");
    }
}