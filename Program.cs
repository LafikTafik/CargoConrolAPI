using Microsoft.EntityFrameworkCore;
using CCAPI.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Добавляем контроллеры
builder.Services.AddControllers();

// Настраиваем подключение к SQL Server
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

// Настройка Swagger с разделами (тегами)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "CargoControl API",
        Description = @"
### Роли пользователей:
- **User**: Может просматривать свои данные
- **Driver**: Может просматривать свои транспортировки
- **Moderator**: Может управлять заказами и транспортом
- **Admin**: Полный доступ",
        Contact = new OpenApiContact
        {
            Name = "Выполнил: Студент групы 3бАСУ2",
            Url = new Uri("https://github.com/LafikTafik/CargoConrolAPI")
        },
        License = new OpenApiLicense
        {
            Name = "MIT License",
            Url = new Uri("https://opensource.org/licenses/MIT")
        }
    });

    // 🔐 Настройка JWT Bearer авторизации
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Введите Bearer + JWT токен"
    });

    // ✅ КЛЮЧЕВАЯ СТРОКА: добавляет Bearer автоматически
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>() // пустой массив — значит, нет дополнительных прав
        }
    });

    // ✅ ГРУППИРОВКА ПО КОНТРОЛЛЕРАМ — ЭТО САМОЕ ВАЖНОЕ!
    options.TagActionsBy(api => new[] { api.ActionDescriptor.RouteValues["controller"] });
    options.DocInclusionPredicate((name, api) => true);

    // 📄 Подключаем XML-комментарии (если есть)
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

// 🔑 JWT-аутентификация
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])
        )
    };
});

// ✅ Авторизация
builder.Services.AddAuthorization();

// HttpContextAccessor (если используешь User в контроллерах)
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// В режиме разработки — Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// 🔐 Порядок важен!
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();