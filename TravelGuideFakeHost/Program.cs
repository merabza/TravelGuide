WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

//Add services to the container.

// ReSharper disable once using
WebApplication app = builder.Build();

//Configure the HTTP request pipeline.

await app.RunAsync();
