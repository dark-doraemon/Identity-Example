using Microsoft.AspNetCore.Authorization;

namespace Identity.CustomPolicy
{
    public class AllowUsersHandler : AuthorizationHandler<AllowUserPolicy>
    {
        //HandleRequirementAsync được gọi khi authorization system cần kiêm tra truy cập của 1 action nào đó
        //và nó bị ghi theo yêu cầu mong muốn của chúng ta vì vậy khi authorization nó sẽ vào hàm này
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, AllowUserPolicy requirement)
        {
            //kiểm tra tên các user được cho phép và user name hiện tại có trùng không
            if (requirement.AllowUsers.Any(user => user.Equals(context.User.Identity.Name, StringComparison.OrdinalIgnoreCase)))
            {
                //This method is called if the request meets the requirement.
                //The argument, to this method is the AllowUserPolicy object received by the method.
                context.Succeed(requirement); 
            }
            else
            {
                context.Fail();//This method is called if the request fails to meet the requirement.
            }
            return Task.CompletedTask;
        }
    }
}
