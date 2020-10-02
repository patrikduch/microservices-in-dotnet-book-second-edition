namespace ProductCatalog
{
  using Microsoft.AspNetCore.Builder;
  using Microsoft.Extensions.DependencyInjection;

  public class Startup
  {
    public void ConfigureServices(IServiceCollection services)
    {
      services.Scan(selector => selector.FromAssemblyOf<Startup>().AddClasses().AsImplementedInterfaces());
      services.AddControllers().AddXmlSerializerFormatters();
    }

    public void Configure(IApplicationBuilder app)
    {
      app.UseRouting();
      app.UseEndpoints(endpoints => endpoints.MapControllers());
    }
  }

}
