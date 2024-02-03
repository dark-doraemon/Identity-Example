using Microsoft.AspNetCore.Authorization;

namespace Identity.CustomPolicy
{

    //class này dùng để biết chỉ nhưng user nào được truyên vào constructor mới đáp ứng được policy
    public class AllowUserPolicy : IAuthorizationRequirement
    {
        public string[] AllowUsers { get; set; }

        //params là 1 từ khóa dùng để chỉ 1 thanm số có nhiều nhiều đối số 
        //VD khi sử dụng params var a = new AllowUserPolicy("1","2","3",4")
        public AllowUserPolicy(params string[] users)
        {
            AllowUsers = users;
        }
    }
}
