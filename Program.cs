using Identity.CustomPolicy;
using Identity.IdentityPolicy;
using Identity.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Identity
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);


            builder.Services.AddDbContext<AppIdentityDbContext>(options =>
            {
                //options.UseSqlServer(builder.Configuration["ConnectionStrings:DefaultConnection"]);
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
            });


            //add Identity và chỉ định class nào dùng để đại diện cho users (AppUser)
            //và class nào dùng để đại diện cho role (IdentityRole)

            //AddEntityFrameworkStores method dùng để chỉ định rằng identity sẽ sử dụng EF core và db context class(AppIdentityDbContext)

            //AddDefautTokenProviders method dùng để add default token providers
            //được sử dụng để tạo ra tokens dùng để reset password, change email,....
            //và dùng để tạo ra mã xác thực 2 yếu tố
            //AddIdentity dùng để đăng kí các dịch dịch vụ như UserManager,SignInManager,RoleManager....(nói chung những class liên quan tới identity đều nằm trong AddIdentity)
            builder.Services.AddIdentity<AppUser, IdentityRole>().AddEntityFrameworkStores<AppIdentityDbContext>().AddDefaultTokenProviders();

            builder.Services.Configure<IdentityOptions>(options =>
            {
                //mặc định password được set các rule như thế này
                options.Password.RequiredLength = 5;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireDigit = true;

                //2 users can not resgister the same email
                options.User.RequireUniqueEmail = true;

                //a list of characters that can only be used when creating a username 
                //options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyz";

                //Xác nhận email
                options.SignIn.RequireConfirmedEmail = true;

            });

            //đăng kí dịch vụ để Check password
            builder.Services.AddTransient<IPasswordValidator<AppUser>, CustomPasswordPolicy>();

            //đăng kí dịch vụ để Check username và email
            //builder.Services.AddTransient<IUserValidator<AppUser>, CustomUsernameEmailPolicy>();

            //configure chuyển hướng tới trang login
            //builder.Services.ConfigureApplicationCookie(option =>
            //{
            //    option.LoginPath = "/Authenticate/Login";
            //});

            //configure cho cookie
            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.Cookie.Name = "Cookiecuatoi";
                options.ExpireTimeSpan = TimeSpan.FromMinutes(20);
                options.SlidingExpiration = true;//xác định xem phiên đăng nhập có được cập nhật không

                //configure cho đường dẫn khi bị deny
                options.AccessDeniedPath = "/Account/AccessDenied";
            });


            //Đăng kí dịch vụ xử lý custom policy ,policy sẽ thực hiện theo yêu cầu mình mong muốn
            builder.Services.AddTransient<IAuthorizationHandler, AllowUsersHandler>();

            builder.Services.AddAuthorization(options =>
            {
                //tự tạo policies cho chương trình của mình 
                options.AddPolicy("AspManager", policy =>
                {

                    policy.RequireRole("Manager");// user phải có role là manager
                    policy.RequireClaim("Coding-skill", "ASP.NET Core MVC"); // Phải có claim là Coding-skill 

                });

                options.AddPolicy("AllowTom", policy =>
                {
                    //AddRequirements : Specifies a custom requirement to the policy
                    //khởi tạo 1 class AllowUserPolicy và cho chỉ những user này mới thỏa điều kiện của policy
                    //khi AddRequirements thì hàm HandleRequirementAsync của AuthorizationHandler sẽ xử lý 
                    //mà AuthorizationHandler được đăng kí theo AllowUsersHandler instance
                    //vi vậy nó sẽ gọi HandleRequirementAsync của AllowUsersHandler 
                    policy.AddRequirements(new AllowUserPolicy("tom","tuan","dinh"));
                });
            });

            //đăng ký 1 IAuthorizationHandler khác
            builder.Services.AddTransient<IAuthorizationHandler, AllowPrivateHandler>();

            //tạo 1 policy mới tên là PrivateAccess
            builder.Services.AddAuthorization(opts =>
            {
                opts.AddPolicy("PrivateAccess", policy =>
                {
                    //ở đây chúng ta không pass bất kì đối sồ nào vào class AllowPrivatePolicy
                    //chúng ta sẽ pass vào từ Claims controller
                    policy.AddRequirements(new AllowPrivatePolicy());
                });
            });


            //đăng kí dịch vụ sử dụng log in bằng google
            builder.Services.AddAuthentication().AddGoogle(options =>
            {
                options.ClientId = "116407193869-c9h65mgs1g0lscdpju7caciddh5aaks8.apps.googleusercontent.com";
                options.ClientSecret = "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";
                options.SignInScheme = IdentityConstants.ExternalScheme;
            });
            // Add services to the container.
            builder.Services.AddControllersWithViews();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();


            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Account}/{action=Login}");

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Admin}/{action=Delete}/{id?}");


            app.Run();
        }
    }
}
