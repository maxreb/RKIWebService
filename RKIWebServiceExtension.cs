using Microsoft.Extensions.DependencyInjection;
using Reble.RKIWebService.Services;
using Reble.RKIWebService.Services.Arcgis;

namespace Reble.RKIWebService
{
	public static class RKIWebServiceExtension
	{
		public static IServiceCollection AddRKIWebService(this IServiceCollection services)
		{
			services.AddSingleton<ICovidApiService, ArcgisService>();
			services.AddSingleton<HospitalizationService>();
			services.AddHostedService(s => s.GetRequiredService<ICovidApiService>() as ArcgisService);
			services.AddHostedService(s => s.GetRequiredService<HospitalizationService>());

			return services;
		}
	}
}