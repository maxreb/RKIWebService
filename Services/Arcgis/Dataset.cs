using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Reble.RKIWebService.Entities;

namespace Reble.RKIWebService.Services.Arcgis
{
	public abstract class Dataset
	{
		protected abstract string QueryBaseURL { get; }
		protected abstract string QueryKeyURL { get; }
		protected string QueryAllURL => QueryBaseURL + "&where=1%3D1";


		internal ArcgisData? CurrentDataSet { get; private set; }
		public DateTime LastUpdate { get; internal set; }
		internal SortedDictionary<DateTime, ArcgisData> PastData { get; } = new SortedDictionary<DateTime, ArcgisData>();
		private readonly HttpClient _http;

		public Dataset(HttpClient? http = null)
		{
			_http = http ?? new HttpClient();
		}

		public abstract Task<string> QueryFromCityKeyJson(string cityKey);
		internal abstract Func<Feature, bool> LinqKeySearchMethod(string cityKey);

		public ICovid19Data? GetCurrent(string cityKey)
			=> CurrentDataSet?.Features.FirstOrDefault(LinqKeySearchMethod(cityKey))?.Attributes;


		public bool TryGetFromCityKey(string cityKey, DateTime from, out IEnumerable<ICovid19Data> data, DateTime? to = null)
		{
			to ??= LastUpdate;
			//if dataset is null
			//or state key does not exist
			//or citykey does not exist
			//return empty data and false
			if (CurrentDataSet == null || !CurrentDataSet.Features.Any(LinqKeySearchMethod(cityKey)))
			{
				data = Enumerable.Empty<ICovid19Data>();
				return false;
			}

			data = PastData
				.Where(x => x.Key >= from && x.Key <= to)
				.Select(x => (ICovid19Data?)x.Value.Features.FirstOrDefault(LinqKeySearchMethod(cityKey))?.Attributes)
				.OfType<ICovid19Data>();
			return data.Any();
		}


		internal async Task<string> QueryJson(string? key = null)
		{
			string uri = string.IsNullOrEmpty(key) ? QueryAllURL : string.Format(QueryKeyURL, key);
			var res = await _http.GetAsync(uri).ConfigureAwait(false) ?? throw new Exception("No data");
			return await res.Content.ReadAsStringAsync().ConfigureAwait(false);
		}

		internal bool SetCurrentDataSet(string json, ILogger? logger = null)
		{
			CurrentDataSet = DeserializeArcgis(json);

			if (CurrentDataSet is null)
			{
				logger?.LogError("Deserialization failed, CurrentDataSet is null");
				return false;
			}

			if (!CurrentDataSet.Features.Any())
			{
				logger?.LogError("Deserialization failed, no features");
				return false;
			}

			LastUpdate = CurrentDataSet.Features.First().Attributes.LastUpdate;
			PastData[LastUpdate] = CurrentDataSet;
			return true;
		}
		//public virtual ICovid19Data Deserialize(string json)
		//{
		//	var data = DeserializeArcgis(json);
		//	if (data?.Features == null || !data.Features.Any())
		//	{
		//		throw new SerializationException(json);
		//	}
		//	return data.Features.First().Attributes;
		//}

		public abstract ArcgisData? DeserializeArcgis(string json);
	}
}