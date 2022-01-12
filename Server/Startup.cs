using Bazzigg.Database.Context;

using Kartrider.Api.AspNetCore;
using Kartrider.Metadata;
using Kartrider.Metadata.AspNetCore;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

using Server.Jsons.Converter;
using Server.Models.Etc;
using Server.Services;

using System;
using System.IO;
using System.Net;
using System.Text;

namespace Server
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IWebHostEnvironment env)
        {
            Configuration = configuration;
            Env = env;
        }
        public IWebHostEnvironment Env { get; }
        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddResponseCompression(options =>
            {
                options.Providers.Add<BrotliCompressionProvider>();
                options.Providers.Add<GzipCompressionProvider>();
            });
            services.AddControllers();

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "My API",
                    Version = "v1"
                });
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    In = ParameterLocation.Header,
                    Description = "Please insert JWT with Bearer into field",
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "Bearer"
                });
                c.AddSecurityRequirement(new OpenApiSecurityRequirement {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });
            services.AddOptions<JwtOptions>().Bind(Configuration.GetSection("JwtOptions"));

            services.AddKartriderApi(Configuration["KartriderApiKey"]);
            services.AddKartriderMetadata(option =>
            {
                option.Path = Path.Combine(Env.WebRootPath, "metadata");
                option.UpdateInterval = 43200;
                option.UpdateNow = true;
            });
            services.AddDbContext<AppDbContext>(
            dbContextOptions => dbContextOptions
                .UseMySql(Configuration.GetConnectionString("App"), ServerVersion.Parse("10.5.6-MariaDB", ServerType.MariaDb))
#if DEBUG
                .EnableSensitiveDataLogging() // <-- These two calls are optional but help
                .EnableDetailedErrors()       // <-- with debugging (remove for production).
#endif
        );
            services.AddAuthentication(x =>
            {
                x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(x =>
            {
                var jwtSecret = Encoding.ASCII.GetBytes(Configuration.GetSection("JwtOptions:Secret").Value);
                x.RequireHttpsMetadata = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")?.Equals("Production") ?? false;
                /*
                 * 토큰을 저장함, 이러면 컨트롤러에서 아래와 같이 토큰에 접근 가능하다.
                 * var accessToken = await HttpContext.GetTokenAsync("access_token");
                 */
                x.SaveToken = false;
                x.TokenValidationParameters = new TokenValidationParameters
                {
                    /*
                     * 유효성 검사
                     * https://docs.microsoft.com/ko-kr/azure/active-directory/develop/scenario-protected-web-api-app-configuration
                     */

                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(jwtSecret),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.Zero
                };
            });

            services.AddScoped<IJwtService, JwtService>();
            services.AddSingleton<IPlayerService, PlayerService>();
            services.AddControllers().AddJsonOptions(options =>
            {
                // API Response 모델에서 열거형 License값을 문자열로 변환해주기 위함
                options.JsonSerializerOptions.Converters.Add(new LicenseToStringWriteOnlyConverter());
                // API Response 모델에서 TimeSpan데이터를 레코드 형식(m:s:ms)으로 변환해주기 위함
                options.JsonSerializerOptions.Converters.Add(new TimeSpanToStringWriteOnlyConverter());

                options.JsonSerializerOptions.Converters.Add(new DateTimeToStringWriteOnlyConverter());

            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, AppDbContext appDbContext, IKartriderMetadata kartriderMetadata)
        {
            kartriderMetadata.OnUpdated += (metadata, nextRun) =>
            {
                var webClient = new WebClient();
                UpdateFromUrl("https://raw.githubusercontent.com/mschadev/kartrider-open-api-docs/master/metadata/track.json", MetadataType.Track);
                UpdateFromUrl("https://raw.githubusercontent.com/mschadev/kartrider-open-api-docs/master/metadata/channel.json", ExtendMetadataType.Channel);
                void UpdateFromUrl<T>(string url, T metadataType) where T : Enum
                {
                    string path = Path.Combine(Path.GetTempPath(), Path.GetTempFileName());
                    webClient.DownloadFile(url, path);
                    metadata.MetadataUpdate(path, metadataType);
                    File.Delete(path);
                }
            };

            appDbContext.Database.EnsureCreated();
            appDbContext.Database.ExecuteSqlRaw(
                File.ReadAllText(Path.Combine("Sql", "GetPlayerSummarys_procedure.sql")));
            app.UseResponseCompression();
            app.UseStaticFiles();
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "API V1");
                });
            }
            app.UseRouting();

            app.UseAuthentication(); 
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
