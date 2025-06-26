using HighThroughputApi.Models;
using HighThroughputApi.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.DependencyInjection;
using RedLockNet;
using RedLockNet.SERedis;
using RedLockNet.SERedis.Configuration;
using StackExchange.Redis;
using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using HighThroughputApi.Interfaces;
using HighThroughputApi.Repositories;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Newtonsoft.Json;
using Microsoft.Extensions.Options;

namespace HighThroughputApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddApiVersioning(options =>
            {
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.ReportApiVersions = true;
                options.ApiVersionReader = new UrlSegmentApiVersionReader();
            });

            // Add services to the container
            //builder.Services.AddDbContextPool<AppDbContext>(options =>
            //    options.UseSqlite("Data Source=app.db"));
            builder.Services.AddDbContextPool<AppDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            var redis = ConnectionMultiplexer.Connect(
                builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379");
            builder.Services.AddSingleton<IConnectionMultiplexer>(redis);

            builder.Services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = builder.Configuration.GetConnectionString("Redis");
                options.InstanceName = "HighThroughputApi:"; 
            });

            var redLockFactory = RedLockFactory.Create(new List<RedLockMultiplexer> { redis });
            builder.Services.AddSingleton<RedLockFactory>(redLockFactory);
            builder.Services.AddScoped<ItemService>();
            builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
            builder.Services.AddScoped<IItemRepository, ItemRepository>();
            builder.Services.AddScoped<IOrderItemRepository, OrderItemRepository>();
            builder.Services.AddScoped<IOrderRepository, OrderRepository>();

            builder.Services.AddSwaggerGen(options =>
            {
                options.OperationFilter<AddIfMatchHeaderOperationFilter>();
            });

            builder.Services.AddControllers()
                .AddJsonOptions(opt =>
                {
                   // opt.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve;
                    opt.JsonSerializerOptions.WriteIndented = true;
                });



            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Services.AddRateLimiter(options =>
            {

                options.OnRejected = async (context, cancellationToken) =>
                {
                    context.HttpContext.Response.StatusCode = 429;
                    await context.HttpContext.Response.WriteAsync("exceeded rate limit", cancellationToken);
                };

                options.AddFixedWindowLimiter(policyName: "fixed", config =>
                {
                    config.Window = TimeSpan.FromSeconds(10);  //time window
                    config.PermitLimit = 5; //max limit on requests
                    config.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                    config.QueueLimit = 2;                     
                });
            });


            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAngularApp",
                    policy => policy
                        .WithOrigins("https://45.33.28.119") // Angular dev server on ubuntu
                        .AllowAnyHeader()
                        .AllowAnyMethod());
            });

            builder.Logging.ClearProviders();
            builder.Logging.AddConsole();

            var app = builder.Build();

            // Configure the HTTP request pipeline
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseCors("AllowAngularApp");

            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Database.Migrate();
             
            }

            app.UseRateLimiter(); //middleware

            app.MapControllers().RequireRateLimiting("fixed"); 

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
