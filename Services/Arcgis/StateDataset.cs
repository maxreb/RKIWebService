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
	public class StateDataset : Dataset
	{
		public StateDataset(HttpClient? http = null) : base(http)
		{
		}

		protected override string QueryBaseURL { get; } = "https://services7.arcgis.com/mOBPykOjAyBO2ZKk/arcgis/rest/services/Coronaf%C3%A4lle_in_den_Bundesl%C3%A4ndern/FeatureServer/0/query?outFields=Fallzahl,OBJECTID_1,LAN_ew_GEN,LAN_ew_EWZ,Aktualisierung,faelle_100000_EW,Death,cases7_bl_per_100k,cases7_bl,death7_bl,cases7_bl_per_100k_txt&returnGeometry=false&f=json";
		protected override string QueryKeyURL => QueryBaseURL + "&where=OBJECTID_1=%27{0}%27";
		public override ArcgisData? DeserializeArcgis(string json)
		{
			json = json
				.Replace("\"cases7_bl_per_100k\"", "\"cases7_per_100k\"")
				.Replace("\"Fallzahl\"", "\"cases\"")
				.Replace("\"faelle_100000_EW\"", "\"cases_per_100k\"")
				.Replace("\"Death\"", "\"deaths\"");
			var res = JsonSerializer.Deserialize<ArcgisData?>(json);
			if (res is null)
				return null;
			foreach (var feature in res.Features)
			{
				feature.Attributes.DeathRate = feature.Attributes.TotalDeath / (double)feature.Attributes.TotalStatePopulation;
				feature.Attributes.CasesPerPopulation = feature.Attributes.Cases / (double)feature.Attributes.TotalStatePopulation;
			}
			return res;
		}

		public override Task<string> QueryFromCityKeyJson(string cityKey)
		{
			var stateKey = CitiesRepository.GetStateFromCityKey(cityKey);
			return QueryJson(stateKey.ToString());
		}
		internal override Func<Feature, bool> LinqKeySearchMethod(string key) => (t) => t.Attributes.StateKey == CitiesRepository.GetStateFromCityKey(key);
	}
}
