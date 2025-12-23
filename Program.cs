using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

//allw my UI to call in for this port. Otherwise it gets cors problems.
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowUI", policy =>
        policy.WithOrigins("http://localhost:5056")
              .AllowAnyHeader()
              .AllowAnyMethod());
});


builder.Services.AddDbContext<UAVRouterContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddControllers();
var app = builder.Build();
app.UseCors("AllowUI"); //use my "allow policy" here
app.MapControllers();
app.Run();


//TODO:
//chose drone for flight first
//calculate if it can make it
//any final UI clean