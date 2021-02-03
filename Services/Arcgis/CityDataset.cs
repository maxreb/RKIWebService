using Microsoft.Extensions.Logging;
using RKIWebService.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace RKIWebService.Services.Arcgis
{
	public class CityDataset : Dataset
	{
		public CityDataset(HttpClient? http = null) : base(http)
		{
		}

		protected override string QueryBaseURL { get; } = "https://services7.arcgis.com/mOBPykOjAyBO2ZKk/arcgis/rest/services/RKI_Landkreisdaten/FeatureServer/0/query?outFields=cases7_per_100k,last_update,cases,GEN,rs,cases_per_100k,death_rate,deaths,cases_per_population&returnGeometry=false&outSR=4326&f=json";
		protected override string QueryKeyURL => QueryBaseURL + "&where=rs=%27{0}%27";

		public override ArcgisData? DeserializeArcgis(string json) => JsonSerializer.Deserialize<ArcgisData?>(json);
		public override Task<string> QueryFromCityKeyJson(string cityKey) => QueryJson(cityKey);

		internal override Func<Feature, bool> LinqKeySearchMethod(string key) => (t) => t.Attributes.CityKey == key;
	}
}
