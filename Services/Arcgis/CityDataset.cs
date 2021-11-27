using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Reble.RKIWebService.Entities;

namespace Reble.RKIWebService.Services.Arcgis
{
	public class CityDataset : Dataset
	{
		public CityDataset(HttpClient? http = null) : base(http)
		{
		}

		protected override string QueryBaseURL =>
			"https://services7.arcgis.com/mOBPykOjAyBO2ZKk/arcgis/rest/services/RKI_Landkreisdaten/FeatureServer/0/query?outFields=cases7_per_100k,last_update,cases,GEN,rs,cases_per_100k,death_rate,deaths,cases_per_population&returnGeometry=false&outSR=4326&f=json";

		protected override string QueryKeyURL => QueryBaseURL + "&where=rs=%27{0}%27";

		public override ArcgisData? DeserializeArcgis(string json) => JsonSerializer.Deserialize<ArcgisData?>(json);
		public override Task<string> QueryFromCityKeyJson(string cityKey) => QueryJson(cityKey);

		internal override Func<Feature, bool> LinqKeySearchMethod(string key) => (t) => t.Attributes.CityKey == key;
	}
}