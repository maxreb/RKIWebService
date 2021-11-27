using System;
using System.Collections.Generic;
using Microsoft.Extensions.Hosting;
using Reble.RKIWebService.Entities;

namespace Reble.RKIWebService.Services
{
	public interface ICovidApiService : IHostedService
	{
		ICovid19Data? GetCurrentCityData(string cityKey);
		ICovid19Data? GetCurrentStateData(string cityKey);

		/// <summary>
		/// Tries to get the data from specific time span
		/// </summary>
		/// <param name="cityKey">The city key found in CitiesRepository</param>
		/// <param name="from">DateTime from</param>
		/// <param name="data">The output as an enuerable</param>
		/// <param name="to">DateTime to</param>
		/// <returns>On Success: true
		/// On Failure: false and data.Count == 0</returns>
		bool TryGetCityData(string cityKey, DateTime from, out IEnumerable<ICovid19Data> data, DateTime? to = null);

		bool TryGetStateData(string cityKey, DateTime from, out IEnumerable<ICovid19Data> data, DateTime? to = null);
		bool TryGetCountryData(DateTime from, out IEnumerable<ICovid19Data> data, DateTime? to = null);
	}
}