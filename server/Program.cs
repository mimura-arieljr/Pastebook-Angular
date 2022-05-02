class Program
{
    public static void Main(string[] args)
    {
        // https://stackoverflow.com/questions/59434242/asp-net-core-gives-system-net-sockets-socketexception-error-on-heroku
        // https://habr.com/en/post/450904/
        // https://stackoverflow.com/questions/70332897/how-to-change-default-port-no-of-my-net-core-6-api
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddControllersWithViews()
            .AddJsonOptions(options => options.JsonSerializerOptions.PropertyNamingPolicy = null);
            // Sets response object to Pascal Case
            // https://docs.microsoft.com/en-us/aspnet/core/web-api/advanced/formatting?view=aspnetcore-5.0
        builder.Services.AddCors();
        var app = builder.Build();
        app.UseCors(config => config.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin());
        app.UseFileServer();
        app.UseRouting();
        app.MapControllers();
        // app.Run("http://*:" + Environment.GetEnvironmentVariable("PORT"));
        app.Run("http://localhost:5000");
    }
}
