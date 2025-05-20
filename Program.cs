using log4net;
using log4net.Config;
using System.Reflection;
using Microsoft.AspNetCore.OData;
using Microsoft.EntityFrameworkCore;
using Microsoft.OData.ModelBuilder;
using myappmvc.Data;
using myappmvc.Models;
using Microsoft.Extensions.Logging.Log4Net.AspNetCore;
using StackExchange.Redis;
using myappmvc.Interfaces;
using myappmvc.Services;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using myappmvc.LogEventArgs;



var builder = WebApplication.CreateBuilder(args);


// Đăng ký IConnectionMultiplexer với StackExchange.Redis 
var redisConnectionString = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";


// Đăng ký service:Redis cache với interface:StackExchange.Redis
builder.Services.AddScoped<IRedisCacheService, RedisCacheService>();


//khai báo kết nối đến Redis
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
    ConnectionMultiplexer.Connect(redisConnectionString));


// Đăng ký logger theo event
builder.Services.AddSingleton<EventBasedLogger>();
builder.Services.AddSingleton<Log4NetEventHandler>();

builder.Services.AddSingleton<ILoggerService, LoggerService>();


builder.Logging.ClearProviders(); 
builder.Logging.AddLog4Net();     


var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");


// Cấu hình DbContext với SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.UseRelationalNulls(); 
    }));

builder.Services.AddControllersWithViews();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// OData
builder.Services.AddControllers()
    .AddOData(options => options
        .Select()   // Chọn thuộc tính 
        .Filter()   // Lọc
        .OrderBy()  // Sắp xếp    
        .Expand()   // Mở rộng 
        .SetMaxTop(100)  // Giới hạn số bản ghi 
        .Count()    // Đếm bản ghi
        .AddRouteComponents("odata", GetEdmModel()) // Thêm OData route
    );

var app = builder.Build();

var eventLogger = app.Services.GetRequiredService<EventBasedLogger>();
var logHandler = app.Services.GetRequiredService<Log4NetEventHandler>();
eventLogger.LogRaised += logHandler.Handle;

app.UseSwagger();
    app.UseSwaggerUI();


app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapControllers();


app.Run();

// Cấu hình OData   
static Microsoft.OData.Edm.IEdmModel GetEdmModel()
{
    var builder = new ODataConventionModelBuilder();

    var employee = builder.EntitySet<Employee>("Employees"); // Đăng ký Employee cho OData
    var department = builder.EntitySet<Department>("Departments"); // Đăng ký Department cho OData

    employee.EntityType.HasRequired(e => e.Department);

    return builder.GetEdmModel();
}


