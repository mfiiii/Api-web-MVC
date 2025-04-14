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



var builder = WebApplication.CreateBuilder(args);


// Đăng ký IConnectionMultiplexer
var redisConnectionString = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
    ConnectionMultiplexer.Connect(redisConnectionString));

// Đăng ký RedisCacheService sử dụng StackExchange.Redis
builder.Services.AddScoped<IRedisCacheService, RedisCacheService>();



builder.Logging.ClearProviders(); 
builder.Logging.AddLog4Net();     // Thêm Log4Net 



var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddControllersWithViews();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//  Cấu hình OData
builder.Services.AddControllers()
    .AddOData(options => options
        .Select()   // Chọn thuộc tính 
        .Filter()   // Lọc
        .OrderBy()  // Sắp xếp    
        .Expand()   // chạy data liên quan 
        .SetMaxTop(100)  // Giới hạn số bản ghi 
        .Count()    // Đếm bản ghi
        .AddRouteComponents("odata", GetEdmModel()) // Thêm OData route
    );

var app = builder.Build();


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

    // Mối quan hệ Employee -> Department
    employee.EntityType.HasRequired(e => e.Department);

    return builder.GetEdmModel();
}


