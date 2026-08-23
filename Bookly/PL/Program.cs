using BLL.Hubs;
using BLL.Interfaces;
using BLL.Mapping;
using BLL.Services;
using BLL.Services.Implementation;
using BLL.Services.Interfaces;
using BLL.Settings;
using DAL;
using DAL.Models.Identity;
using DAL.Repository.Implementation;
using DAL.Repository.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;

namespace PL
{
    public class Program
    {
        public static void Main(string[] args)
        {

            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddLocalization(options =>
            {
                options.ResourcesPath = "Resources";
            });

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services.Configure<FileStorageSettings>(builder.Configuration.GetSection("FileStorageSettings"));
            builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
            builder.Services.Configure<PaymobSettings>(builder.Configuration.GetSection("PaymobSettings"));

            builder.Services.AddIdentity<ApplicationUser, IdentityRole<int>>(options =>
            {
                // Password settings
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequireUppercase = true;
                options.Password.RequiredLength = 8;

                // User settings
                options.User.RequireUniqueEmail = true;
                options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+ ";
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

            var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>();

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false; // Set to true in production
                options.SaveToken = true;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidAudience = jwtSettings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SigningKey)),
                    ClockSkew = TimeSpan.Zero
                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        if (context.Request.Cookies.ContainsKey("access_token"))
                        {
                            context.Token = context.Request.Cookies["access_token"];
                        }

                        return Task.CompletedTask;
                    },

                    OnTokenValidated = async context =>
                    {
                        var userManager = context.HttpContext.RequestServices
                            .GetRequiredService<UserManager<ApplicationUser>>();

                        var userId = context.Principal?
                            .FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?
                            .Value;

                        if (string.IsNullOrEmpty(userId))
                        {
                            context.Fail("Invalid user.");
                            return;
                        }

                        var user = await userManager.FindByIdAsync(userId);

                        if (user == null)
                        {
                            context.Fail("User does not exist.");
                            return;
                        }

                        if (user.LockoutEnd.HasValue &&
                            user.LockoutEnd.Value > DateTimeOffset.UtcNow)
                        {
                            context.Fail("User account is suspended.");
                        }
                    },

                    OnChallenge = context =>
                    {
                        context.HandleResponse();

                        context.Response.Redirect("/Account/Login");

                        return Task.CompletedTask;
                    },

                    OnForbidden = context =>
                    {
                        context.Response.Redirect("/Account/AccessDenied");

                        return Task.CompletedTask;
                    }
                };
            });

            builder.Services.ConfigureApplicationCookie(options =>
                {
                    options.LoginPath = "/Account/Login";
                    options.LogoutPath = "/Account/Logout";
                    options.AccessDeniedPath = "/Account/AccessDenied";
                    options.Cookie.HttpOnly = true;
                    options.ExpireTimeSpan = TimeSpan.FromDays(14);
                    options.SlidingExpiration = true;
                });

            builder.Services.AddAutoMapper(config =>
            {
                config.AddProfile<BookingProfile>();
                config.AddProfile<ListingProfile>();
                config.AddProfile<MessageProfile>();
                config.AddProfile<NotificationProfile>();
                config.AddProfile<PaymentProfile>();
                config.AddProfile<ReviewProfile>();
                config.AddProfile<UserProfile>();
            });

            builder.Services.AddValidatorsFromAssemblyContaining<Program>();

            builder.Services.AddSignalR();

            builder.Services.AddScoped<IFileUploader, FileUploader>();
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<ITokenService, TokenService>();
            builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            builder.Services.AddScoped<IHomeService, HomeService>();
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<IListingService, ListingService>();
            builder.Services.AddScoped<IAmenityService, AmenityService>();
            builder.Services.AddScoped<IVerificationService, VerificationService>();
            builder.Services.AddScoped<IAdminService, AdminService>();
            builder.Services.AddScoped<INotificationService, NotificationService>();
            builder.Services.AddScoped<IBookingService, BookingService>();
            builder.Services.AddHttpClient<IPaymentService, PaymentService>();
            builder.Services.AddScoped<IWishlistService, WishlistService>();
            builder.Services.AddScoped<IReviewService, ReviewService>();

            var app = builder.Build();
            var supportedCultures = new[] { "en", "ar" };

            var localizationOptions = new RequestLocalizationOptions()
                .SetDefaultCulture("en")
                .AddSupportedCultures(supportedCultures)
                .AddSupportedUICultures(supportedCultures);

            app.UseRequestLocalization(localizationOptions);
            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapStaticAssets();
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}")
                .WithStaticAssets();

            app.MapHub<NotificationHub>("/hubs/notifications");

            app.Run();
        }
    }
}