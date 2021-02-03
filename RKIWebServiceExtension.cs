using Microsoft.Extensions.DependencyInjection;
using RKIWebService.Services;
using RKIWebService.Services.Arcgis;
using System;

namespace Reble.RKIWebService
{
	public static class RKIWebServiceExtension
	{
		public static IServiceCollection AddRKIWebService(this IServiceCollection services)
		{
			services.AddSingleton<ICovidApiService, ArcgisService>();
			services.AddHostedService(s => s.GetService<ICovidApiService>() as ArcgisService);

			return services;
		}
	}
}
